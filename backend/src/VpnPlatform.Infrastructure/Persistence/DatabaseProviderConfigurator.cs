using Microsoft.EntityFrameworkCore;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;

namespace VpnPlatform.Infrastructure.Persistence;

public static class DatabaseProviderConfigurator
{
    public const string PostgresProvider = "Postgres";
    public const string SqliteProvider = "Sqlite";

    public static DbContextOptionsBuilder UseConfiguredDatabase(this DbContextOptionsBuilder builder, IConfiguration configuration)
    {
        var provider = configuration["Database:Provider"] ?? PostgresProvider;
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection is required.");

        if (IsSqlite(provider))
        {
            EnsureSqliteDirectory(connectionString);
            return builder.UseSqlite(connectionString);
        }

        if (IsPostgres(provider))
        {
            return builder.UseNpgsql(connectionString);
        }

        throw new InvalidOperationException($"Unsupported Database:Provider '{provider}'. Use '{PostgresProvider}' or '{SqliteProvider}'.");
    }

    public static bool IsSqlite(string? provider)
        => string.Equals(provider, SqliteProvider, StringComparison.OrdinalIgnoreCase)
           || string.Equals(provider, "SQLite", StringComparison.OrdinalIgnoreCase);

    public static bool IsPostgres(string? provider)
        => string.Equals(provider, PostgresProvider, StringComparison.OrdinalIgnoreCase)
           || string.Equals(provider, "PostgreSQL", StringComparison.OrdinalIgnoreCase)
           || string.Equals(provider, "Npgsql", StringComparison.OrdinalIgnoreCase);

    private static void EnsureSqliteDirectory(string connectionString)
    {
        var sqlite = new SqliteConnectionStringBuilder(connectionString);
        if (string.IsNullOrWhiteSpace(sqlite.DataSource) || string.Equals(sqlite.DataSource, ":memory:", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var directory = Path.GetDirectoryName(Path.GetFullPath(sqlite.DataSource));
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }
    }
}
