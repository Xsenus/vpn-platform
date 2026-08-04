using System.Data;
using Microsoft.EntityFrameworkCore;
using VpnPlatform.Application.Common;

namespace VpnPlatform.Infrastructure.Persistence;

public static class LocalSqliteSchemaRepair
{
    public static async Task<int> ApplyAsync(ApplicationDbContext db, CancellationToken cancellationToken = default)
    {
        if (!db.Database.IsSqlite())
        {
            return 0;
        }

        var repaired = 0;
        if (await TableExistsAsync(db, "PaymentProviderAccounts", cancellationToken)
            && !await ColumnExistsAsync(db, "PaymentProviderAccounts", "WebhookUrl", cancellationToken))
        {
            await db.Database.ExecuteSqlRawAsync(
                """ALTER TABLE "PaymentProviderAccounts" ADD COLUMN "WebhookUrl" TEXT NOT NULL DEFAULT '';""",
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

        return repaired;
    }

    private static async Task BackfillTelegramNotificationDeduplicationKeysAsync(
        ApplicationDbContext db,
        CancellationToken cancellationToken)
    {
        var notifications = await db.TelegramBotNotifications
            .Where(x => x.DeduplicationKey == string.Empty)
            .ToListAsync(cancellationToken);
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
            ordered[0].DeduplicationKey = group.Key;
            foreach (var duplicate in ordered.Skip(1))
            {
                duplicate.DeduplicationKey = $"legacy:{duplicate.Id:N}";
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
