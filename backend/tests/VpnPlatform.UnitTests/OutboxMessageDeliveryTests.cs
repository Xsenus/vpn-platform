using System.Text.Json;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using VpnPlatform.Application.Abstractions;
using VpnPlatform.Application.Services;
using VpnPlatform.Domain.Entities;
using VpnPlatform.Infrastructure.Persistence;
using VpnPlatform.Infrastructure.Services;
using Xunit;

namespace VpnPlatform.UnitTests;

public class OutboxMessageDeliveryTests
{
    [Fact]
    public async Task Concurrent_Dispatchers_Should_Invoke_Sink_Once()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"vpn-platform-outbox-delivery-{Guid.NewGuid():N}.db");
        var connectionString = $"Data Source={dbPath};Cache=Shared;Default Timeout=10";
        var clock = new MutableClock(new DateTimeOffset(2026, 8, 4, 14, 0, 0, TimeSpan.Zero));
        var message = Message(clock.UtcNow);
        try
        {
            await using (var setup = CreateDbContext(connectionString))
            {
                await setup.Database.EnsureCreatedAsync();
                setup.OutboxMessages.Add(message);
                await setup.SaveChangesAsync();
            }

            var sink = new BlockingSink();
            await using var firstDb = CreateDbContext(connectionString);
            await using var secondDb = CreateDbContext(connectionString);
            var first = new OutboxMessageDeliveryService(firstDb, clock, sink);
            var second = new OutboxMessageDeliveryService(secondDb, clock, sink);

            var firstTask = first.DeliverAsync(message.Id);
            await sink.FirstCallStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));
            var secondTask = second.DeliverAsync(message.Id);
            await Task.Delay(200);
            Assert.Equal(1, sink.Calls);
            sink.Release();

            var results = await Task.WhenAll(firstTask, secondTask);
            Assert.All(results, result => Assert.True(result.IsSuccess, result.Error));
            Assert.Equal(1, sink.Calls);
            await using var inspect = CreateDbContext(connectionString);
            var stored = await inspect.OutboxMessages.AsNoTracking().SingleAsync();
            Assert.NotNull(stored.ProcessedAt);
            Assert.Equal(1, stored.Attempts);
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
    public async Task Stale_Processing_Lease_Should_Be_Reclaimed()
    {
        await using var connection = await OpenConnectionAsync();
        await using var db = CreateDbContext(connection);
        await db.Database.EnsureCreatedAsync();
        var clock = new MutableClock(new DateTimeOffset(2026, 8, 4, 14, 0, 0, TimeSpan.Zero));
        var message = Message(clock.UtcNow.AddMinutes(-2));
        message.Attempts = 1;
        message.ProcessingStartedAt = clock.UtcNow.AddMinutes(-2);
        db.OutboxMessages.Add(message);
        await db.SaveChangesAsync();
        var sink = new RecordingSink();

        var result = await new OutboxMessageDeliveryService(db, clock, sink).DeliverAsync(message.Id);

        Assert.True(result.IsSuccess, result.Error);
        Assert.Equal(1, sink.Calls);
        var stored = await db.OutboxMessages.AsNoTracking().SingleAsync();
        Assert.Equal(2, stored.Attempts);
        Assert.NotNull(stored.ProcessedAt);
    }

    [Fact]
    public async Task Fresh_Processing_Lease_Should_Not_Invoke_Sink()
    {
        await using var connection = await OpenConnectionAsync();
        await using var db = CreateDbContext(connection);
        await db.Database.EnsureCreatedAsync();
        var clock = new MutableClock(new DateTimeOffset(2026, 8, 4, 14, 0, 0, TimeSpan.Zero));
        var message = Message(clock.UtcNow);
        message.Attempts = 1;
        message.ProcessingStartedAt = clock.UtcNow;
        db.OutboxMessages.Add(message);
        await db.SaveChangesAsync();
        var sink = new RecordingSink();

        var result = await new OutboxMessageDeliveryService(db, clock, sink).DeliverAsync(message.Id);

        Assert.False(result.IsSuccess);
        Assert.True(result.IsRetryable);
        Assert.Equal(0, sink.Calls);
    }

    [Fact]
    public async Task Transient_Failure_Should_Retry_With_Redacted_Backoff()
    {
        await using var connection = await OpenConnectionAsync();
        await using var db = CreateDbContext(connection);
        await db.Database.EnsureCreatedAsync();
        var clock = new MutableClock(new DateTimeOffset(2026, 8, 4, 14, 0, 0, TimeSpan.Zero));
        var message = Message(clock.UtcNow);
        db.OutboxMessages.Add(message);
        await db.SaveChangesAsync();
        var sink = new FailFirstSink();
        var delivery = new OutboxMessageDeliveryService(db, clock, sink);

        var first = await delivery.DeliverAsync(message.Id);

        Assert.False(first.IsSuccess);
        Assert.True(first.IsRetryable);
        var pending = await db.OutboxMessages.AsNoTracking().SingleAsync();
        Assert.NotNull(pending.NextAttemptAt);
        Assert.Null(pending.ProcessingStartedAt);
        Assert.Contains("REDACTED", pending.LastError!, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("super-secret", pending.LastError!, StringComparison.Ordinal);

        Assert.False((await delivery.DeliverAsync(message.Id)).IsSuccess);
        Assert.Equal(1, sink.Calls);
        clock.Advance(TimeSpan.FromSeconds(11));
        Assert.True((await delivery.DeliverAsync(message.Id)).IsSuccess);
        Assert.Equal(2, sink.Calls);
    }

    [Fact]
    public async Task Cancellation_Should_Persist_Retry_And_Propagate()
    {
        await using var connection = await OpenConnectionAsync();
        await using var db = CreateDbContext(connection);
        await db.Database.EnsureCreatedAsync();
        var clock = new MutableClock(new DateTimeOffset(2026, 8, 4, 14, 0, 0, TimeSpan.Zero));
        var message = Message(clock.UtcNow);
        db.OutboxMessages.Add(message);
        await db.SaveChangesAsync();
        using var cancellation = new CancellationTokenSource();
        var delivery = new OutboxMessageDeliveryService(db, clock, new CancellingSink(cancellation));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => delivery.DeliverAsync(message.Id, cancellation.Token));

        var stored = await db.OutboxMessages.AsNoTracking().SingleAsync();
        Assert.Null(stored.ProcessingStartedAt);
        Assert.NotNull(stored.NextAttemptAt);
        Assert.Contains("cancel", stored.LastError!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Tenth_Failure_Should_Move_Message_To_Dead_Letter()
    {
        await using var connection = await OpenConnectionAsync();
        await using var db = CreateDbContext(connection);
        await db.Database.EnsureCreatedAsync();
        var clock = new MutableClock(new DateTimeOffset(2026, 8, 4, 14, 0, 0, TimeSpan.Zero));
        var message = Message(clock.UtcNow);
        message.Attempts = 9;
        db.OutboxMessages.Add(message);
        await db.SaveChangesAsync();

        var result = await new OutboxMessageDeliveryService(db, clock, new AlwaysFailSink()).DeliverAsync(message.Id);

        Assert.False(result.IsSuccess);
        Assert.False(result.IsRetryable);
        var stored = await db.OutboxMessages.AsNoTracking().SingleAsync();
        Assert.Equal(10, stored.Attempts);
        Assert.NotNull(stored.FailedAt);
        Assert.Null(stored.NextAttemptAt);
    }

    [Fact]
    public async Task Permanent_Handler_Error_Should_Fail_Without_Retry()
    {
        await using var connection = await OpenConnectionAsync();
        await using var db = CreateDbContext(connection);
        await db.Database.EnsureCreatedAsync();
        var clock = new MutableClock(new DateTimeOffset(2026, 8, 4, 14, 0, 0, TimeSpan.Zero));
        var message = Message(clock.UtcNow);
        db.OutboxMessages.Add(message);
        await db.SaveChangesAsync();

        var result = await new OutboxMessageDeliveryService(db, clock, new PermanentFailSink()).DeliverAsync(message.Id);

        Assert.False(result.IsSuccess);
        Assert.False(result.IsRetryable);
        var stored = await db.OutboxMessages.AsNoTracking().SingleAsync();
        Assert.Equal(1, stored.Attempts);
        Assert.NotNull(stored.FailedAt);
    }

    [Fact]
    public async Task Dispatchable_Query_Should_Return_Due_And_Stale_Only()
    {
        await using var connection = await OpenConnectionAsync();
        await using var db = CreateDbContext(connection);
        await db.Database.EnsureCreatedAsync();
        var clock = new MutableClock(new DateTimeOffset(2026, 8, 4, 14, 0, 0, TimeSpan.Zero));
        var due = Message(clock.UtcNow, "due");
        var scheduled = Message(clock.UtcNow, "scheduled");
        scheduled.NextAttemptAt = clock.UtcNow.AddMinutes(1);
        var stale = Message(clock.UtcNow.AddMinutes(-2), "stale");
        stale.ProcessingStartedAt = clock.UtcNow.AddMinutes(-2);
        var active = Message(clock.UtcNow, "active");
        active.ProcessingStartedAt = clock.UtcNow;
        var processed = Message(clock.UtcNow, "processed");
        processed.ProcessedAt = clock.UtcNow;
        var failed = Message(clock.UtcNow, "failed");
        failed.FailedAt = clock.UtcNow;
        db.OutboxMessages.AddRange(due, scheduled, stale, active, processed, failed);
        await db.SaveChangesAsync();

        var ids = await new OutboxMessageDeliveryService(db, clock, new RecordingSink()).GetDispatchableIdsAsync(20);

        Assert.Equal(new[] { due.Id, stale.Id }.OrderBy(x => x), ids.OrderBy(x => x));
    }

    [Fact]
    public async Task NotificationRequested_Should_Create_Pending_Email_Delivery()
    {
        await using var connection = await OpenConnectionAsync();
        await using var db = CreateDbContext(connection);
        await db.Database.EnsureCreatedAsync();
        var user = User();
        db.Users.Add(user);
        var message = Message(
            new DateTimeOffset(2026, 8, 4, 14, 0, 0, TimeSpan.Zero),
            payloadJson: JsonSerializer.Serialize(new { userId = user.Id, templateKey = "subscription_activated" }),
            type: "NotificationRequested");
        db.OutboxMessages.Add(message);
        await db.SaveChangesAsync();
        var clock = new MutableClock(message.CreatedAt);

        var result = await new OutboxMessageDeliveryService(db, clock, new LocalOutboxMessageSink(db)).DeliverAsync(message.Id);

        Assert.True(result.IsSuccess, result.Error);
        var notification = await db.NotificationDeliveries.AsNoTracking().SingleAsync();
        Assert.Equal(user.Id, notification.UserId);
        Assert.Equal(user.Email, notification.ToAddress);
        Assert.Equal("subscription_activated", notification.TemplateKey);
        Assert.Contains(message.Id.ToString(), notification.PayloadJson, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task PasswordResetRequested_Should_Create_Pending_Email_Delivery()
    {
        await using var connection = await OpenConnectionAsync();
        await using var db = CreateDbContext(connection);
        await db.Database.EnsureCreatedAsync();
        var user = User();
        db.Users.Add(user);
        var message = Message(
            new DateTimeOffset(2026, 8, 4, 14, 0, 0, TimeSpan.Zero),
            payloadJson: JsonSerializer.Serialize(new { userId = user.Id, email = user.Email, validationTokenReturned = false }),
            type: "password_reset_requested");
        db.OutboxMessages.Add(message);
        await db.SaveChangesAsync();

        var result = await new OutboxMessageDeliveryService(
            db,
            new MutableClock(message.CreatedAt),
            new LocalOutboxMessageSink(db)).DeliverAsync(message.Id);

        Assert.True(result.IsSuccess, result.Error);
        var notification = await db.NotificationDeliveries.AsNoTracking().SingleAsync();
        Assert.Equal(user.Id, notification.UserId);
        Assert.Equal(user.Email, notification.ToAddress);
        Assert.Equal("password_reset_requested", notification.TemplateKey);
        Assert.Contains(message.Id.ToString(), notification.PayloadJson, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task PaymentStatusChanged_Should_Validate_And_Complete_Without_Notification()
    {
        await using var connection = await OpenConnectionAsync();
        await using var db = CreateDbContext(connection);
        await db.Database.EnsureCreatedAsync();
        var now = new DateTimeOffset(2026, 8, 4, 14, 0, 0, TimeSpan.Zero);
        var message = Message(
            now,
            payloadJson: JsonSerializer.Serialize(new { paymentId = Guid.NewGuid(), orderId = Guid.NewGuid(), status = "Succeeded" }),
            type: "PaymentStatusChanged");
        db.OutboxMessages.Add(message);
        await db.SaveChangesAsync();

        var result = await new OutboxMessageDeliveryService(
            db,
            new MutableClock(now),
            new LocalOutboxMessageSink(db)).DeliverAsync(message.Id);

        Assert.True(result.IsSuccess, result.Error);
        Assert.NotNull((await db.OutboxMessages.AsNoTracking().SingleAsync()).ProcessedAt);
        Assert.Empty(await db.NotificationDeliveries.ToListAsync());
    }

    [Fact]
    public async Task Invalid_Local_Sink_Payload_Should_Be_Dead_Lettered()
    {
        await using var connection = await OpenConnectionAsync();
        await using var db = CreateDbContext(connection);
        await db.Database.EnsureCreatedAsync();
        var clock = new MutableClock(new DateTimeOffset(2026, 8, 4, 14, 0, 0, TimeSpan.Zero));
        var message = Message(clock.UtcNow, payloadJson: "{}", type: "NotificationRequested");
        db.OutboxMessages.Add(message);
        await db.SaveChangesAsync();

        var result = await new OutboxMessageDeliveryService(db, clock, new LocalOutboxMessageSink(db)).DeliverAsync(message.Id);

        Assert.False(result.IsSuccess);
        Assert.NotNull((await db.OutboxMessages.AsNoTracking().SingleAsync()).FailedAt);
        Assert.Empty(await db.NotificationDeliveries.ToListAsync());
    }

    [Fact]
    public async Task Unsupported_Event_Type_Should_Be_Dead_Lettered()
    {
        await using var connection = await OpenConnectionAsync();
        await using var db = CreateDbContext(connection);
        await db.Database.EnsureCreatedAsync();
        var clock = new MutableClock(new DateTimeOffset(2026, 8, 4, 14, 0, 0, TimeSpan.Zero));
        var message = Message(clock.UtcNow, payloadJson: "{}", type: "UnknownEvent");
        db.OutboxMessages.Add(message);
        await db.SaveChangesAsync();

        var result = await new OutboxMessageDeliveryService(db, clock, new LocalOutboxMessageSink(db)).DeliverAsync(message.Id);

        Assert.False(result.IsSuccess);
        var stored = await db.OutboxMessages.AsNoTracking().SingleAsync();
        Assert.NotNull(stored.FailedAt);
        Assert.Contains("Unsupported", stored.LastError!, StringComparison.OrdinalIgnoreCase);
    }

    private static OutboxMessage Message(
        DateTimeOffset now,
        string correlationId = "event-1",
        string? payloadJson = null,
        string type = "OrderTimelineEvent")
        => new()
        {
            Type = type,
            CorrelationId = correlationId,
            PayloadJson = payloadJson ?? JsonSerializer.Serialize(new { orderId = Guid.NewGuid(), eventType = "created" }),
            CreatedAt = now,
            UpdatedAt = now
        };

    private static User User()
        => new()
        {
            Email = "outbox@example.test",
            DisplayName = "Outbox User",
            PasswordHash = "hash",
            ReferralCode = Guid.NewGuid().ToString("N")
        };

    private static async Task<SqliteConnection> OpenConnectionAsync()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        return connection;
    }

    private static ApplicationDbContext CreateDbContext(string connectionString)
        => new(new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(connectionString).Options);

    private static ApplicationDbContext CreateDbContext(SqliteConnection connection)
        => new(new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(connection).Options);

    private sealed class MutableClock(DateTimeOffset utcNow) : IClock
    {
        public DateTimeOffset UtcNow { get; private set; } = utcNow;
        public void Advance(TimeSpan value) => UtcNow = UtcNow.Add(value);
    }

    private class RecordingSink : IOutboxMessageSink
    {
        public int Calls { get; protected set; }
        public virtual Task DispatchAsync(Guid messageId, string type, string correlationId, string payloadJson, CancellationToken cancellationToken)
        {
            Calls++;
            return Task.CompletedTask;
        }
    }

    private sealed class BlockingSink : RecordingSink
    {
        private readonly TaskCompletionSource _release = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource FirstCallStarted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public override async Task DispatchAsync(Guid messageId, string type, string correlationId, string payloadJson, CancellationToken cancellationToken)
        {
            Calls++;
            FirstCallStarted.TrySetResult();
            await _release.Task.WaitAsync(cancellationToken);
        }

        public void Release() => _release.TrySetResult();
    }

    private sealed class FailFirstSink : RecordingSink
    {
        public override Task DispatchAsync(Guid messageId, string type, string correlationId, string payloadJson, CancellationToken cancellationToken)
        {
            Calls++;
            return Calls == 1
                ? Task.FromException(new InvalidOperationException("token=super-secret"))
                : Task.CompletedTask;
        }
    }

    private sealed class AlwaysFailSink : IOutboxMessageSink
    {
        public Task DispatchAsync(Guid messageId, string type, string correlationId, string payloadJson, CancellationToken cancellationToken)
            => Task.FromException(new InvalidOperationException("temporary failure"));
    }

    private sealed class PermanentFailSink : IOutboxMessageSink
    {
        public Task DispatchAsync(Guid messageId, string type, string correlationId, string payloadJson, CancellationToken cancellationToken)
            => Task.FromException(new OutboxPermanentDispatchException("invalid payload"));
    }

    private sealed class CancellingSink(CancellationTokenSource cancellation) : IOutboxMessageSink
    {
        public Task DispatchAsync(Guid messageId, string type, string correlationId, string payloadJson, CancellationToken cancellationToken)
        {
            cancellation.Cancel();
            return Task.FromCanceled(cancellation.Token);
        }
    }
}
