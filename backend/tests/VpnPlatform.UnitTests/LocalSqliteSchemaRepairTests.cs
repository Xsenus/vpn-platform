using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
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
}
