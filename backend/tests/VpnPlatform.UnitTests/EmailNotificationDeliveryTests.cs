using System.Data.Common;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using VpnPlatform.Application.Abstractions;
using VpnPlatform.Application.Services;
using VpnPlatform.Domain.Entities;
using VpnPlatform.Domain.Enums;
using VpnPlatform.Infrastructure.Persistence;
using Xunit;

namespace VpnPlatform.UnitTests;

public sealed class EmailNotificationDeliveryTests
{
    [Fact]
    public async Task Password_Reset_Should_Decrypt_Code_And_Mark_Sent()
    {
        await using var connection = await OpenConnectionAsync();
        await using var db = CreateDbContext(connection);
        await db.Database.EnsureCreatedAsync();
        var now = new DateTimeOffset(2026, 8, 5, 9, 0, 0, TimeSpan.Zero);
        var user = User();
        var delivery = Delivery(user, now, "password_reset_requested", JsonSerializer.Serialize(new
        {
            protectedResetToken = "protected:reset-code-123",
            expiryMinutes = 30
        }));
        db.Users.Add(user);
        db.NotificationDeliveries.Add(delivery);
        await db.SaveChangesAsync();
        var sender = new RecordingSender();

        var result = await new EmailNotificationDeliveryService(db, new MutableClock(now), sender, new TestProtector())
            .DeliverAsync(delivery.Id);

        Assert.True(result.IsSuccess, result.Error);
        Assert.True(result.Value!.Delivered);
        var message = Assert.Single(sender.Messages);
        Assert.Equal(user.Email, message.ToAddress);
        Assert.Contains("reset-code-123", message.Body, StringComparison.Ordinal);
        var stored = await db.NotificationDeliveries.AsNoTracking().SingleAsync();
        Assert.Equal(NotificationDeliveryStatus.Sent, stored.Status);
        Assert.Equal(1, stored.Attempts);
        Assert.NotNull(stored.SentAt);
        Assert.Null(stored.ProcessingStartedAt);
    }

    [Fact]
    public async Task Transient_Failure_Should_Retry_With_Redacted_Backoff()
    {
        await using var connection = await OpenConnectionAsync();
        await using var db = CreateDbContext(connection);
        await db.Database.EnsureCreatedAsync();
        var clock = new MutableClock(new DateTimeOffset(2026, 8, 5, 9, 0, 0, TimeSpan.Zero));
        var user = User();
        var delivery = Delivery(user, clock.UtcNow);
        db.Users.Add(user);
        db.NotificationDeliveries.Add(delivery);
        await db.SaveChangesAsync();
        var sender = new FailFirstSender();
        var service = new EmailNotificationDeliveryService(db, clock, sender, new TestProtector());

        var first = await service.DeliverAsync(delivery.Id);

        Assert.False(first.IsSuccess);
        Assert.True(first.IsRetryable);
        var pending = await db.NotificationDeliveries.AsNoTracking().SingleAsync();
        Assert.Equal(NotificationDeliveryStatus.Pending, pending.Status);
        Assert.NotNull(pending.NextAttemptAt);
        Assert.Contains("REDACTED", pending.ErrorText!, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("super-secret", pending.ErrorText!, StringComparison.Ordinal);
        Assert.DoesNotContain(user.Email!, pending.ErrorText!, StringComparison.OrdinalIgnoreCase);
        Assert.False((await service.DeliverAsync(delivery.Id)).IsSuccess);
        Assert.Equal(1, sender.Calls);

        clock.Advance(TimeSpan.FromSeconds(11));
        Assert.True((await service.DeliverAsync(delivery.Id)).IsSuccess);
        Assert.Equal(2, sender.Calls);
    }

    [Fact]
    public async Task Password_Reset_Failure_Should_Redact_Code_Before_Error_Length_Limit()
    {
        await using var connection = await OpenConnectionAsync();
        await using var db = CreateDbContext(connection);
        await db.Database.EnsureCreatedAsync();
        var now = new DateTimeOffset(2026, 8, 14, 7, 0, 0, TimeSpan.Zero);
        var resetCode = "reset-code-" + new string('s', 64);
        var user = User();
        var delivery = Delivery(user, now, "password_reset_requested", JsonSerializer.Serialize(new
        {
            protectedResetToken = $"protected:{resetCode}",
            expiryMinutes = 30
        }));
        db.Users.Add(user);
        db.NotificationDeliveries.Add(delivery);
        await db.SaveChangesAsync();
        var sender = new ThrowingSender(new string('x', 470) + resetCode);

        var result = await new EmailNotificationDeliveryService(db, new MutableClock(now), sender, new TestProtector())
            .DeliverAsync(delivery.Id);

        Assert.False(result.IsSuccess);
        var stored = await db.NotificationDeliveries.AsNoTracking().SingleAsync();
        Assert.DoesNotContain(resetCode[..30], stored.ErrorText ?? string.Empty, StringComparison.Ordinal);
        Assert.Contains("REDACTED", stored.ErrorText ?? string.Empty, StringComparison.OrdinalIgnoreCase);
        Assert.True(stored.ErrorText?.Length <= 500);
    }

    [Fact]
    public async Task Dispatchable_Query_Should_Include_Due_And_Stale_Only()
    {
        await using var connection = await OpenConnectionAsync();
        var interceptor = new CommandCaptureInterceptor();
        await using var db = CreateDbContext(connection, interceptor);
        await db.Database.EnsureCreatedAsync();
        var clock = new MutableClock(new DateTimeOffset(2026, 8, 5, 9, 0, 0, TimeSpan.Zero));
        var user = User();
        var due = Delivery(user, clock.UtcNow);
        var scheduled = Delivery(user, clock.UtcNow);
        scheduled.NextAttemptAt = clock.UtcNow.AddMinutes(1);
        var stale = Delivery(user, clock.UtcNow.AddMinutes(-2));
        stale.ProcessingStartedAt = clock.UtcNow.AddMinutes(-2);
        var active = Delivery(user, clock.UtcNow);
        active.ProcessingStartedAt = clock.UtcNow;
        var sent = Delivery(user, clock.UtcNow);
        sent.Status = NotificationDeliveryStatus.Sent;
        db.Users.Add(user);
        db.NotificationDeliveries.AddRange(due, scheduled, stale, active, sent);
        await db.SaveChangesAsync();
        interceptor.Commands.Clear();

        var ids = await new EmailNotificationDeliveryService(db, clock, new RecordingSender(), new TestProtector())
            .GetDispatchableIdsAsync(20);

        Assert.Equal(new[] { due.Id, stale.Id }.OrderBy(x => x), ids.OrderBy(x => x));
        Assert.Contains(interceptor.Commands, command =>
            command.Contains("NotificationDeliveries", StringComparison.Ordinal) &&
            command.Contains("julianday", StringComparison.OrdinalIgnoreCase) &&
            command.Contains("LIMIT", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Missing_Template_Should_Fail_Permanently_Without_Leaking_Payload()
    {
        await using var connection = await OpenConnectionAsync();
        await using var db = CreateDbContext(connection);
        await db.Database.EnsureCreatedAsync();
        var now = new DateTimeOffset(2026, 8, 5, 9, 0, 0, TimeSpan.Zero);
        var user = User();
        var delivery = Delivery(user, now, "unknown-template", "{\"token\":\"payload-secret\"}");
        db.Users.Add(user);
        db.NotificationDeliveries.Add(delivery);
        await db.SaveChangesAsync();

        var result = await new EmailNotificationDeliveryService(db, new MutableClock(now), new RecordingSender(), new TestProtector())
            .DeliverAsync(delivery.Id);

        Assert.False(result.IsSuccess);
        var stored = await db.NotificationDeliveries.AsNoTracking().SingleAsync();
        Assert.Equal(NotificationDeliveryStatus.Failed, stored.Status);
        Assert.DoesNotContain("payload-secret", stored.ErrorText ?? string.Empty, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Deleted_Recipient_Should_Cancel_Without_Sending()
    {
        await using var connection = await OpenConnectionAsync();
        await using var db = CreateDbContext(connection);
        await db.Database.EnsureCreatedAsync();
        var now = new DateTimeOffset(2026, 8, 5, 9, 0, 0, TimeSpan.Zero);
        var user = User();
        user.Status = UserStatus.Deleted;
        var delivery = Delivery(user, now);
        db.Users.Add(user);
        db.NotificationDeliveries.Add(delivery);
        await db.SaveChangesAsync();
        var sender = new RecordingSender();

        var result = await new EmailNotificationDeliveryService(db, new MutableClock(now), sender, new TestProtector())
            .DeliverAsync(delivery.Id);

        Assert.True(result.IsSuccess, result.Error);
        Assert.Equal("cancelled", result.Value!.Status);
        Assert.Empty(sender.Messages);
        Assert.Equal(NotificationDeliveryStatus.Cancelled, (await db.NotificationDeliveries.AsNoTracking().SingleAsync()).Status);
    }

    private static NotificationDelivery Delivery(
        User user,
        DateTimeOffset now,
        string templateKey = "subscription_activated",
        string payloadJson = "{}")
        => new()
        {
            UserId = user.Id,
            SourceOutboxMessageId = Guid.NewGuid(),
            TemplateKey = templateKey,
            Channel = NotificationChannelType.Email,
            ToAddress = user.Email!,
            PayloadJson = payloadJson,
            CreatedAt = now,
            UpdatedAt = now
        };

    private static User User()
        => new()
        {
            Email = $"email-{Guid.NewGuid():N}@example.test",
            DisplayName = "Email User",
            PasswordHash = "hash",
            ReferralCode = Guid.NewGuid().ToString("N"),
            Status = UserStatus.Active
        };

    private static async Task<SqliteConnection> OpenConnectionAsync()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        return connection;
    }

    private static ApplicationDbContext CreateDbContext(SqliteConnection connection)
        => new(new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(connection).Options);

    private static ApplicationDbContext CreateDbContext(SqliteConnection connection, IInterceptor interceptor)
        => new(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(connection)
            .AddInterceptors(interceptor)
            .Options);

    private sealed class CommandCaptureInterceptor : DbCommandInterceptor
    {
        public List<string> Commands { get; } = [];

        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<DbDataReader> result,
            CancellationToken cancellationToken = default)
        {
            Commands.Add(command.CommandText);
            return ValueTask.FromResult(result);
        }
    }

    private sealed class MutableClock(DateTimeOffset utcNow) : IClock
    {
        public DateTimeOffset UtcNow { get; private set; } = utcNow;
        public void Advance(TimeSpan value) => UtcNow = UtcNow.Add(value);
    }

    private sealed class TestProtector : ISecretProtector
    {
        public string Protect(string plaintext) => "protected:" + plaintext;
        public string Unprotect(string protectedValue) => protectedValue.StartsWith("protected:", StringComparison.Ordinal)
            ? protectedValue[10..]
            : throw new InvalidOperationException("Invalid protected value.");
        public string Mask(string? value, int visibleTail = 4) => "***";
    }

    private sealed class RecordingSender : IEmailSender
    {
        public List<EmailMessage> Messages { get; } = [];
        public Task SendAsync(EmailMessage message, CancellationToken cancellationToken)
        {
            Messages.Add(message);
            return Task.CompletedTask;
        }
    }

    private sealed class FailFirstSender : IEmailSender
    {
        public int Calls { get; private set; }
        public Task SendAsync(EmailMessage message, CancellationToken cancellationToken)
        {
            Calls++;
            if (Calls == 1)
            {
                throw new InvalidOperationException($"token=super-secret smtp mailbox {message.ToAddress} unavailable");
            }

            return Task.CompletedTask;
        }
    }

    private sealed class ThrowingSender(string error) : IEmailSender
    {
        public Task SendAsync(EmailMessage message, CancellationToken cancellationToken)
            => throw new InvalidOperationException(error);
    }
}
