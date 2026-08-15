using System.Data;
using System.Globalization;
using Microsoft.EntityFrameworkCore;
using VpnPlatform.Application.Common;

namespace VpnPlatform.Infrastructure.Persistence;

public static class LocalSqliteSchemaRepair
{
    public static async Task<int> PrepareMigrationsAsync(
        ApplicationDbContext db,
        DateTimeOffset repairAt,
        CancellationToken cancellationToken = default)
    {
        if (!db.Database.IsSqlite())
        {
            return 0;
        }

        var prepared = 0;
        if (await TableExistsAsync(db, "OutboxMessages", cancellationToken)
            && !await IndexIsUniqueAsync(db, "OutboxMessages", "IX_OutboxMessages_Type_CorrelationId", cancellationToken)
            && await NormalizeDuplicateOutboxCreatedAtAsync(db, cancellationToken))
        {
            prepared++;
        }

        if (await TableExistsAsync(db, "ProvisioningRuns", cancellationToken)
            && !await IndexIsUniqueAsync(db, "ProvisioningRuns", "IX_ProvisioningRuns_Active_NodeId", cancellationToken)
            && await NormalizeDuplicateActiveProvisioningCreatedAtAsync(db, cancellationToken))
        {
            prepared++;
        }

        if (await TableExistsAsync(db, "TelegramAccounts", cancellationToken)
            && !await IndexIsUniqueAsync(db, "TelegramAccounts", "IX_TelegramAccounts_UserId", cancellationToken))
        {
            await BackfillDuplicateTelegramAccountLinksAsync(db, repairAt, cancellationToken);
            prepared++;
        }

        if (await TableExistsAsync(db, "PanelSyncRuns", cancellationToken)
            && !await IndexIsUniqueAsync(db, "PanelSyncRuns", "IX_PanelSyncRuns_Running_VpnPanelId", cancellationToken))
        {
            await BackfillDuplicateRunningPanelSyncRunsAsync(db, repairAt, cancellationToken);
            prepared++;
        }

        if (await TableExistsAsync(db, "PaymentProviderAccounts", cancellationToken)
            && !await IndexIsUniqueAsync(db, "PaymentProviderAccounts", "IX_PaymentProviderAccounts_Provider", cancellationToken))
        {
            await BackfillDuplicatePaymentProviderDefaultsAsync(db, cancellationToken);
            prepared++;
        }

        return prepared;
    }

    public static async Task<int> ApplyAsync(
        ApplicationDbContext db,
        DateTimeOffset repairAt,
        CancellationToken cancellationToken = default)
    {
        if (!db.Database.IsSqlite())
        {
            return 0;
        }

        var repaired = 0;
        if (await TableExistsAsync(db, "Orders", cancellationToken))
        {
            if (!await ColumnExistsAsync(db, "Orders", "PendingIntentKey", cancellationToken))
            {
                await db.Database.ExecuteSqlRawAsync(
                    """ALTER TABLE "Orders" ADD COLUMN "PendingIntentKey" TEXT NULL;""",
                    cancellationToken);
                repaired++;
            }

            if (!await IndexIsUniqueAsync(db, "Orders", "IX_Orders_Pending_IntentKey", cancellationToken))
            {
                await db.Database.ExecuteSqlRawAsync(
                    """CREATE UNIQUE INDEX "IX_Orders_Pending_IntentKey" ON "Orders" ("PendingIntentKey") WHERE "Status" = 1 AND "PendingIntentKey" IS NOT NULL;""",
                    cancellationToken);
                repaired++;
            }
        }

        foreach (var (table, sql) in new[]
                 {
                     ("ReferralPrograms", """ALTER TABLE "ReferralPrograms" ADD COLUMN "Revision" INTEGER NOT NULL DEFAULT 0;"""),
                     ("AppReleases", """ALTER TABLE "AppReleases" ADD COLUMN "Revision" INTEGER NOT NULL DEFAULT 0;"""),
                     ("FaqEntries", """ALTER TABLE "FaqEntries" ADD COLUMN "Revision" INTEGER NOT NULL DEFAULT 0;"""),
                     ("SiteContentBlocks", """ALTER TABLE "SiteContentBlocks" ADD COLUMN "Revision" INTEGER NOT NULL DEFAULT 0;"""),
                     ("WorkScenarios", """ALTER TABLE "WorkScenarios" ADD COLUMN "Revision" INTEGER NOT NULL DEFAULT 0;"""),
                     ("Tariffs", """ALTER TABLE "Tariffs" ADD COLUMN "Revision" INTEGER NOT NULL DEFAULT 0;"""),
                     ("VpnNodes", """ALTER TABLE "VpnNodes" ADD COLUMN "Revision" INTEGER NOT NULL DEFAULT 0;"""),
                     ("VpnPanels", """ALTER TABLE "VpnPanels" ADD COLUMN "Revision" INTEGER NOT NULL DEFAULT 0;"""),
                     ("VpnInbounds", """ALTER TABLE "VpnInbounds" ADD COLUMN "Revision" INTEGER NOT NULL DEFAULT 0;"""),
                     ("VpnClients", """ALTER TABLE "VpnClients" ADD COLUMN "Revision" INTEGER NOT NULL DEFAULT 0;""")
                 })
        {
            if (!await TableExistsAsync(db, table, cancellationToken)
                || await ColumnExistsAsync(db, table, "Revision", cancellationToken))
            {
                continue;
            }

            await db.Database.ExecuteSqlRawAsync(sql, cancellationToken);
            repaired++;
        }

        if (await TableExistsAsync(db, "SupportConversations", cancellationToken)
            && !await ColumnExistsAsync(db, "SupportConversations", "Revision", cancellationToken))
        {
            await db.Database.ExecuteSqlRawAsync(
                """ALTER TABLE "SupportConversations" ADD COLUMN "Revision" INTEGER NOT NULL DEFAULT 0;""",
                cancellationToken);
            repaired++;
        }

        if (await TableExistsAsync(db, "Users", cancellationToken)
            && !await ColumnExistsAsync(db, "Users", "SessionVersion", cancellationToken))
        {
            await db.Database.ExecuteSqlRawAsync(
                """ALTER TABLE "Users" ADD COLUMN "SessionVersion" INTEGER NOT NULL DEFAULT 0;""",
                cancellationToken);
            repaired++;
        }

        if (await TableExistsAsync(db, "UserRefreshTokens", cancellationToken)
            && !await ColumnExistsAsync(db, "UserRefreshTokens", "SessionVersion", cancellationToken))
        {
            await db.Database.ExecuteSqlRawAsync(
                """ALTER TABLE "UserRefreshTokens" ADD COLUMN "SessionVersion" INTEGER NOT NULL DEFAULT 0;""",
                cancellationToken);
            repaired++;
        }

        if (await TableExistsAsync(db, "UserRefreshTokens", cancellationToken)
            && !await ColumnExistsAsync(db, "UserRefreshTokens", "FamilyId", cancellationToken))
        {
            await db.Database.ExecuteSqlRawAsync(
                """ALTER TABLE "UserRefreshTokens" ADD COLUMN "FamilyId" TEXT NULL;""",
                cancellationToken);
            repaired++;
        }

        if (await TableExistsAsync(db, "UserRefreshTokens", cancellationToken)
            && !await ColumnExistsAsync(db, "UserRefreshTokens", "Revision", cancellationToken))
        {
            await db.Database.ExecuteSqlRawAsync(
                """ALTER TABLE "UserRefreshTokens" ADD COLUMN "Revision" INTEGER NOT NULL DEFAULT 0;""",
                cancellationToken);
            repaired++;
        }

        if (await TableExistsAsync(db, "UserRefreshTokens", cancellationToken)
            && !await IndexExistsAsync(db, "IX_UserRefreshTokens_UserId_SessionVersion_FamilyId", cancellationToken))
        {
            await db.Database.ExecuteSqlRawAsync(
                """CREATE INDEX "IX_UserRefreshTokens_UserId_SessionVersion_FamilyId" ON "UserRefreshTokens" ("UserId", "SessionVersion", "FamilyId");""",
                cancellationToken);
            repaired++;
        }

        if (await TableExistsAsync(db, "PasswordResetTokens", cancellationToken)
            && !await ColumnExistsAsync(db, "PasswordResetTokens", "InvalidatedAt", cancellationToken))
        {
            await db.Database.ExecuteSqlRawAsync(
                """ALTER TABLE "PasswordResetTokens" ADD COLUMN "InvalidatedAt" TEXT NULL;""",
                cancellationToken);
            repaired++;
        }

        if (await TableExistsAsync(db, "PasswordResetTokens", cancellationToken)
            && !await ColumnExistsAsync(db, "PasswordResetTokens", "InvalidationReason", cancellationToken))
        {
            await db.Database.ExecuteSqlRawAsync(
                """ALTER TABLE "PasswordResetTokens" ADD COLUMN "InvalidationReason" TEXT NOT NULL DEFAULT '';""",
                cancellationToken);
            repaired++;
        }

        if (await TableExistsAsync(db, "PasswordResetTokens", cancellationToken)
            && !await ColumnExistsAsync(db, "PasswordResetTokens", "Revision", cancellationToken))
        {
            await db.Database.ExecuteSqlRawAsync(
                """ALTER TABLE "PasswordResetTokens" ADD COLUMN "Revision" INTEGER NOT NULL DEFAULT 0;""",
                cancellationToken);
            repaired++;
        }

        if (await TableExistsAsync(db, "PasswordResetTokens", cancellationToken)
            && !await ColumnExistsAsync(db, "PasswordResetTokens", "Generation", cancellationToken))
        {
            await db.Database.ExecuteSqlRawAsync(
                """ALTER TABLE "PasswordResetTokens" ADD COLUMN "Generation" INTEGER NOT NULL DEFAULT 0;""",
                cancellationToken);
            repaired++;
        }

        if (await TableExistsAsync(db, "Users", cancellationToken)
            && !await TableExistsAsync(db, "PasswordResetStates", cancellationToken))
        {
            await db.Database.ExecuteSqlRawAsync(
                """
                CREATE TABLE "PasswordResetStates" (
                    "Id" TEXT NOT NULL CONSTRAINT "PK_PasswordResetStates" PRIMARY KEY,
                    "UserId" TEXT NOT NULL,
                    "Generation" INTEGER NOT NULL DEFAULT 0,
                    "Revision" INTEGER NOT NULL DEFAULT 0,
                    "CreatedAt" TEXT NOT NULL,
                    "UpdatedAt" TEXT NOT NULL,
                    CONSTRAINT "FK_PasswordResetStates_Users_UserId" FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE CASCADE
                );
                CREATE UNIQUE INDEX "IX_PasswordResetStates_UserId" ON "PasswordResetStates" ("UserId");
                """,
                cancellationToken);
            repaired++;
        }

        if (await TableExistsAsync(db, "PasswordResetStates", cancellationToken)
            && !await IndexExistsAsync(db, "IX_PasswordResetStates_UserId", cancellationToken))
        {
            await db.Database.ExecuteSqlRawAsync(
                """CREATE UNIQUE INDEX "IX_PasswordResetStates_UserId" ON "PasswordResetStates" ("UserId");""",
                cancellationToken);
            repaired++;
        }

        var telegramLinkLifecycleUpgraded = false;
        if (await TableExistsAsync(db, "Users", cancellationToken)
            && !await TableExistsAsync(db, "TelegramLinkStates", cancellationToken))
        {
            await db.Database.ExecuteSqlRawAsync(
                """
                CREATE TABLE "TelegramLinkStates" (
                    "Id" TEXT NOT NULL CONSTRAINT "PK_TelegramLinkStates" PRIMARY KEY,
                    "UserId" TEXT NOT NULL,
                    "Generation" INTEGER NOT NULL DEFAULT 0,
                    "Revision" INTEGER NOT NULL DEFAULT 0,
                    "CreatedAt" TEXT NOT NULL,
                    "UpdatedAt" TEXT NOT NULL,
                    CONSTRAINT "FK_TelegramLinkStates_Users_UserId" FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE CASCADE
                );
                CREATE UNIQUE INDEX "IX_TelegramLinkStates_UserId" ON "TelegramLinkStates" ("UserId");
                """,
                cancellationToken);
            telegramLinkLifecycleUpgraded = true;
            repaired++;
        }

        if (await TableExistsAsync(db, "TelegramBotDeepLinks", cancellationToken))
        {
            foreach (var (column, sql) in new[]
                     {
                         ("Generation", """ALTER TABLE "TelegramBotDeepLinks" ADD COLUMN "Generation" INTEGER NOT NULL DEFAULT 0;"""),
                         ("InvalidatedAt", """ALTER TABLE "TelegramBotDeepLinks" ADD COLUMN "InvalidatedAt" TEXT NULL;"""),
                         ("InvalidationReason", """ALTER TABLE "TelegramBotDeepLinks" ADD COLUMN "InvalidationReason" TEXT NOT NULL DEFAULT '';"""),
                         ("Revision", """ALTER TABLE "TelegramBotDeepLinks" ADD COLUMN "Revision" INTEGER NOT NULL DEFAULT 0;""")
                     })
            {
                if (await ColumnExistsAsync(db, "TelegramBotDeepLinks", column, cancellationToken))
                {
                    continue;
                }

                await db.Database.ExecuteSqlRawAsync(sql, cancellationToken);
                telegramLinkLifecycleUpgraded = true;
                repaired++;
            }
        }

        if (telegramLinkLifecycleUpgraded
            && await TableExistsAsync(db, "Users", cancellationToken)
            && await TableExistsAsync(db, "TelegramBotDeepLinks", cancellationToken)
            && await TableExistsAsync(db, "TelegramLinkStates", cancellationToken))
        {
            await db.Database.ExecuteSqlInterpolatedAsync(
                $"""
                UPDATE "TelegramBotDeepLinks"
                SET
                    "InvalidatedAt" = {repairAt},
                    "InvalidationReason" = 'telegram_link_lifecycle_migration',
                    "Revision" = 1,
                    "UpdatedAt" = {repairAt}
                WHERE "Purpose" = 'link_account' AND "UsedAt" IS NULL;

                INSERT OR IGNORE INTO "TelegramLinkStates"
                    ("Id", "UserId", "Generation", "Revision", "CreatedAt", "UpdatedAt")
                SELECT
                    "UserId", "UserId", 1, 1, {repairAt}, {repairAt}
                FROM "TelegramBotDeepLinks"
                WHERE "UserId" IS NOT NULL
                GROUP BY "UserId";
                """,
                cancellationToken);
        }

        if (await TableExistsAsync(db, "TelegramAccounts", cancellationToken)
            && !await IndexIsUniqueAsync(db, "TelegramAccounts", "IX_TelegramAccounts_UserId", cancellationToken))
        {
            await BackfillDuplicateTelegramAccountLinksAsync(db, repairAt, cancellationToken);
            await db.Database.ExecuteSqlRawAsync(
                """DROP INDEX IF EXISTS "IX_TelegramAccounts_UserId";""",
                cancellationToken);
            await db.Database.ExecuteSqlRawAsync(
                """CREATE UNIQUE INDEX "IX_TelegramAccounts_UserId" ON "TelegramAccounts" ("UserId") WHERE "UserId" IS NOT NULL;""",
                cancellationToken);
            repaired++;
        }

        if (await TableExistsAsync(db, "PaymentProviderAccounts", cancellationToken)
            && !await ColumnExistsAsync(db, "PaymentProviderAccounts", "WebhookUrl", cancellationToken))
        {
            await db.Database.ExecuteSqlRawAsync(
                """ALTER TABLE "PaymentProviderAccounts" ADD COLUMN "WebhookUrl" TEXT NOT NULL DEFAULT '';""",
                cancellationToken);
            repaired++;
        }

        if (await TableExistsAsync(db, "PaymentProviderAccounts", cancellationToken)
            && !await IndexIsUniqueAsync(db, "PaymentProviderAccounts", "IX_PaymentProviderAccounts_Provider", cancellationToken))
        {
            await BackfillDuplicatePaymentProviderDefaultsAsync(db, cancellationToken);
            await db.Database.ExecuteSqlRawAsync(
                """DROP INDEX IF EXISTS "IX_PaymentProviderAccounts_Provider";""",
                cancellationToken);
            await db.Database.ExecuteSqlRawAsync(
                """CREATE UNIQUE INDEX "IX_PaymentProviderAccounts_Provider" ON "PaymentProviderAccounts" ("Provider") WHERE "IsDefault" = 1;""",
                cancellationToken);
            repaired++;
        }

        if (await TableExistsAsync(db, "TelegramBotNotifications", cancellationToken))
        {
            if (!await ColumnExistsAsync(db, "TelegramBotNotifications", "DeduplicationKey", cancellationToken))
            {
                await db.Database.ExecuteSqlRawAsync(
                    """ALTER TABLE "TelegramBotNotifications" ADD COLUMN "DeduplicationKey" TEXT NOT NULL DEFAULT '';""",
                    cancellationToken);
                repaired++;
            }

            await BackfillTelegramNotificationDeduplicationKeysAsync(db, cancellationToken);
            if (!await IndexExistsAsync(db, "IX_TelegramBotNotifications_DeduplicationKey", cancellationToken))
            {
                await db.Database.ExecuteSqlRawAsync(
                    """CREATE UNIQUE INDEX "IX_TelegramBotNotifications_DeduplicationKey" ON "TelegramBotNotifications" ("DeduplicationKey");""",
                    cancellationToken);
                repaired++;
            }
        }

        if (await TableExistsAsync(db, "OutboxMessages", cancellationToken))
        {
            foreach (var (column, sql) in new[]
                     {
                         ("ProcessingStartedAt", """ALTER TABLE "OutboxMessages" ADD COLUMN "ProcessingStartedAt" TEXT NULL;"""),
                         ("NextAttemptAt", """ALTER TABLE "OutboxMessages" ADD COLUMN "NextAttemptAt" TEXT NULL;"""),
                         ("FailedAt", """ALTER TABLE "OutboxMessages" ADD COLUMN "FailedAt" TEXT NULL;""")
                     })
            {
                if (await ColumnExistsAsync(db, "OutboxMessages", column, cancellationToken))
                {
                    continue;
                }

                await db.Database.ExecuteSqlRawAsync(sql, cancellationToken);
                repaired++;
            }

            if (!await IndexIsUniqueAsync(db, "OutboxMessages", "IX_OutboxMessages_Type_CorrelationId", cancellationToken))
            {
                await BackfillOutboxDuplicateCorrelationsAsync(db, repairAt, cancellationToken);
                await db.Database.ExecuteSqlRawAsync(
                    """DROP INDEX IF EXISTS "IX_OutboxMessages_Type_CorrelationId";""",
                    cancellationToken);
                await db.Database.ExecuteSqlRawAsync(
                    """CREATE UNIQUE INDEX "IX_OutboxMessages_Type_CorrelationId" ON "OutboxMessages" ("Type", "CorrelationId");""",
                    cancellationToken);
                repaired++;
            }
        }

        if (await TableExistsAsync(db, "NotificationDeliveries", cancellationToken))
        {
            foreach (var (column, sql) in new[]
                     {
                         ("SourceOutboxMessageId", """ALTER TABLE "NotificationDeliveries" ADD COLUMN "SourceOutboxMessageId" TEXT NULL;"""),
                         ("ProcessingStartedAt", """ALTER TABLE "NotificationDeliveries" ADD COLUMN "ProcessingStartedAt" TEXT NULL;"""),
                         ("NextAttemptAt", """ALTER TABLE "NotificationDeliveries" ADD COLUMN "NextAttemptAt" TEXT NULL;""")
                     })
            {
                if (await ColumnExistsAsync(db, "NotificationDeliveries", column, cancellationToken))
                {
                    continue;
                }

                await db.Database.ExecuteSqlRawAsync(sql, cancellationToken);
                repaired++;
            }

            if (!await IndexExistsAsync(db, "IX_NotificationDeliveries_SourceOutboxMessageId", cancellationToken))
            {
                await db.Database.ExecuteSqlRawAsync(
                    """CREATE UNIQUE INDEX "IX_NotificationDeliveries_SourceOutboxMessageId" ON "NotificationDeliveries" ("SourceOutboxMessageId");""",
                    cancellationToken);
                repaired++;
            }

            if (!await IndexExistsAsync(db, "IX_NotificationDeliveries_Status_NextAttemptAt", cancellationToken))
            {
                await db.Database.ExecuteSqlRawAsync(
                    """CREATE INDEX "IX_NotificationDeliveries_Status_NextAttemptAt" ON "NotificationDeliveries" ("Status", "NextAttemptAt");""",
                    cancellationToken);
                repaired++;
            }
        }

        if (await TableExistsAsync(db, "ProvisioningRuns", cancellationToken))
        {
            foreach (var (column, sql) in new[]
                     {
                         ("Revision", """ALTER TABLE "ProvisioningRuns" ADD COLUMN "Revision" INTEGER NOT NULL DEFAULT 0;"""),
                         ("AttemptCount", """ALTER TABLE "ProvisioningRuns" ADD COLUMN "AttemptCount" INTEGER NOT NULL DEFAULT 0;"""),
                         ("ProcessingStartedAt", """ALTER TABLE "ProvisioningRuns" ADD COLUMN "ProcessingStartedAt" TEXT NULL;"""),
                         ("LeaseExpiresAt", """ALTER TABLE "ProvisioningRuns" ADD COLUMN "LeaseExpiresAt" TEXT NULL;"""),
                         ("LastError", """ALTER TABLE "ProvisioningRuns" ADD COLUMN "LastError" TEXT NULL;""")
                     })
            {
                if (await ColumnExistsAsync(db, "ProvisioningRuns", column, cancellationToken))
                {
                    continue;
                }

                await db.Database.ExecuteSqlRawAsync(sql, cancellationToken);
                repaired++;
            }

            if (!await IndexIsUniqueAsync(db, "ProvisioningRuns", "IX_ProvisioningRuns_Active_NodeId", cancellationToken))
            {
                await BackfillDuplicateActiveProvisioningRunsAsync(db, repairAt, cancellationToken);
                await db.Database.ExecuteSqlRawAsync(
                    """DROP INDEX IF EXISTS "IX_ProvisioningRuns_Active_NodeId";""",
                    cancellationToken);
                await db.Database.ExecuteSqlRawAsync(
                    """CREATE UNIQUE INDEX "IX_ProvisioningRuns_Active_NodeId" ON "ProvisioningRuns" ("NodeId") WHERE "Status" IN (0, 1, 8, 9, 12, 13, 15);""",
                    cancellationToken);
                repaired++;
            }
        }

        if (await TableExistsAsync(db, "Subscriptions", cancellationToken))
        {
            foreach (var (column, sql) in new[]
                     {
                         ("LifecycleAttemptCount", """ALTER TABLE "Subscriptions" ADD COLUMN "LifecycleAttemptCount" INTEGER NOT NULL DEFAULT 0;"""),
                         ("LifecycleProcessingStartedAt", """ALTER TABLE "Subscriptions" ADD COLUMN "LifecycleProcessingStartedAt" TEXT NULL;"""),
                         ("LifecycleLeaseExpiresAt", """ALTER TABLE "Subscriptions" ADD COLUMN "LifecycleLeaseExpiresAt" TEXT NULL;"""),
                         ("LifecycleNextAttemptAt", """ALTER TABLE "Subscriptions" ADD COLUMN "LifecycleNextAttemptAt" TEXT NULL;"""),
                         ("LifecycleLastError", """ALTER TABLE "Subscriptions" ADD COLUMN "LifecycleLastError" TEXT NULL;""")
                     })
            {
                if (await ColumnExistsAsync(db, "Subscriptions", column, cancellationToken))
                {
                    continue;
                }

                await db.Database.ExecuteSqlRawAsync(sql, cancellationToken);
                repaired++;
            }
        }

        if (await TableExistsAsync(db, "PanelSyncRuns", cancellationToken)
            && !await IndexIsUniqueAsync(db, "PanelSyncRuns", "IX_PanelSyncRuns_Running_VpnPanelId", cancellationToken))
        {
            await RedactLegacyPanelErrorsAsync(db, cancellationToken);
            await BackfillDuplicateRunningPanelSyncRunsAsync(db, repairAt, cancellationToken);
            await db.Database.ExecuteSqlRawAsync(
                """DROP INDEX IF EXISTS "IX_PanelSyncRuns_Running_VpnPanelId";""",
                cancellationToken);
            await db.Database.ExecuteSqlRawAsync(
                """CREATE UNIQUE INDEX "IX_PanelSyncRuns_Running_VpnPanelId" ON "PanelSyncRuns" ("VpnPanelId") WHERE "Status" = 1;""",
                cancellationToken);
            repaired++;
        }

        return repaired;
    }

    private static async Task BackfillDuplicateTelegramAccountLinksAsync(
        ApplicationDbContext db,
        DateTimeOffset repairAt,
        CancellationToken cancellationToken)
    {
        await db.Database.ExecuteSqlInterpolatedAsync(
            $"""
            CREATE TEMP TABLE "__DuplicateTelegramAccountLinks" AS
            SELECT "Id" FROM (
                SELECT
                    "Id",
                    row_number() OVER (
                        PARTITION BY "UserId"
                        ORDER BY ("LinkedAt" IS NULL), julianday("LinkedAt") DESC, julianday("UpdatedAt") DESC, julianday("CreatedAt") DESC, "Id") AS link_rank
                FROM "TelegramAccounts"
                WHERE "UserId" IS NOT NULL
            ) AS ranked
            WHERE link_rank > 1;

            UPDATE "TelegramAccounts"
            SET
                "UserId" = NULL,
                "LinkedAt" = NULL,
                "UpdatedAt" = {repairAt}
            WHERE "Id" IN (SELECT "Id" FROM "__DuplicateTelegramAccountLinks");

            DROP TABLE "__DuplicateTelegramAccountLinks";
            """,
            cancellationToken);
    }

    private static async Task BackfillDuplicateRunningPanelSyncRunsAsync(
        ApplicationDbContext db,
        DateTimeOffset repairAt,
        CancellationToken cancellationToken)
    {
        await db.Database.ExecuteSqlInterpolatedAsync(
            $"""
            CREATE TEMP TABLE "__DuplicateRunningPanelSyncRuns" AS
            SELECT "Id" FROM (
                SELECT
                    "Id",
                    row_number() OVER (
                        PARTITION BY "VpnPanelId"
                        ORDER BY julianday("StartedAt"), julianday("CreatedAt"), "Id") AS running_rank
                FROM "PanelSyncRuns"
                WHERE "Status" = 1
            ) AS ranked
            WHERE running_rank > 1;

            UPDATE "PanelSyncRuns"
            SET
                "Status" = 3,
                "FinishedAt" = {repairAt},
                "ErrorMessage" = 'Duplicate running panel sync quarantined during local schema repair.',
                "UpdatedAt" = {repairAt}
            WHERE "Id" IN (SELECT "Id" FROM "__DuplicateRunningPanelSyncRuns");

            DROP TABLE "__DuplicateRunningPanelSyncRuns";
            """,
            cancellationToken);
    }

    private static async Task BackfillDuplicatePaymentProviderDefaultsAsync(
        ApplicationDbContext db,
        CancellationToken cancellationToken)
    {
        await db.Database.ExecuteSqlRawAsync(
            """
            WITH ranked_defaults AS (
                SELECT
                    "Id",
                    row_number() OVER (
                        PARTITION BY "Provider"
                        ORDER BY julianday("UpdatedAt") DESC, julianday("CreatedAt") DESC, "Id") AS default_rank
                FROM "PaymentProviderAccounts"
                WHERE "IsDefault" = 1
            )
            UPDATE "PaymentProviderAccounts"
            SET "IsDefault" = 0
            WHERE "Id" IN (
                SELECT "Id"
                FROM ranked_defaults
                WHERE default_rank > 1
            );
            """,
            cancellationToken);
    }

    private static async Task RedactLegacyPanelErrorsAsync(
        ApplicationDbContext db,
        CancellationToken cancellationToken)
    {
        await db.Database.ExecuteSqlRawAsync(
            """
            UPDATE "PanelSyncRuns"
            SET "ErrorMessage" = 'Historical panel sync error redacted during local schema repair.'
            WHERE "ErrorMessage" <> '';
            """,
            cancellationToken);

        if (await TableExistsAsync(db, "VpnPanels", cancellationToken))
        {
            await db.Database.ExecuteSqlRawAsync(
                """
                UPDATE "VpnPanels"
                SET "LastError" = 'Historical panel error redacted during local schema repair.'
                WHERE "LastError" <> '';
                """,
                cancellationToken);
        }

        if (await TableExistsAsync(db, "PanelHealthChecks", cancellationToken))
        {
            await db.Database.ExecuteSqlRawAsync(
                """
                UPDATE "PanelHealthChecks"
                SET "ErrorMessage" = 'Historical panel health error redacted during local schema repair.'
                WHERE "ErrorMessage" <> '';
                """,
                cancellationToken);
        }
    }

    private static async Task BackfillDuplicateActiveProvisioningRunsAsync(
        ApplicationDbContext db,
        DateTimeOffset repairAt,
        CancellationToken cancellationToken)
    {
        await db.Database.ExecuteSqlInterpolatedAsync(
            $"""
            CREATE TEMP TABLE "__DuplicateActiveProvisioningRuns" AS
            SELECT "Id" FROM (
                SELECT
                    "Id",
                    row_number() OVER (
                        PARTITION BY "NodeId"
                        ORDER BY
                            CASE WHEN "Status" IN (1, 9, 13) THEN 0 ELSE 1 END,
                            julianday("CreatedAt"),
                            "Id") AS active_rank
                FROM "ProvisioningRuns"
                WHERE "Status" IN (0, 1, 8, 9, 12, 13, 15)
            ) AS ranked
            WHERE active_rank > 1;

            UPDATE "ProvisioningRuns"
            SET
                "Status" = CASE WHEN "DryRun" THEN 10 ELSE 3 END,
                "ProcessingStartedAt" = NULL,
                "LeaseExpiresAt" = NULL,
                "FinishedAt" = {repairAt},
                "LastError" = 'Duplicate active provisioning run quarantined during local schema repair.',
                "ExecutionLog" = substr(CASE
                    WHEN "ExecutionLog" = '' THEN 'Duplicate active provisioning run quarantined during local schema repair.'
                    ELSE "ExecutionLog" || char(10) || 'Duplicate active provisioning run quarantined during local schema repair.'
                END, 1, 4000),
                "UpdatedAt" = {repairAt}
            WHERE "Id" IN (SELECT "Id" FROM "__DuplicateActiveProvisioningRuns");

            DROP TABLE "__DuplicateActiveProvisioningRuns";
            """,
            cancellationToken);
    }

    private static Task<bool> NormalizeDuplicateOutboxCreatedAtAsync(
        ApplicationDbContext db,
        CancellationToken cancellationToken)
        => NormalizeCreatedAtAsync(
            db,
            """
            SELECT message."Id", message."CreatedAt"
            FROM "OutboxMessages" AS message
            WHERE EXISTS (
                SELECT 1
                FROM "OutboxMessages" AS duplicate
                WHERE duplicate."Type" = message."Type"
                  AND duplicate."CorrelationId" = message."CorrelationId"
                  AND duplicate."Id" <> message."Id")
            """,
            "OutboxMessages",
            cancellationToken);

    private static Task<bool> NormalizeDuplicateActiveProvisioningCreatedAtAsync(
        ApplicationDbContext db,
        CancellationToken cancellationToken)
        => NormalizeCreatedAtAsync(
            db,
            """
            SELECT run."Id", run."CreatedAt"
            FROM "ProvisioningRuns" AS run
            WHERE run."Status" IN (0, 1, 8, 9, 12, 13, 15)
              AND EXISTS (
                  SELECT 1
                  FROM "ProvisioningRuns" AS duplicate
                  WHERE duplicate."NodeId" = run."NodeId"
                    AND duplicate."Status" IN (0, 1, 8, 9, 12, 13, 15)
                    AND duplicate."Id" <> run."Id")
            """,
            "ProvisioningRuns",
            cancellationToken);

    private static async Task<bool> NormalizeCreatedAtAsync(
        ApplicationDbContext db,
        string selectSql,
        string tableName,
        CancellationToken cancellationToken)
    {
        await EnsureOpenAsync(db, cancellationToken);
        var rows = new List<(string Id, string CreatedAt)>();
        await using (var select = db.Database.GetDbConnection().CreateCommand())
        {
            select.CommandText = selectSql;
            await using var reader = await select.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                rows.Add((reader.GetString(0), reader.GetString(1)));
            }
        }

        var changed = false;
        foreach (var row in rows)
        {
            if (!DateTimeOffset.TryParse(
                    row.CreatedAt,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.RoundtripKind,
                    out var parsed))
            {
                throw new InvalidOperationException($"SQLite {tableName}.CreatedAt contains an invalid timestamp for migration preflight.");
            }

            var canonical = parsed.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture);
            if (string.Equals(canonical, row.CreatedAt, StringComparison.Ordinal))
            {
                continue;
            }

            await using var update = db.Database.GetDbConnection().CreateCommand();
            update.CommandText = $"UPDATE \"{tableName}\" SET \"CreatedAt\" = $createdAt WHERE \"Id\" = $id";
            AddParameter(update, "$createdAt", canonical);
            AddParameter(update, "$id", row.Id);
            await update.ExecuteNonQueryAsync(cancellationToken);
            changed = true;
        }

        return changed;
    }

    private static async Task BackfillTelegramNotificationDeduplicationKeysAsync(
        ApplicationDbContext db,
        CancellationToken cancellationToken)
    {
        var notifications = await db.TelegramBotNotifications
            .Where(x => x.DeduplicationKey == string.Empty || x.DeduplicationKey.StartsWith("legacy:"))
            .ToListAsync(cancellationToken);
        var canonicalKeys = (await db.TelegramBotNotifications
                .AsNoTracking()
                .Where(x => x.DeduplicationKey != string.Empty
                    && !x.DeduplicationKey.StartsWith("legacy:")
                    && !x.DeduplicationKey.StartsWith("duplicate:"))
                .Select(x => x.DeduplicationKey)
                .ToListAsync(cancellationToken))
            .ToHashSet(StringComparer.Ordinal);
        foreach (var group in notifications.GroupBy(x => TelegramNotificationDeduplication.CreateKey(
                     x.TelegramUserId,
                     x.Type,
                     x.PayloadJson), StringComparer.Ordinal))
        {
            var ordered = group
                .OrderBy(NotificationPriority)
                .ThenBy(x => x.CreatedAt)
                .ThenBy(x => x.Id)
                .ToList();
            var duplicates = ordered.AsEnumerable();
            if (canonicalKeys.Add(group.Key))
            {
                ordered[0].DeduplicationKey = group.Key;
                duplicates = ordered.Skip(1);
            }

            foreach (var duplicate in duplicates)
            {
                duplicate.DeduplicationKey = $"duplicate:{duplicate.Id:N}";
                if (duplicate.Status is "pending" or "sending")
                {
                    duplicate.Status = "cancelled";
                    duplicate.NextAttemptAt = null;
                    duplicate.ErrorText = "Duplicate Telegram notification cancelled during local schema repair.";
                }
            }
        }

        if (notifications.Count > 0)
        {
            await db.SaveChangesAsync(cancellationToken);
        }
    }

    private static int NotificationPriority(VpnPlatform.Domain.Entities.TelegramBotNotification notification)
        => notification.Status switch
        {
            "sent" => 0,
            "pending" or "sending" => 1,
            _ => 2
        };

    private static async Task BackfillOutboxDuplicateCorrelationsAsync(
        ApplicationDbContext db,
        DateTimeOffset repairAt,
        CancellationToken cancellationToken)
    {
        var messages = await db.OutboxMessages.ToListAsync(cancellationToken);
        foreach (var group in messages.GroupBy(x => (x.Type, x.CorrelationId)).Where(x => x.Count() > 1))
        {
            var ordered = group
                .OrderBy(x => x.ProcessedAt.HasValue ? 0 : 1)
                .ThenBy(x => x.CreatedAt)
                .ThenBy(x => x.Id)
                .ToList();
            foreach (var duplicate in ordered.Skip(1))
            {
                duplicate.CorrelationId = $"legacy:{duplicate.Id:N}";
                if (!duplicate.ProcessedAt.HasValue
                    && duplicate.Type is not ("password_reset_requested" or "PaymentStatusChanged"))
                {
                    duplicate.FailedAt = repairAt;
                    duplicate.LastError = "Duplicate outbox message cancelled during local schema repair.";
                }
            }
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    private static async Task<bool> TableExistsAsync(ApplicationDbContext db, string tableName, CancellationToken cancellationToken)
    {
        await using var command = db.Database.GetDbConnection().CreateCommand();
        command.CommandText = "SELECT COUNT(1) FROM sqlite_master WHERE type = 'table' AND name = $tableName";
        AddParameter(command, "$tableName", tableName);

        await EnsureOpenAsync(db, cancellationToken);
        var result = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt32(result) > 0;
    }

    private static async Task<bool> ColumnExistsAsync(ApplicationDbContext db, string tableName, string columnName, CancellationToken cancellationToken)
    {
        await using var command = db.Database.GetDbConnection().CreateCommand();
        command.CommandText = $"PRAGMA table_info(\"{tableName.Replace("\"", "\"\"", StringComparison.Ordinal)}\")";

        await EnsureOpenAsync(db, cancellationToken);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            if (string.Equals(reader["name"]?.ToString(), columnName, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static async Task<bool> IndexExistsAsync(ApplicationDbContext db, string indexName, CancellationToken cancellationToken)
    {
        await using var command = db.Database.GetDbConnection().CreateCommand();
        command.CommandText = "SELECT COUNT(1) FROM sqlite_master WHERE type = 'index' AND name = $indexName";
        AddParameter(command, "$indexName", indexName);

        await EnsureOpenAsync(db, cancellationToken);
        var result = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt32(result) > 0;
    }

    private static async Task<bool> IndexIsUniqueAsync(
        ApplicationDbContext db,
        string tableName,
        string indexName,
        CancellationToken cancellationToken)
    {
        await using var command = db.Database.GetDbConnection().CreateCommand();
        command.CommandText = $"PRAGMA index_list(\"{tableName.Replace("\"", "\"\"", StringComparison.Ordinal)}\")";
        await EnsureOpenAsync(db, cancellationToken);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            if (string.Equals(reader["name"]?.ToString(), indexName, StringComparison.OrdinalIgnoreCase))
            {
                return Convert.ToInt32(reader["unique"]) == 1;
            }
        }

        return false;
    }

    private static async Task EnsureOpenAsync(ApplicationDbContext db, CancellationToken cancellationToken)
    {
        var connection = db.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync(cancellationToken);
        }
    }

    private static void AddParameter(IDbCommand command, string name, object value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value;
        command.Parameters.Add(parameter);
    }
}
