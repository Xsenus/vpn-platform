using System.Text.Json;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using VpnPlatform.Application.Abstractions;
using VpnPlatform.Application.DTOs;
using VpnPlatform.Application.Services;
using VpnPlatform.Domain.Entities;
using VpnPlatform.Infrastructure.Persistence;
using Xunit;

namespace VpnPlatform.UnitTests;

public class TelegramNotificationDeliveryTests
{
    [Fact]
    public async Task Concurrent_Dispatchers_Should_Send_Notification_Once()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"vpn-platform-telegram-notification-{Guid.NewGuid():N}.db");
        var connectionString = $"Data Source={dbPath};Cache=Shared;Default Timeout=10";
        var clock = new MutableClock(new DateTimeOffset(2026, 8, 4, 13, 0, 0, TimeSpan.Zero));
        var notification = Notification(clock.UtcNow);
        try
        {
            await using (var seedDb = CreateDbContext(connectionString))
            {
                await seedDb.Database.EnsureCreatedAsync();
                seedDb.TelegramBotNotifications.Add(notification);
                await seedDb.SaveChangesAsync();
            }

            var provider = new BlockingProvider();
            await using var firstDb = CreateDbContext(connectionString);
            await using var secondDb = CreateDbContext(connectionString);
            var firstDelivery = new TelegramNotificationDeliveryService(firstDb, clock, provider);
            var secondDelivery = new TelegramNotificationDeliveryService(secondDb, clock, provider);

            var firstTask = firstDelivery.DeliverAsync(notification.Id, CancellationToken.None);
            await provider.FirstCallStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));
            var secondTask = secondDelivery.DeliverAsync(notification.Id, CancellationToken.None);
            await Task.Delay(250);
            Assert.Equal(1, provider.SendCalls);
            provider.Release();

            var results = await Task.WhenAll(firstTask, secondTask);

            Assert.All(results, result => Assert.True(result.IsSuccess, result.Error));
            Assert.Equal(1, provider.SendCalls);
            await using var inspectDb = CreateDbContext(connectionString);
            var stored = await inspectDb.TelegramBotNotifications.AsNoTracking().SingleAsync(x => x.Id == notification.Id);
            Assert.Equal("sent", stored.Status);
            Assert.Equal(1, stored.AttemptCount);
            Assert.NotNull(stored.SentAt);
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            if (File.Exists(dbPath))
            {
                File.Delete(dbPath);
            }
        }
    }

    [Fact]
    public async Task Stale_Sending_Notification_Should_Be_Reclaimed()
    {
        await using var connection = await OpenConnectionAsync();
        await using var db = CreateDbContext(connection);
        await db.Database.EnsureCreatedAsync();
        var clock = new MutableClock(new DateTimeOffset(2026, 8, 4, 13, 0, 0, TimeSpan.Zero));
        var notification = Notification(clock.UtcNow.Subtract(TimeSpan.FromMinutes(2)), "sending", attemptCount: 1);
        db.TelegramBotNotifications.Add(notification);
        await db.SaveChangesAsync();
        var provider = new RecordingProvider();
        var delivery = new TelegramNotificationDeliveryService(db, clock, provider);

        var result = await delivery.DeliverAsync(notification.Id, CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error);
        Assert.Equal(1, provider.SendCalls);
        var stored = await db.TelegramBotNotifications.AsNoTracking().SingleAsync(x => x.Id == notification.Id);
        Assert.Equal("sent", stored.Status);
        Assert.Equal(2, stored.AttemptCount);
    }

    [Fact]
    public async Task Fresh_Sending_Notification_Should_Remain_Leased()
    {
        await using var connection = await OpenConnectionAsync();
        await using var db = CreateDbContext(connection);
        await db.Database.EnsureCreatedAsync();
        var clock = new MutableClock(new DateTimeOffset(2026, 8, 4, 13, 0, 0, TimeSpan.Zero));
        var notification = Notification(clock.UtcNow, "sending", attemptCount: 1);
        db.TelegramBotNotifications.Add(notification);
        await db.SaveChangesAsync();
        var provider = new RecordingProvider();
        var delivery = new TelegramNotificationDeliveryService(db, clock, provider);

        var result = await delivery.DeliverAsync(notification.Id, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.True(result.IsRetryable);
        Assert.Equal(0, provider.SendCalls);
    }

    [Fact]
    public async Task Transient_Failure_Should_Retry_With_Redacted_Error()
    {
        await using var connection = await OpenConnectionAsync();
        await using var db = CreateDbContext(connection);
        await db.Database.EnsureCreatedAsync();
        var clock = new MutableClock(new DateTimeOffset(2026, 8, 4, 13, 0, 0, TimeSpan.Zero));
        var notification = Notification(clock.UtcNow);
        db.TelegramBotNotifications.Add(notification);
        await db.SaveChangesAsync();
        var provider = new FailFirstProvider();
        var delivery = new TelegramNotificationDeliveryService(db, clock, provider);

        var first = await delivery.DeliverAsync(notification.Id, CancellationToken.None);

        Assert.False(first.IsSuccess);
        Assert.True(first.IsRetryable);
        var pending = await db.TelegramBotNotifications.AsNoTracking().SingleAsync(x => x.Id == notification.Id);
        Assert.Equal("pending", pending.Status);
        Assert.NotNull(pending.NextAttemptAt);
        Assert.Contains("REDACTED", pending.ErrorText, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("super-secret", pending.ErrorText, StringComparison.Ordinal);

        var scheduled = await delivery.DeliverAsync(notification.Id, CancellationToken.None);
        Assert.False(scheduled.IsSuccess);
        Assert.Equal(1, provider.SendCalls);

        clock.Advance(TimeSpan.FromSeconds(11));
        var retry = await delivery.DeliverAsync(notification.Id, CancellationToken.None);

        Assert.True(retry.IsSuccess, retry.Error);
        Assert.Equal(2, provider.SendCalls);
        Assert.Equal("sent", (await db.TelegramBotNotifications.AsNoTracking().SingleAsync(x => x.Id == notification.Id)).Status);
    }

    [Fact]
    public async Task Cancellation_Should_Persist_Retry_State_And_Propagate()
    {
        await using var connection = await OpenConnectionAsync();
        await using var db = CreateDbContext(connection);
        await db.Database.EnsureCreatedAsync();
        var clock = new MutableClock(new DateTimeOffset(2026, 8, 4, 13, 0, 0, TimeSpan.Zero));
        var notification = Notification(clock.UtcNow);
        db.TelegramBotNotifications.Add(notification);
        await db.SaveChangesAsync();
        using var cancellation = new CancellationTokenSource();
        var delivery = new TelegramNotificationDeliveryService(db, clock, new CancellingProvider(cancellation));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => delivery.DeliverAsync(notification.Id, cancellation.Token));

        var stored = await db.TelegramBotNotifications.AsNoTracking().SingleAsync(x => x.Id == notification.Id);
        Assert.Equal("pending", stored.Status);
        Assert.NotNull(stored.NextAttemptAt);
        Assert.Contains("cancel", stored.ErrorText, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Fifth_Failure_Should_Move_Notification_To_Failed()
    {
        await using var connection = await OpenConnectionAsync();
        await using var db = CreateDbContext(connection);
        await db.Database.EnsureCreatedAsync();
        var clock = new MutableClock(new DateTimeOffset(2026, 8, 4, 13, 0, 0, TimeSpan.Zero));
        var notification = Notification(clock.UtcNow, attemptCount: 4);
        db.TelegramBotNotifications.Add(notification);
        await db.SaveChangesAsync();
        var delivery = new TelegramNotificationDeliveryService(db, clock, new AlwaysFailProvider());

        var result = await delivery.DeliverAsync(notification.Id, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.False(result.IsRetryable);
        var stored = await db.TelegramBotNotifications.AsNoTracking().SingleAsync(x => x.Id == notification.Id);
        Assert.Equal("failed", stored.Status);
        Assert.Equal(5, stored.AttemptCount);
        Assert.Null(stored.NextAttemptAt);
    }

    [Fact]
    public async Task Blocked_Account_Should_Cancel_Notification_Without_Send()
    {
        await using var connection = await OpenConnectionAsync();
        await using var db = CreateDbContext(connection);
        await db.Database.EnsureCreatedAsync();
        var clock = new MutableClock(new DateTimeOffset(2026, 8, 4, 13, 0, 0, TimeSpan.Zero));
        var notification = Notification(clock.UtcNow);
        db.TelegramBotNotifications.Add(notification);
        db.TelegramAccounts.Add(new TelegramAccount { TelegramUserId = notification.TelegramUserId, IsBlocked = true });
        await db.SaveChangesAsync();
        var provider = new RecordingProvider();
        var delivery = new TelegramNotificationDeliveryService(db, clock, provider);

        var result = await delivery.DeliverAsync(notification.Id, CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error);
        Assert.Equal(0, provider.SendCalls);
        Assert.Equal("cancelled", (await db.TelegramBotNotifications.AsNoTracking().SingleAsync(x => x.Id == notification.Id)).Status);
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("{\"text\":123}")]
    [InlineData("{\"text\":\"Hello\",\"replyMarkupJson\":{}}")]
    [InlineData("{\"text\":\"Hello\",\"replyMarkupJson\":\"not-json\"}")]
    public async Task Invalid_Payload_Should_Fail_Without_Send(string payloadJson)
    {
        await using var connection = await OpenConnectionAsync();
        await using var db = CreateDbContext(connection);
        await db.Database.EnsureCreatedAsync();
        var clock = new MutableClock(new DateTimeOffset(2026, 8, 4, 13, 0, 0, TimeSpan.Zero));
        var notification = Notification(clock.UtcNow);
        notification.PayloadJson = payloadJson;
        db.TelegramBotNotifications.Add(notification);
        await db.SaveChangesAsync();
        var provider = new RecordingProvider();
        var delivery = new TelegramNotificationDeliveryService(db, clock, provider);

        var result = await delivery.DeliverAsync(notification.Id, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(0, provider.SendCalls);
        Assert.Equal("failed", (await db.TelegramBotNotifications.AsNoTracking().SingleAsync(x => x.Id == notification.Id)).Status);
    }

    [Fact]
    public async Task Legacy_Plain_Text_Payload_Should_Be_Delivered()
    {
        await using var connection = await OpenConnectionAsync();
        await using var db = CreateDbContext(connection);
        await db.Database.EnsureCreatedAsync();
        var clock = new MutableClock(new DateTimeOffset(2026, 8, 4, 13, 0, 0, TimeSpan.Zero));
        var notification = Notification(clock.UtcNow);
        notification.PayloadJson = "Legacy notification text";
        db.TelegramBotNotifications.Add(notification);
        await db.SaveChangesAsync();
        var provider = new RecordingProvider();
        var delivery = new TelegramNotificationDeliveryService(db, clock, provider);

        var result = await delivery.DeliverAsync(notification.Id, CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error);
        Assert.Equal(1, provider.SendCalls);
        Assert.Equal("sent", (await db.TelegramBotNotifications.AsNoTracking().SingleAsync(x => x.Id == notification.Id)).Status);
    }

    [Fact]
    public async Task Dispatchable_Query_Should_Return_Due_And_Stale_Only()
    {
        await using var connection = await OpenConnectionAsync();
        await using var db = CreateDbContext(connection);
        await db.Database.EnsureCreatedAsync();
        var clock = new MutableClock(new DateTimeOffset(2026, 8, 4, 13, 0, 0, TimeSpan.Zero));
        var due = Notification(clock.UtcNow.Subtract(TimeSpan.FromMinutes(3)));
        var scheduled = Notification(clock.UtcNow);
        scheduled.NextAttemptAt = clock.UtcNow.AddMinutes(1);
        var stale = Notification(clock.UtcNow.Subtract(TimeSpan.FromMinutes(2)), "sending", attemptCount: 1);
        var active = Notification(clock.UtcNow, "sending", attemptCount: 1);
        var sent = Notification(clock.UtcNow, "sent", attemptCount: 1);
        sent.SentAt = clock.UtcNow;
        db.TelegramBotNotifications.AddRange(due, scheduled, stale, active, sent);
        await db.SaveChangesAsync();
        var delivery = new TelegramNotificationDeliveryService(db, clock, new RecordingProvider());

        var ids = await delivery.GetDispatchableIdsAsync(20, CancellationToken.None);

        Assert.Equal(new[] { due.Id, stale.Id }.OrderBy(x => x), ids.OrderBy(x => x));
    }

    private static TelegramBotNotification Notification(
        DateTimeOffset now,
        string status = "pending",
        int attemptCount = 0)
        => new()
        {
            TelegramUserId = 777001,
            Type = "test",
            PayloadJson = JsonSerializer.Serialize(new { text = "Hello", replyMarkupJson = (string?)null }),
            Status = status,
            AttemptCount = attemptCount,
            NextAttemptAt = status == "pending" ? now : null,
            CreatedAt = now,
            UpdatedAt = now
        };

    private static async Task<SqliteConnection> OpenConnectionAsync()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        return connection;
    }

    private static ApplicationDbContext CreateDbContext(SqliteConnection connection)
        => new(new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(connection).Options);

    private static ApplicationDbContext CreateDbContext(string connectionString)
        => new(new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(connectionString).Options);

    private sealed class MutableClock(DateTimeOffset now) : IClock
    {
        public DateTimeOffset UtcNow { get; private set; } = now;
        public void Advance(TimeSpan duration) => UtcNow = UtcNow.Add(duration);
    }

    private class RecordingProvider : ITelegramInvoiceProvider
    {
        private int _sendCalls;
        public int SendCalls => Volatile.Read(ref _sendCalls);
        public Task<TelegramInvoiceResult> CreateInvoiceAsync(TelegramInvoiceRequest request, CancellationToken cancellationToken) => Task.FromResult(new TelegramInvoiceResult(request.Payload, "{}"));
        public Task AnswerPreCheckoutQueryAsync(string preCheckoutQueryId, bool ok, string? errorMessage, CancellationToken cancellationToken) => Task.CompletedTask;
        public virtual Task SendMessageAsync(long chatId, string text, string? replyMarkupJson, CancellationToken cancellationToken) { Interlocked.Increment(ref _sendCalls); return Task.CompletedTask; }
        protected void RecordSend() => Interlocked.Increment(ref _sendCalls);
    }

    private sealed class FailFirstProvider : RecordingProvider
    {
        public override Task SendMessageAsync(long chatId, string text, string? replyMarkupJson, CancellationToken cancellationToken)
        {
            RecordSend();
            return SendCalls == 1
                ? Task.FromException(new InvalidOperationException("password=super-secret"))
                : Task.CompletedTask;
        }
    }

    private sealed class AlwaysFailProvider : RecordingProvider
    {
        public override Task SendMessageAsync(long chatId, string text, string? replyMarkupJson, CancellationToken cancellationToken)
        {
            RecordSend();
            return Task.FromException(new InvalidOperationException("temporary Telegram failure"));
        }
    }

    private sealed class BlockingProvider : ITelegramInvoiceProvider
    {
        private readonly TaskCompletionSource _release = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int _sendCalls;
        public TaskCompletionSource FirstCallStarted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public int SendCalls => Volatile.Read(ref _sendCalls);
        public Task<TelegramInvoiceResult> CreateInvoiceAsync(TelegramInvoiceRequest request, CancellationToken cancellationToken) => Task.FromResult(new TelegramInvoiceResult(request.Payload, "{}"));
        public Task AnswerPreCheckoutQueryAsync(string preCheckoutQueryId, bool ok, string? errorMessage, CancellationToken cancellationToken) => Task.CompletedTask;
        public async Task SendMessageAsync(long chatId, string text, string? replyMarkupJson, CancellationToken cancellationToken) { Interlocked.Increment(ref _sendCalls); FirstCallStarted.TrySetResult(); await _release.Task.WaitAsync(cancellationToken); }
        public void Release() => _release.TrySetResult();
    }

    private sealed class CancellingProvider(CancellationTokenSource cancellation) : ITelegramInvoiceProvider
    {
        public Task<TelegramInvoiceResult> CreateInvoiceAsync(TelegramInvoiceRequest request, CancellationToken cancellationToken) => Task.FromResult(new TelegramInvoiceResult(request.Payload, "{}"));
        public Task AnswerPreCheckoutQueryAsync(string preCheckoutQueryId, bool ok, string? errorMessage, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task SendMessageAsync(long chatId, string text, string? replyMarkupJson, CancellationToken cancellationToken) { cancellation.Cancel(); throw new OperationCanceledException(cancellation.Token); }
    }
}
