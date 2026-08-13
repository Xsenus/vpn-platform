using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using VpnPlatform.Domain.Entities;
using VpnPlatform.Domain.Enums;
using VpnPlatform.Infrastructure.Persistence;
using Xunit;

namespace VpnPlatform.UnitTests;

public class LocalSqliteSchemaRepairTests
{
    [Fact]
    public async Task ApplyAsync_Should_Add_Missing_User_Session_Columns_To_Existing_Local_Sqlite_Db()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(connection)
            .Options;
        await using var db = new ApplicationDbContext(options);
        await db.Database.ExecuteSqlRawAsync(
            """
            CREATE TABLE "Users" (
                "Id" TEXT NOT NULL PRIMARY KEY,
                "Email" TEXT NULL
            );
            CREATE TABLE "UserRefreshTokens" (
                "Id" TEXT NOT NULL PRIMARY KEY,
                "UserId" TEXT NOT NULL,
                "TokenHash" TEXT NOT NULL
            );
            CREATE TABLE "PasswordResetTokens" (
                "Id" TEXT NOT NULL PRIMARY KEY,
                "UserId" TEXT NOT NULL,
                "TokenHash" TEXT NOT NULL
            );
            """);

        var repaired = await LocalSqliteSchemaRepair.ApplyAsync(db);

        Assert.Equal(11, repaired);
        Assert.True(await ColumnExistsAsync(connection, "Users", "SessionVersion"));
        Assert.True(await IndexIsUniqueAsync(connection, "TelegramLinkStates", "IX_TelegramLinkStates_UserId"));
        Assert.True(await ColumnExistsAsync(connection, "UserRefreshTokens", "SessionVersion"));
        Assert.True(await ColumnExistsAsync(connection, "UserRefreshTokens", "FamilyId"));
        Assert.True(await ColumnExistsAsync(connection, "UserRefreshTokens", "Revision"));
        Assert.True(await IndexExistsAsync(connection, "IX_UserRefreshTokens_UserId_SessionVersion_FamilyId"));
        Assert.True(await ColumnExistsAsync(connection, "PasswordResetTokens", "InvalidatedAt"));
        Assert.True(await ColumnExistsAsync(connection, "PasswordResetTokens", "InvalidationReason"));
        Assert.True(await ColumnExistsAsync(connection, "PasswordResetTokens", "Revision"));
        Assert.True(await ColumnExistsAsync(connection, "PasswordResetTokens", "Generation"));
        Assert.True(await IndexIsUniqueAsync(connection, "PasswordResetStates", "IX_PasswordResetStates_UserId"));
        Assert.Equal(0, await LocalSqliteSchemaRepair.ApplyAsync(db));
    }

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

        Assert.Equal(2, repaired);
        Assert.True(await ColumnExistsAsync(connection, "PaymentProviderAccounts", "WebhookUrl"));
        Assert.True(await IndexIsUniqueAsync(connection, "PaymentProviderAccounts", "IX_PaymentProviderAccounts_Provider"));
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

        Assert.Equal(0, await LocalSqliteSchemaRepair.PrepareMigrationsAsync(db));
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

        Assert.Equal(6, repaired);
        Assert.True(await ColumnExistsAsync(connection, "ProvisioningRuns", "Revision"));
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
    public async Task ApplyAsync_Should_Preserve_Oldest_Queued_Provisioning_Run_Across_Mixed_Offsets()
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
        var olderId = Guid.NewGuid();
        var newerId = Guid.NewGuid();
        var olderInstant = new DateTimeOffset(2026, 8, 4, 10, 0, 0, TimeSpan.FromHours(5));
        var newerInstant = new DateTimeOffset(2026, 8, 4, 8, 0, 0, TimeSpan.Zero);
        await db.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO "ProvisioningRuns"
                ("Id", "CreatedAt", "UpdatedAt", "NodeId", "Status", "RequestedByUserId", "DryRun", "StartedAt", "FinishedAt", "ExecutionLog")
            VALUES
                ({olderId}, {olderInstant}, {olderInstant}, {nodeId}, 8, NULL, 1, {olderInstant}, NULL, 'older'),
                ({newerId}, {newerInstant}, {newerInstant}, {nodeId}, 8, NULL, 1, {newerInstant}, NULL, 'newer');
            """);

        Assert.Equal(6, await LocalSqliteSchemaRepair.ApplyAsync(db));

        var runs = await db.ProvisioningRuns.AsNoTracking().ToDictionaryAsync(x => x.Id);
        Assert.Equal(ProvisioningRunStatus.PrecheckQueued, runs[olderId].Status);
        Assert.Equal(ProvisioningRunStatus.PrecheckFailed, runs[newerId].Status);
        Assert.Contains("quarantined", runs[newerId].LastError, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task PrepareMigrationsAsync_Should_Preserve_Oldest_Outbox_Duplicate_Across_Mixed_Offsets()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(connection)
            .Options;
        await using var db = new ApplicationDbContext(options);
        var migrator = db.GetService<IMigrator>();
        await db.Database.ExecuteSqlRawAsync(
            """
            CREATE TABLE "OutboxMessages" (
                "Id" TEXT NOT NULL PRIMARY KEY,
                "CreatedAt" TEXT NOT NULL,
                "UpdatedAt" TEXT NOT NULL,
                "Type" TEXT NOT NULL,
                "PayloadJson" TEXT NOT NULL,
                "CorrelationId" TEXT NOT NULL,
                "Attempts" INTEGER NOT NULL,
                "ProcessedAt" TEXT NULL,
                "LastError" TEXT NULL
            );
            CREATE INDEX "IX_OutboxMessages_Type_CorrelationId"
                ON "OutboxMessages" ("Type", "CorrelationId");
            """);
        await MarkMigrationsBeforeAsync(db, "20260804131342_OutboxDispatchRecovery");

        var olderId = Guid.NewGuid();
        var newerId = Guid.NewGuid();
        var olderInstant = new DateTimeOffset(2026, 8, 4, 10, 0, 0, TimeSpan.FromHours(5));
        var newerInstant = new DateTimeOffset(2026, 8, 4, 8, 0, 0, TimeSpan.Zero);
        var payloadJson = "{}";
        await db.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO "OutboxMessages"
                ("Id", "CreatedAt", "UpdatedAt", "Type", "PayloadJson", "CorrelationId", "Attempts", "ProcessedAt", "LastError")
            VALUES
                ({olderId}, {olderInstant}, {olderInstant}, 'NotificationRequested', {payloadJson}, 'mixed-offset', 0, NULL, NULL),
                ({newerId}, {newerInstant}, {newerInstant}, 'NotificationRequested', {payloadJson}, 'mixed-offset', 0, NULL, NULL);
            """);

        Assert.Equal(1, await LocalSqliteSchemaRepair.PrepareMigrationsAsync(db));
        await migrator.MigrateAsync("20260804131342_OutboxDispatchRecovery");

        var messages = await db.OutboxMessages.AsNoTracking().ToDictionaryAsync(x => x.Id);
        Assert.Equal("mixed-offset", messages[olderId].CorrelationId);
        Assert.Null(messages[olderId].FailedAt);
        Assert.StartsWith("legacy:", messages[newerId].CorrelationId, StringComparison.Ordinal);
        Assert.NotNull(messages[newerId].FailedAt);
    }

    [Fact]
    public async Task PrepareMigrationsAsync_Should_Fail_Closed_On_Invalid_Duplicate_Timestamp()
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
                "CorrelationId" TEXT NOT NULL,
                "CreatedAt" TEXT NOT NULL
            );
            CREATE INDEX "IX_OutboxMessages_Type_CorrelationId"
                ON "OutboxMessages" ("Type", "CorrelationId");
            INSERT INTO "OutboxMessages" ("Id", "Type", "CorrelationId", "CreatedAt")
            VALUES
                ('11111111-1111-1111-1111-111111111111', 'NotificationRequested', 'invalid-time', 'not-a-timestamp'),
                ('22222222-2222-2222-2222-222222222222', 'NotificationRequested', 'invalid-time', '2026-08-04T08:00:00.0000000+00:00');
            """);

        var error = await Assert.ThrowsAsync<InvalidOperationException>(
            () => LocalSqliteSchemaRepair.PrepareMigrationsAsync(db));

        Assert.Contains("invalid timestamp", error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task PrepareMigrationsAsync_Should_Preserve_Oldest_Queued_Provisioning_Run_Across_Mixed_Offsets()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(connection)
            .Options;
        await using var db = new ApplicationDbContext(options);
        var migrator = db.GetService<IMigrator>();
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
        await MarkMigrationsBeforeAsync(db, "20260804134818_ProvisioningWorkerRecovery");

        var nodeId = Guid.NewGuid();
        var olderId = Guid.NewGuid();
        var newerId = Guid.NewGuid();
        var olderInstant = new DateTimeOffset(2026, 8, 4, 10, 0, 0, TimeSpan.FromHours(5));
        var newerInstant = new DateTimeOffset(2026, 8, 4, 8, 0, 0, TimeSpan.Zero);
        await db.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO "ProvisioningRuns"
                ("Id", "CreatedAt", "UpdatedAt", "NodeId", "Status", "RequestedByUserId", "DryRun", "StartedAt", "FinishedAt", "ExecutionLog")
            VALUES
                ({olderId}, {olderInstant}, {olderInstant}, {nodeId}, 8, NULL, 1, {olderInstant}, NULL, 'older'),
                ({newerId}, {newerInstant}, {newerInstant}, {nodeId}, 8, NULL, 1, {newerInstant}, NULL, 'newer');
            """);

        Assert.Equal(1, await LocalSqliteSchemaRepair.PrepareMigrationsAsync(db));
        await migrator.MigrateAsync("20260804134818_ProvisioningWorkerRecovery");

        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT "Id", "Status", "LastError"
            FROM "ProvisioningRuns"
            ORDER BY "Id";
            """;
        await using var reader = await command.ExecuteReaderAsync();
        var statuses = new Dictionary<Guid, (long Status, string? LastError)>();
        while (await reader.ReadAsync())
        {
            statuses[Guid.Parse(reader.GetString(0))] = (reader.GetInt64(1), reader.IsDBNull(2) ? null : reader.GetString(2));
        }

        Assert.Equal((long)ProvisioningRunStatus.PrecheckQueued, statuses[olderId].Status);
        Assert.Null(statuses[olderId].LastError);
        Assert.Equal((long)ProvisioningRunStatus.PrecheckFailed, statuses[newerId].Status);
        Assert.Contains("quarantined", statuses[newerId].LastError, StringComparison.OrdinalIgnoreCase);
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

    [Fact]
    public async Task ApplyAsync_Should_Quarantine_Duplicate_Running_Panel_Sync_And_Create_Unique_Index()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(connection)
            .Options;
        await using var db = new ApplicationDbContext(options);
        await db.Database.ExecuteSqlRawAsync(
            """
            CREATE TABLE "PanelSyncRuns" (
                "Id" TEXT NOT NULL PRIMARY KEY,
                "VpnPanelId" TEXT NOT NULL,
                "Status" INTEGER NOT NULL,
                "StartedAt" TEXT NOT NULL,
                "FinishedAt" TEXT NULL,
                "SummaryJson" TEXT NOT NULL,
                "ErrorMessage" TEXT NOT NULL,
                "CreatedAt" TEXT NOT NULL,
                "UpdatedAt" TEXT NOT NULL
            );
            """);
        var panelId = Guid.NewGuid();
        var firstId = Guid.NewGuid();
        var duplicateId = Guid.NewGuid();
        var historicalFailureId = Guid.NewGuid();
        var olderInstant = new DateTimeOffset(2026, 8, 4, 10, 0, 0, TimeSpan.FromHours(5));
        var newerInstant = new DateTimeOffset(2026, 8, 4, 8, 0, 0, TimeSpan.Zero);
        var summaryJson = "{}";
        await db.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO "PanelSyncRuns"
                ("Id", "VpnPanelId", "Status", "StartedAt", "FinishedAt", "SummaryJson", "ErrorMessage", "CreatedAt", "UpdatedAt")
            VALUES
                ({firstId}, {panelId}, 1, {olderInstant}, NULL, {summaryJson}, '', {olderInstant}, {olderInstant}),
                ({duplicateId}, {panelId}, 1, {newerInstant}, NULL, {summaryJson}, '', {newerInstant}, {newerInstant}),
                ({historicalFailureId}, {panelId}, 3, {newerInstant.AddMinutes(1)}, {newerInstant.AddMinutes(1)}, {summaryJson}, 'password=legacy-secret', {newerInstant.AddMinutes(1)}, {newerInstant.AddMinutes(1)});
            """);

        Assert.Equal(1, await LocalSqliteSchemaRepair.PrepareMigrationsAsync(db));
        var preparedRuns = await db.PanelSyncRuns.AsNoTracking().ToDictionaryAsync(x => x.Id);
        Assert.Equal(PanelSyncRunStatus.Running, preparedRuns[firstId].Status);
        Assert.Equal(PanelSyncRunStatus.Failed, preparedRuns[duplicateId].Status);
        Assert.Contains("quarantined", preparedRuns[duplicateId].ErrorMessage, StringComparison.OrdinalIgnoreCase);
        var repaired = await LocalSqliteSchemaRepair.ApplyAsync(db);

        Assert.Equal(1, repaired);
        Assert.True(await IndexIsUniqueAsync(connection, "PanelSyncRuns", "IX_PanelSyncRuns_Running_VpnPanelId"));
        var runs = await db.PanelSyncRuns.AsNoTracking().ToDictionaryAsync(x => x.Id);
        Assert.Equal(PanelSyncRunStatus.Running, runs[firstId].Status);
        Assert.Equal(PanelSyncRunStatus.Failed, runs[duplicateId].Status);
        Assert.NotNull(runs[duplicateId].FinishedAt);
        Assert.Contains("redacted", runs[duplicateId].ErrorMessage, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("legacy-secret", runs[historicalFailureId].ErrorMessage, StringComparison.Ordinal);
        Assert.Contains("redacted", runs[historicalFailureId].ErrorMessage, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(0, await LocalSqliteSchemaRepair.ApplyAsync(db));
    }

    [Fact]
    public async Task ApplyAsync_Should_Repair_Telegram_Link_Lifecycle_And_Duplicate_Accounts()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(connection)
            .Options;
        await using var db = new ApplicationDbContext(options);
        await db.Database.ExecuteSqlRawAsync(
            """
            CREATE TABLE "Users" (
                "Id" TEXT NOT NULL PRIMARY KEY,
                "SessionVersion" INTEGER NOT NULL DEFAULT 0,
                "CreatedAt" TEXT NOT NULL,
                "UpdatedAt" TEXT NOT NULL
            );
            CREATE TABLE "TelegramBotDeepLinks" (
                "Id" TEXT NOT NULL PRIMARY KEY,
                "UserId" TEXT NULL,
                "TokenHash" TEXT NOT NULL,
                "Purpose" TEXT NOT NULL,
                "ExpiresAt" TEXT NOT NULL,
                "UsedAt" TEXT NULL,
                "UsedByTelegramUserId" INTEGER NULL,
                "MetadataJson" TEXT NOT NULL,
                "CreatedAt" TEXT NOT NULL,
                "UpdatedAt" TEXT NOT NULL
            );
            CREATE TABLE "TelegramAccounts" (
                "Id" TEXT NOT NULL PRIMARY KEY,
                "UserId" TEXT NULL,
                "TelegramUserId" INTEGER NOT NULL,
                "LinkedAt" TEXT NULL,
                "CreatedAt" TEXT NOT NULL,
                "UpdatedAt" TEXT NOT NULL
            );
            CREATE INDEX "IX_TelegramAccounts_UserId" ON "TelegramAccounts" ("UserId");
            """);
        var userId = Guid.NewGuid();
        var oldAccountId = Guid.NewGuid();
        var currentAccountId = Guid.NewGuid();
        var linkId = Guid.NewGuid();
        var now = new DateTimeOffset(2026, 8, 5, 9, 0, 0, TimeSpan.Zero);
        var olderInstant = new DateTimeOffset(2026, 8, 5, 10, 0, 0, TimeSpan.FromHours(5));
        var newerInstant = new DateTimeOffset(2026, 8, 5, 8, 0, 0, TimeSpan.Zero);
        var metadataJson = "{}";
        await db.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO "Users" ("Id", "SessionVersion", "CreatedAt", "UpdatedAt")
            VALUES ({userId}, 0, {now}, {now});
            INSERT INTO "TelegramBotDeepLinks"
                ("Id", "UserId", "TokenHash", "Purpose", "ExpiresAt", "UsedAt", "UsedByTelegramUserId", "MetadataJson", "CreatedAt", "UpdatedAt")
            VALUES ({linkId}, {userId}, 'legacy-link-hash', 'link_account', {now.AddMinutes(10)}, NULL, NULL, {metadataJson}, {now}, {now});
            INSERT INTO "TelegramAccounts" ("Id", "UserId", "TelegramUserId", "LinkedAt", "CreatedAt", "UpdatedAt")
            VALUES
                ({oldAccountId}, {userId}, 777301, {olderInstant}, {olderInstant}, {olderInstant}),
                ({currentAccountId}, {userId}, 777302, {newerInstant}, {newerInstant}, {newerInstant});
            """);

        Assert.Equal(1, await LocalSqliteSchemaRepair.PrepareMigrationsAsync(db));
        var repaired = await LocalSqliteSchemaRepair.ApplyAsync(db);

        Assert.Equal(7, repaired);
        Assert.True(await IndexIsUniqueAsync(connection, "TelegramLinkStates", "IX_TelegramLinkStates_UserId"));
        Assert.True(await ColumnExistsAsync(connection, "TelegramBotDeepLinks", "Generation"));
        Assert.True(await ColumnExistsAsync(connection, "TelegramBotDeepLinks", "InvalidatedAt"));
        Assert.True(await ColumnExistsAsync(connection, "TelegramBotDeepLinks", "InvalidationReason"));
        Assert.True(await ColumnExistsAsync(connection, "TelegramBotDeepLinks", "Revision"));
        Assert.True(await IndexIsUniqueAsync(connection, "TelegramAccounts", "IX_TelegramAccounts_UserId"));

        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT
                (SELECT "Generation" FROM "TelegramLinkStates" LIMIT 1),
                (SELECT "Revision" FROM "TelegramLinkStates" LIMIT 1),
                (SELECT "InvalidationReason" FROM "TelegramBotDeepLinks" LIMIT 1),
                (SELECT "Revision" FROM "TelegramBotDeepLinks" LIMIT 1),
                (SELECT COUNT(*) FROM "TelegramAccounts" WHERE "UserId" IS NOT NULL),
                (SELECT "TelegramUserId" FROM "TelegramAccounts" WHERE "UserId" IS NOT NULL LIMIT 1);
            """;
        await using var reader = await command.ExecuteReaderAsync();
        Assert.True(await reader.ReadAsync());
        Assert.Equal(1L, reader.GetInt64(0));
        Assert.Equal(1L, reader.GetInt64(1));
        Assert.Equal("telegram_link_lifecycle_migration", reader.GetString(2));
        Assert.Equal(1L, reader.GetInt64(3));
        Assert.Equal(1L, reader.GetInt64(4));
        Assert.Equal(777302L, reader.GetInt64(5));
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

    [Fact]
    public async Task ApplyAsync_Should_Add_Pending_Order_Intent_Constraint_To_Legacy_Sqlite()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(connection).Options;
        await using var db = new ApplicationDbContext(options);
        await db.Database.ExecuteSqlRawAsync(
            """
            CREATE TABLE "Orders" (
                "Id" TEXT NOT NULL PRIMARY KEY,
                "Status" INTEGER NOT NULL
            );
            """);

        var repaired = await LocalSqliteSchemaRepair.ApplyAsync(db);

        Assert.Equal(2, repaired);
        Assert.True(await ColumnExistsAsync(connection, "Orders", "PendingIntentKey"));
        Assert.True(await IndexIsUniqueAsync(connection, "Orders", "IX_Orders_Pending_IntentKey"));
        Assert.Equal(0, await LocalSqliteSchemaRepair.ApplyAsync(db));
    }

    [Fact]
    public async Task ApplyAsync_Should_Add_Support_Revision_To_Legacy_Sqlite()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(connection).Options;
        await using var db = new ApplicationDbContext(options);
        await db.Database.ExecuteSqlRawAsync(
            """
            CREATE TABLE "SupportConversations" (
                "Id" TEXT NOT NULL PRIMARY KEY,
                "Status" TEXT NOT NULL
            );
            """);

        var repaired = await LocalSqliteSchemaRepair.ApplyAsync(db);

        Assert.Equal(1, repaired);
        Assert.True(await ColumnExistsAsync(connection, "SupportConversations", "Revision"));
        Assert.Equal(0, await LocalSqliteSchemaRepair.ApplyAsync(db));
    }

    private static OutboxMessage OutboxMessage(string correlationId)
        => new()
        {
            Type = "NotificationRequested",
            CorrelationId = correlationId,
            PayloadJson = "{}",
            CreatedAt = new DateTimeOffset(2026, 8, 4, 13, 0, 0, TimeSpan.Zero),
            UpdatedAt = new DateTimeOffset(2026, 8, 4, 13, 0, 0, TimeSpan.Zero)
        };

    private static async Task MarkMigrationsBeforeAsync(ApplicationDbContext db, string targetMigration)
    {
        await db.Database.ExecuteSqlRawAsync(
            """
            CREATE TABLE "__EFMigrationsHistory" (
                "MigrationId" TEXT NOT NULL CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY,
                "ProductVersion" TEXT NOT NULL
            );
            """);
        foreach (var migration in db.Database.GetMigrations().TakeWhile(x => x != targetMigration))
        {
            await db.Database.ExecuteSqlInterpolatedAsync($"""
                INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
                VALUES ({migration}, '9.0.16');
                """);
        }
    }

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
