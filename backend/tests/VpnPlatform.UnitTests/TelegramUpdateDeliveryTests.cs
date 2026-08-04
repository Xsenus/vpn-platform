using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using VpnPlatform.Application.Abstractions;
using VpnPlatform.Application.DTOs;
using VpnPlatform.Application.Services;
using VpnPlatform.Domain.Entities;
using VpnPlatform.Infrastructure.Persistence;
using Xunit;

namespace VpnPlatform.UnitTests;

public class TelegramUpdateDeliveryTests
{
    [Fact]
    public async Task Partial_Delivery_Should_Not_Repeat_PreCheckout_After_Message_Failure()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var db = CreateDbContext(connection);
        await db.Database.EnsureCreatedAsync();
        var clock = new MutableClock(new DateTimeOffset(2026, 8, 4, 12, 0, 0, TimeSpan.Zero));
        db.TelegramBotUpdates.Add(ProcessedUpdate(9101, clock.UtcNow, includePreCheckout: true));
        await db.SaveChangesAsync();
        var provider = new FailFirstMessageProvider();
        var delivery = new TelegramUpdateDeliveryService(db, clock, provider);

        var first = await delivery.DeliverAsync(9101, CancellationToken.None);

        Assert.False(first.IsSuccess);
        Assert.True(first.IsRetryable);
        var partial = await db.TelegramBotUpdates.SingleAsync(x => x.UpdateId == 9101);
        Assert.NotNull(partial.PreCheckoutAnsweredAt);
        Assert.Null(partial.ResponseSentAt);
        Assert.Equal(1, provider.PreCheckoutCalls);
        Assert.Equal(1, provider.MessageCalls);

        clock.Advance(TimeSpan.FromSeconds(11));
        var retry = await delivery.DeliverAsync(9101, CancellationToken.None);

        Assert.True(retry.IsSuccess, retry.Error);
        Assert.Equal(1, provider.PreCheckoutCalls);
        Assert.Equal(2, provider.MessageCalls);
        Assert.NotNull((await db.TelegramBotUpdates.SingleAsync(x => x.UpdateId == 9101)).ResponseSentAt);
    }

    [Fact]
    public async Task Concurrent_Delivery_Should_Send_Message_Once()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"vpn-platform-telegram-delivery-{Guid.NewGuid():N}.db");
        var connectionString = $"Data Source={dbPath};Cache=Shared;Default Timeout=10";
        var clock = new MutableClock(new DateTimeOffset(2026, 8, 4, 12, 0, 0, TimeSpan.Zero));
        try
        {
            await using (var seedDb = CreateDbContext(connectionString))
            {
                await seedDb.Database.EnsureCreatedAsync();
                seedDb.TelegramBotUpdates.Add(ProcessedUpdate(9102, clock.UtcNow));
                await seedDb.SaveChangesAsync();
            }

            var provider = new BlockingProvider();
            await using var firstDb = CreateDbContext(connectionString);
            await using var secondDb = CreateDbContext(connectionString);
            var firstDelivery = new TelegramUpdateDeliveryService(firstDb, clock, provider);
            var secondDelivery = new TelegramUpdateDeliveryService(secondDb, clock, provider);

            var firstTask = firstDelivery.DeliverAsync(9102, CancellationToken.None);
            await provider.FirstCallStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));
            var secondTask = secondDelivery.DeliverAsync(9102, CancellationToken.None);
            await Task.Delay(250);
            Assert.Equal(1, provider.MessageCalls);
            provider.Release();

            var results = await Task.WhenAll(firstTask, secondTask);

            Assert.All(results, result => Assert.True(result.IsSuccess, result.Error));
            Assert.Equal(1, provider.MessageCalls);
            await using var inspectDb = CreateDbContext(connectionString);
            var update = await inspectDb.TelegramBotUpdates.AsNoTracking().SingleAsync(x => x.UpdateId == 9102);
            Assert.NotNull(update.ResponseSentAt);
            Assert.Equal(1, update.DeliveryAttemptCount);
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
    public async Task Pending_Query_Should_Return_Only_Due_Unclaimed_Deliveries()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var db = CreateDbContext(connection);
        await db.Database.EnsureCreatedAsync();
        var clock = new MutableClock(new DateTimeOffset(2026, 8, 4, 12, 0, 0, TimeSpan.Zero));
        var due = ProcessedUpdate(9103, clock.UtcNow.Subtract(TimeSpan.FromMinutes(2)));
        var scheduled = ProcessedUpdate(9104, clock.UtcNow);
        scheduled.DeliveryNextAttemptAt = clock.UtcNow.AddMinutes(1);
        var claimed = ProcessedUpdate(9105, clock.UtcNow);
        claimed.DeliveryClaimedAt = clock.UtcNow;
        var sent = ProcessedUpdate(9106, clock.UtcNow);
        sent.ResponseSentAt = clock.UtcNow;
        db.TelegramBotUpdates.AddRange(due, scheduled, claimed, sent);
        await db.SaveChangesAsync();
        var delivery = new TelegramUpdateDeliveryService(db, clock, new RecordingProvider());

        var pending = await delivery.GetPendingUpdateIdsAsync(20, CancellationToken.None);

        Assert.Equal(new[] { 9103L }, pending);
    }

    [Fact]
    public async Task Delivery_Cancellation_Should_Persist_Retry_State_And_Propagate()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var db = CreateDbContext(connection);
        await db.Database.EnsureCreatedAsync();
        var clock = new MutableClock(new DateTimeOffset(2026, 8, 4, 12, 0, 0, TimeSpan.Zero));
        db.TelegramBotUpdates.Add(ProcessedUpdate(9107, clock.UtcNow));
        await db.SaveChangesAsync();
        using var cancellation = new CancellationTokenSource();
        var delivery = new TelegramUpdateDeliveryService(db, clock, new CancellingProvider(cancellation));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => delivery.DeliverAsync(9107, cancellation.Token));

        var update = await db.TelegramBotUpdates.AsNoTracking().SingleAsync(x => x.UpdateId == 9107);
        Assert.Null(update.DeliveryClaimedAt);
        Assert.NotNull(update.DeliveryNextAttemptAt);
        Assert.Contains("cancel", update.DeliveryErrorText, StringComparison.OrdinalIgnoreCase);
        Assert.Null(update.ResponseSentAt);
    }

    private static TelegramBotUpdate ProcessedUpdate(long updateId, DateTimeOffset now, bool includePreCheckout = false)
        => new()
        {
            UpdateId = updateId,
            TelegramUserId = 777001,
            UpdateType = "message",
            RawPayload = "{}",
            PayloadSha256 = "hash",
            IsProcessed = true,
            ProcessedAt = now,
            ResponseChatId = 777001,
            ResponseText = "Hello",
            ResponseReplyMarkupJson = "{}",
            PreCheckoutQueryId = includePreCheckout ? $"pre-{updateId}" : string.Empty,
            PreCheckoutOk = includePreCheckout ? true : null,
            CreatedAt = now,
            UpdatedAt = now
        };

    private static ApplicationDbContext CreateDbContext(SqliteConnection connection)
        => new(new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(connection).Options);

    private static ApplicationDbContext CreateDbContext(string connectionString)
        => new(new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(connectionString).Options);

    private sealed class MutableClock(DateTimeOffset now) : IClock
    {
        public DateTimeOffset UtcNow { get; private set; } = now;
        public void Advance(TimeSpan duration) => UtcNow = UtcNow.Add(duration);
    }

    private sealed class RecordingProvider : ITelegramInvoiceProvider
    {
        public Task<TelegramInvoiceResult> CreateInvoiceAsync(TelegramInvoiceRequest request, CancellationToken cancellationToken)
            => Task.FromResult(new TelegramInvoiceResult(request.Payload, "{}"));
        public Task AnswerPreCheckoutQueryAsync(string preCheckoutQueryId, bool ok, string? errorMessage, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task SendMessageAsync(long chatId, string text, string? replyMarkupJson, CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class FailFirstMessageProvider : ITelegramInvoiceProvider
    {
        public int PreCheckoutCalls { get; private set; }
        public int MessageCalls { get; private set; }
        public Task<TelegramInvoiceResult> CreateInvoiceAsync(TelegramInvoiceRequest request, CancellationToken cancellationToken) => Task.FromResult(new TelegramInvoiceResult(request.Payload, "{}"));
        public Task AnswerPreCheckoutQueryAsync(string preCheckoutQueryId, bool ok, string? errorMessage, CancellationToken cancellationToken) { PreCheckoutCalls++; return Task.CompletedTask; }
        public Task SendMessageAsync(long chatId, string text, string? replyMarkupJson, CancellationToken cancellationToken) { MessageCalls++; return MessageCalls == 1 ? Task.FromException(new InvalidOperationException("temporary send failure")) : Task.CompletedTask; }
    }

    private sealed class BlockingProvider : ITelegramInvoiceProvider
    {
        private readonly TaskCompletionSource _release = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int _messageCalls;
        public TaskCompletionSource FirstCallStarted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public int MessageCalls => Volatile.Read(ref _messageCalls);
        public Task<TelegramInvoiceResult> CreateInvoiceAsync(TelegramInvoiceRequest request, CancellationToken cancellationToken) => Task.FromResult(new TelegramInvoiceResult(request.Payload, "{}"));
        public Task AnswerPreCheckoutQueryAsync(string preCheckoutQueryId, bool ok, string? errorMessage, CancellationToken cancellationToken) => Task.CompletedTask;
        public async Task SendMessageAsync(long chatId, string text, string? replyMarkupJson, CancellationToken cancellationToken) { Interlocked.Increment(ref _messageCalls); FirstCallStarted.TrySetResult(); await _release.Task.WaitAsync(cancellationToken); }
        public void Release() => _release.TrySetResult();
    }

    private sealed class CancellingProvider(CancellationTokenSource cancellation) : ITelegramInvoiceProvider
    {
        public Task<TelegramInvoiceResult> CreateInvoiceAsync(TelegramInvoiceRequest request, CancellationToken cancellationToken) => Task.FromResult(new TelegramInvoiceResult(request.Payload, "{}"));
        public Task AnswerPreCheckoutQueryAsync(string preCheckoutQueryId, bool ok, string? errorMessage, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task SendMessageAsync(long chatId, string text, string? replyMarkupJson, CancellationToken cancellationToken) { cancellation.Cancel(); throw new OperationCanceledException(cancellation.Token); }
    }
}
