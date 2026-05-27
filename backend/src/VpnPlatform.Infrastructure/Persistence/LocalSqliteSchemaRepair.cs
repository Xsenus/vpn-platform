using System.Data;
using Microsoft.EntityFrameworkCore;

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

        return repaired;
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
