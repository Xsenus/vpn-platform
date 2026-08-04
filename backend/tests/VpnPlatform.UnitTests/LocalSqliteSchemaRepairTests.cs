using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using VpnPlatform.Domain.Entities;
using VpnPlatform.Infrastructure.Persistence;
using Xunit;

namespace VpnPlatform.UnitTests;

public class LocalSqliteSchemaRepairTests
{
    [Fact]
    public async Task ApplyAsync_Should_Add_Missing_PaymentProvider_WebhookUrl_To_Existing_Local_Sqlite_Db()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using (var command = connection.CreateCommand())
        {
            command.CommandText = """
            CREATE TABLE "PaymentProviderAccounts" (
                "Id" TEXT NOT NULL PRIMARY KEY,
                "Provider" INTEGER NOT NULL,
                "Mode" INTEGER NOT NULL,
                "Name" TEXT NOT NULL,
                "PublicName" TEXT NOT NULL,
                "IsEnabled" INTEGER NOT NULL,
                "IsDefault" INTEGER NOT NULL,
                "ShopId" TEXT NOT NULL,
                "ApiBaseUrl" TEXT NOT NULL,
                "ReturnUrl" TEXT NOT NULL,
                "SecretKeyProtected" TEXT NOT NULL,
                "WebhookSecretProtected" TEXT NOT NULL,
                "UseWebhookIpAllowList" INTEGER NOT NULL,
                "AllowedWebhookIpRangesCsv" TEXT NOT NULL,
                "ExtraSettingsJson" TEXT NOT NULL,
                "LastHealthCheckAt" TEXT NULL,
                "HealthStatus" INTEGER NOT NULL,
                "CreatedAt" TEXT NOT NULL,
                "UpdatedAt" TEXT NOT NULL
            );
            """;
            await command.ExecuteNonQueryAsync();
        }

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(connection)
            .Options;
        await using var db = new ApplicationDbContext(options);

        var repaired = await LocalSqliteSchemaRepair.ApplyAsync(db);

        Assert.Equal(1, repaired);
        Assert.True(await ColumnExistsAsync(connection, "PaymentProviderAccounts", "WebhookUrl"));
    }

    [Fact]
    public async Task ApplyAsync_Should_Be_Idempotent_When_Column_Already_Exists()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(connection)
            .Options;
        await using var db = new ApplicationDbContext(options);
        await db.Database.EnsureCreatedAsync();

        Assert.Equal(0, await LocalSqliteSchemaRepair.ApplyAsync(db));
        Assert.True(await ColumnExistsAsync(connection, "PaymentProviderAccounts", "WebhookUrl"));
    }

    [Fact]
    public async Task ApplyAsync_Should_Backfill_And_Cancel_Duplicate_Telegram_Notifications()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(connection)
            .Options;
        await using var db = new ApplicationDbContext(options);
        await db.Database.ExecuteSqlRawAsync(
            """
            CREATE TABLE "TelegramBotNotifications" (
                "Id" TEXT NOT NULL PRIMARY KEY,
                "TelegramUserId" INTEGER NOT NULL,
                "Type" TEXT NOT NULL,
                "PayloadJson" TEXT NOT NULL,
                "Status" TEXT NOT NULL,
                "AttemptCount" INTEGER NOT NULL,
                "NextAttemptAt" TEXT NULL,
                "SentAt" TEXT NULL,
                "ErrorText" TEXT NOT NULL,
                "CreatedAt" TEXT NOT NULL,
                "UpdatedAt" TEXT NOT NULL
            );
            """);

        var first = Notification();
        var duplicate = Notification();
        await db.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO "TelegramBotNotifications"
                ("Id", "TelegramUserId", "Type", "PayloadJson", "Status", "AttemptCount", "NextAttemptAt", "SentAt", "ErrorText", "CreatedAt", "UpdatedAt")
            VALUES
                ({first.Id}, {first.TelegramUserId}, {first.Type}, {first.PayloadJson}, {first.Status}, 0, {first.NextAttemptAt}, NULL, '', {first.CreatedAt}, {first.UpdatedAt}),
                ({duplicate.Id}, {duplicate.TelegramUserId}, {duplicate.Type}, {duplicate.PayloadJson}, {duplicate.Status}, 0, {duplicate.NextAttemptAt}, NULL, '', {duplicate.CreatedAt}, {duplicate.UpdatedAt});
            """);

        var repaired = await LocalSqliteSchemaRepair.ApplyAsync(db);

        Assert.Equal(2, repaired);
        Assert.True(await ColumnExistsAsync(connection, "TelegramBotNotifications", "DeduplicationKey"));
        var stored = await db.TelegramBotNotifications.AsNoTracking().OrderBy(x => x.Id).ToListAsync();
        Assert.Equal(2, stored.Count);
        Assert.Equal(2, stored.Select(x => x.DeduplicationKey).Distinct(StringComparer.Ordinal).Count());
        Assert.Single(stored, x => x.Status == "pending");
        Assert.Single(stored, x => x.Status == "cancelled");
        Assert.True(await IndexExistsAsync(connection, "IX_TelegramBotNotifications_DeduplicationKey"));
    }

    private static TelegramBotNotification Notification()
        => new()
        {
            TelegramUserId = 123456,
            Type = "subscription_expiring",
            PayloadJson = "{\"text\":\"Renew\"}",
            Status = "pending",
            NextAttemptAt = new DateTimeOffset(2026, 8, 4, 13, 0, 0, TimeSpan.Zero)
        };

    private static async Task<bool> ColumnExistsAsync(SqliteConnection connection, string tableName, string columnName)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = $"PRAGMA table_info(\"{tableName}\")";
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            if (string.Equals(reader["name"]?.ToString(), columnName, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static async Task<bool> IndexExistsAsync(SqliteConnection connection, string indexName)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(1) FROM sqlite_master WHERE type = 'index' AND name = $indexName";
        command.Parameters.AddWithValue("$indexName", indexName);
        return Convert.ToInt32(await command.ExecuteScalarAsync()) > 0;
    }
}
