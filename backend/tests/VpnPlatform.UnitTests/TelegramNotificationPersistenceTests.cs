using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using VpnPlatform.Application.Common;
using VpnPlatform.Domain.Entities;
using VpnPlatform.Infrastructure.Persistence;
using Xunit;

namespace VpnPlatform.UnitTests;

public class TelegramNotificationPersistenceTests
{
    [Fact]
    public void Deduplication_Key_Should_Match_PostgreSql_Migration_Vector()
    {
        var key = TelegramNotificationDeduplication.CreateKey(
            884422,
            "payment_succeeded",
            "{\"text\":\"Access is ready\",\"orderId\":\"same-order\"}");

        Assert.Equal("894dab9b508eae1148d219cf0e8f14a90d34a509ceec39416afd7f5ae39cc109", key);
    }

    [Fact]
    public async Task Concurrent_Equivalent_Notifications_Should_Persist_Once()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"vpn-platform-telegram-enqueue-{Guid.NewGuid():N}.db");
        var connectionString = $"Data Source={dbPath};Cache=Shared;Default Timeout=10";
        try
        {
            await using (var setup = CreateDbContext(connectionString))
            {
                await setup.Database.EnsureCreatedAsync();
            }

            await using var firstDb = CreateDbContext(connectionString);
            await using var secondDb = CreateDbContext(connectionString);
            firstDb.TelegramBotNotifications.Add(Notification());
            secondDb.TelegramBotNotifications.Add(Notification());

            await Task.WhenAll(
                firstDb.SaveChangesAsync(),
                secondDb.SaveChangesAsync());

            await using var inspectDb = CreateDbContext(connectionString);
            var stored = await inspectDb.TelegramBotNotifications.AsNoTracking().SingleAsync();
            Assert.Equal(
                TelegramNotificationDeduplication.CreateKey(stored.TelegramUserId, stored.Type, stored.PayloadJson),
                stored.DeduplicationKey);
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
    public async Task Equivalent_Notification_Should_Not_Requeue_Sent_Record()
    {
        await using var connection = await OpenConnectionAsync();
        await using var db = CreateDbContext(connection);
        await db.Database.EnsureCreatedAsync();
        var sentAt = new DateTimeOffset(2026, 8, 4, 13, 30, 0, TimeSpan.Zero);
        var sent = Notification("sent");
        sent.SentAt = sentAt;
        db.TelegramBotNotifications.Add(sent);
        await db.SaveChangesAsync();

        db.TelegramBotNotifications.Add(Notification());
        await db.SaveChangesAsync();

        var stored = await db.TelegramBotNotifications.AsNoTracking().SingleAsync();
        Assert.Equal(sent.Id, stored.Id);
        Assert.Equal("sent", stored.Status);
        Assert.Equal(sentAt, stored.SentAt);
    }

    [Theory]
    [InlineData("failed")]
    [InlineData("cancelled")]
    public async Task Equivalent_Notification_Should_Revive_Terminal_Record(string status)
    {
        await using var connection = await OpenConnectionAsync();
        await using var db = CreateDbContext(connection);
        await db.Database.EnsureCreatedAsync();
        var terminal = Notification(status);
        terminal.AttemptCount = 5;
        terminal.ErrorText = "old error";
        terminal.NextAttemptAt = null;
        db.TelegramBotNotifications.Add(terminal);
        await db.SaveChangesAsync();

        var replacement = Notification();
        replacement.NextAttemptAt = terminal.CreatedAt.AddMinutes(5);
        db.TelegramBotNotifications.Add(replacement);
        await db.SaveChangesAsync();

        var stored = await db.TelegramBotNotifications.AsNoTracking().SingleAsync();
        Assert.Equal(terminal.Id, stored.Id);
        Assert.Equal("pending", stored.Status);
        Assert.Equal(0, stored.AttemptCount);
        Assert.Equal(replacement.NextAttemptAt, stored.NextAttemptAt);
        Assert.Null(stored.SentAt);
        Assert.Empty(stored.ErrorText);
    }

    [Fact]
    public async Task Notification_Upsert_Should_Roll_Back_When_Business_Save_Fails()
    {
        await using var connection = await OpenConnectionAsync();
        await using var db = CreateDbContext(connection);
        await db.Database.EnsureCreatedAsync();
        db.Users.Add(User("duplicate@example.test", "first-referral"));
        await db.SaveChangesAsync();

        var duplicateUser = User("duplicate@example.test", "second-referral");
        var notification = Notification();
        db.Users.Add(duplicateUser);
        db.TelegramBotNotifications.Add(notification);

        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());

        await using var inspectDb = CreateDbContext(connection);
        Assert.Equal(0, await inspectDb.TelegramBotNotifications.CountAsync());
        Assert.Equal(1, await inspectDb.Users.CountAsync());
        Assert.Equal(EntityState.Added, db.Entry(notification).State);

        db.Entry(duplicateUser).State = EntityState.Detached;
        await db.SaveChangesAsync();
        Assert.Equal(1, await inspectDb.TelegramBotNotifications.CountAsync());
    }

    [Fact]
    public async Task Duplicate_Notifications_In_One_Unit_Of_Work_Should_Collapse()
    {
        await using var connection = await OpenConnectionAsync();
        await using var db = CreateDbContext(connection);
        await db.Database.EnsureCreatedAsync();
        db.TelegramBotNotifications.AddRange(Notification(), Notification());

        await db.SaveChangesAsync();

        Assert.Equal(1, await db.TelegramBotNotifications.CountAsync());
    }

    private static TelegramBotNotification Notification(string status = "pending")
        => new()
        {
            TelegramUserId = 884422,
            Type = "payment_succeeded",
            PayloadJson = "{\"text\":\"Access is ready\",\"orderId\":\"same-order\"}",
            Status = status,
            NextAttemptAt = new DateTimeOffset(2026, 8, 4, 13, 0, 0, TimeSpan.Zero)
        };

    private static User User(string email, string referralCode)
        => new()
        {
            Email = email,
            DisplayName = email,
            PasswordHash = "hash",
            ReferralCode = referralCode
        };

    private static async Task<SqliteConnection> OpenConnectionAsync()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        return connection;
    }

    private static ApplicationDbContext CreateDbContext(string connectionString)
        => new(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(connectionString)
            .Options);

    private static ApplicationDbContext CreateDbContext(SqliteConnection connection)
        => new(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(connection)
            .Options);
}
