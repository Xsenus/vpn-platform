using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using VpnPlatform.Domain.Entities;
using VpnPlatform.Domain.Enums;
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

    [Fact]
    public async Task ApplyAsync_Should_Upgrade_Outbox_And_Quarantine_Duplicate_Pending_Message()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(connection)
            .Options;
        await using var db = new ApplicationDbContext(options);
        await db.Database.ExecuteSqlRawAsync(
            """
            CREATE TABLE "OutboxMessages" (
                "Id" TEXT NOT NULL PRIMARY KEY,
                "Type" TEXT NOT NULL,
                "PayloadJson" TEXT NOT NULL,
                "CorrelationId" TEXT NOT NULL,
                "Attempts" INTEGER NOT NULL,
                "ProcessedAt" TEXT NULL,
                "LastError" TEXT NULL,
                "CreatedAt" TEXT NOT NULL,
                "UpdatedAt" TEXT NOT NULL
            );
            CREATE INDEX "IX_OutboxMessages_Type_CorrelationId"
                ON "OutboxMessages" ("Type", "CorrelationId");
            """);
        var first = OutboxMessage("duplicate-event");
        var duplicate = OutboxMessage("duplicate-event");
        await db.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO "OutboxMessages"
                ("Id", "Type", "PayloadJson", "CorrelationId", "Attempts", "ProcessedAt", "LastError", "CreatedAt", "UpdatedAt")
            VALUES
                ({first.Id}, {first.Type}, {first.PayloadJson}, {first.CorrelationId}, 0, NULL, NULL, {first.CreatedAt}, {first.UpdatedAt}),
                ({duplicate.Id}, {duplicate.Type}, {duplicate.PayloadJson}, {duplicate.CorrelationId}, 0, NULL, NULL, {duplicate.CreatedAt}, {duplicate.UpdatedAt});
            """);

        var repaired = await LocalSqliteSchemaRepair.ApplyAsync(db);

        Assert.Equal(4, repaired);
        Assert.True(await ColumnExistsAsync(connection, "OutboxMessages", "ProcessingStartedAt"));
        Assert.True(await ColumnExistsAsync(connection, "OutboxMessages", "NextAttemptAt"));
        Assert.True(await ColumnExistsAsync(connection, "OutboxMessages", "FailedAt"));
        Assert.True(await IndexIsUniqueAsync(connection, "IX_OutboxMessages_Type_CorrelationId"));
        var stored = await db.OutboxMessages.AsNoTracking().OrderBy(x => x.Id).ToListAsync();
        Assert.Equal(2, stored.Select(x => x.CorrelationId).Distinct(StringComparer.Ordinal).Count());
        Assert.Single(stored, x => x.FailedAt == null);
        Assert.Single(stored, x => x.FailedAt != null);
    }

    [Fact]
    public async Task ApplyAsync_Should_Upgrade_ProvisioningRuns_And_Quarantine_Duplicate_Active_Run()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(connection)
            .Options;
        await using var db = new ApplicationDbContext(options);
        await db.Database.ExecuteSqlRawAsync(
            """
            CREATE TABLE "ProvisioningRuns" (
                "Id" TEXT NOT NULL PRIMARY KEY,
                "CreatedAt" TEXT NOT NULL,
                "UpdatedAt" TEXT NOT NULL,
                "NodeId" TEXT NOT NULL,
                "Status" INTEGER NOT NULL,
                "RequestedByUserId" TEXT NULL,
                "DryRun" INTEGER NOT NULL,
                "StartedAt" TEXT NOT NULL,
                "FinishedAt" TEXT NULL,
                "ExecutionLog" TEXT NOT NULL
            );
            """);
        var nodeId = Guid.NewGuid();
        var processingId = Guid.NewGuid();
        var duplicateId = Guid.NewGuid();
        var createdAt = new DateTimeOffset(2026, 8, 4, 12, 0, 0, TimeSpan.Zero);
        await db.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO "ProvisioningRuns"
                ("Id", "CreatedAt", "UpdatedAt", "NodeId", "Status", "RequestedByUserId", "DryRun", "StartedAt", "FinishedAt", "ExecutionLog")
            VALUES
                ({processingId}, {createdAt}, {createdAt}, {nodeId}, 9, NULL, 1, {createdAt}, NULL, 'processing'),
                ({duplicateId}, {createdAt.AddMinutes(1)}, {createdAt.AddMinutes(1)}, {nodeId}, 8, NULL, 1, {createdAt.AddMinutes(1)}, NULL, 'queued');
            """);

        var repaired = await LocalSqliteSchemaRepair.ApplyAsync(db);

        Assert.Equal(5, repaired);
        Assert.True(await ColumnExistsAsync(connection, "ProvisioningRuns", "AttemptCount"));
        Assert.True(await ColumnExistsAsync(connection, "ProvisioningRuns", "ProcessingStartedAt"));
        Assert.True(await ColumnExistsAsync(connection, "ProvisioningRuns", "LeaseExpiresAt"));
        Assert.True(await ColumnExistsAsync(connection, "ProvisioningRuns", "LastError"));
        Assert.True(await IndexIsUniqueAsync(connection, "ProvisioningRuns", "IX_ProvisioningRuns_Active_NodeId"));
        var stored = (await db.ProvisioningRuns.AsNoTracking().ToListAsync()).OrderBy(x => x.CreatedAt).ToList();
        Assert.Equal(ProvisioningRunStatus.Prechecking, stored[0].Status);
        Assert.Equal(ProvisioningRunStatus.PrecheckFailed, stored[1].Status);
        Assert.Contains("quarantined", stored[1].LastError, StringComparison.OrdinalIgnoreCase);
        Assert.NotNull(stored[1].FinishedAt);
    }

    [Fact]
    public async Task ApplyAsync_Should_Add_Subscription_Lifecycle_Recovery_Columns()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(connection)
            .Options;
        await using var db = new ApplicationDbContext(options);
        await db.Database.ExecuteSqlRawAsync(
            """
            CREATE TABLE "Subscriptions" (
                "Id" TEXT NOT NULL PRIMARY KEY,
                "Status" INTEGER NOT NULL,
                "CreatedAt" TEXT NOT NULL,
                "UpdatedAt" TEXT NOT NULL
            );
            """);

        var repaired = await LocalSqliteSchemaRepair.ApplyAsync(db);

        Assert.Equal(5, repaired);
        Assert.True(await ColumnExistsAsync(connection, "Subscriptions", "LifecycleAttemptCount"));
        Assert.True(await ColumnExistsAsync(connection, "Subscriptions", "LifecycleProcessingStartedAt"));
        Assert.True(await ColumnExistsAsync(connection, "Subscriptions", "LifecycleLeaseExpiresAt"));
        Assert.True(await ColumnExistsAsync(connection, "Subscriptions", "LifecycleNextAttemptAt"));
        Assert.True(await ColumnExistsAsync(connection, "Subscriptions", "LifecycleLastError"));
        Assert.Equal(0, await LocalSqliteSchemaRepair.ApplyAsync(db));
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

    private static OutboxMessage OutboxMessage(string correlationId)
        => new()
        {
            Type = "NotificationRequested",
            CorrelationId = correlationId,
            PayloadJson = "{}",
            CreatedAt = new DateTimeOffset(2026, 8, 4, 13, 0, 0, TimeSpan.Zero),
            UpdatedAt = new DateTimeOffset(2026, 8, 4, 13, 0, 0, TimeSpan.Zero)
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

    private static Task<bool> IndexIsUniqueAsync(SqliteConnection connection, string indexName)
        => IndexIsUniqueAsync(connection, "OutboxMessages", indexName);

    private static async Task<bool> IndexIsUniqueAsync(SqliteConnection connection, string tableName, string indexName)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = $"PRAGMA index_list(\"{tableName}\")";
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            if (string.Equals(reader["name"]?.ToString(), indexName, StringComparison.OrdinalIgnoreCase))
            {
                return Convert.ToInt32(reader["unique"]) == 1;
            }
        }

        return false;
    }
}
