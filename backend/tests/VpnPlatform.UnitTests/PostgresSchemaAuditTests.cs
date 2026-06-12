using Microsoft.EntityFrameworkCore;
using VpnPlatform.Domain.Entities;
using VpnPlatform.Infrastructure.Persistence;
using Xunit;

namespace VpnPlatform.UnitTests;

public class PostgresSchemaAuditTests
{
    [Fact]
    public void ApplicationDbContext_Postgres_Model_Should_Have_Auditable_Relational_Metadata()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql("Host=localhost;Database=vpn_platform_schema_audit;Username=postgres;Password=postgres")
            .Options;

        using var db = new ApplicationDbContext(options);
        var entityTypes = db.Model.GetEntityTypes()
            .Where(x => !x.IsOwned() && x.GetTableName() is not null)
            .ToArray();

        var tableNames = entityTypes
            .Select(x => x.GetTableName())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x!)
            .ToArray();
        var duplicateTables = tableNames
            .GroupBy(x => x, StringComparer.OrdinalIgnoreCase)
            .Where(x => x.Count() > 1)
            .Select(x => x.Key)
            .ToArray();

        Assert.True(entityTypes.Length >= 50, $"Unexpectedly small EF schema: {entityTypes.Length} mapped entities.");
        Assert.Empty(duplicateTables);
        Assert.All(entityTypes, entityType => Assert.NotNull(entityType.FindPrimaryKey()));
        Assert.Contains(entityTypes, x => x.ClrType == typeof(User));
        Assert.Contains(entityTypes, x => x.ClrType == typeof(Order));
        Assert.Contains(entityTypes, x => x.ClrType == typeof(PaymentAttempt));
        Assert.Contains(entityTypes, x => x.ClrType == typeof(VpnNode));
        Assert.Contains(entityTypes, x => x.ClrType == typeof(AppRelease));

        var indexCount = entityTypes.SelectMany(x => x.GetIndexes()).Count();
        var foreignKeyCount = entityTypes.SelectMany(x => x.GetForeignKeys()).Count();
        var nullableColumnCount = entityTypes.SelectMany(x => x.GetProperties()).Count(x => x.IsNullable);

        Assert.True(indexCount >= 40, $"PostgreSQL model should expose enough indexes for audit. Actual: {indexCount}.");
        Assert.True(foreignKeyCount >= 35, $"PostgreSQL model should expose enough FK relations for audit. Actual: {foreignKeyCount}.");
        Assert.True(nullableColumnCount >= 30, $"PostgreSQL model should expose nullable column metadata for audit. Actual: {nullableColumnCount}.");
    }

    [Fact]
    public void Schema_Audit_Should_Have_Safe_Cross_Platform_Scripts_And_Runbook()
    {
        var root = FindRepositoryRoot();
        var bashScript = File.ReadAllText(Path.Combine(root, "scripts", "audit-postgres-schema.sh"));
        var powershellScript = File.ReadAllText(Path.Combine(root, "scripts", "audit-postgres-schema.ps1"));
        var docs = File.ReadAllText(Path.Combine(root, "docs", "postgres-schema-audit.md"));

        foreach (var script in new[] { bashScript, powershellScript })
        {
            Assert.Contains("dotnet ef migrations list", script, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("dotnet ef migrations script", script, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("--idempotent", script, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("--no-connect", script, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("DATABASE_URL", script, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("psql", script, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("information_schema.columns", script, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("information_schema.table_constraints", script, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("pg_indexes", script, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("FOREIGN KEY", script, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("postgres-schema-snapshot.txt", script, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("postgres-migrations-idempotent.sql", script, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("Database__ApplyMigrationsOnStartup", script, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("Database__SeedDemoData", script, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("Provisioning__LiveExecutionEnabled", script, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("TelegramBot__Enabled", script, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("X3UI_PASSWORD", script, StringComparison.OrdinalIgnoreCase);
        }

        Assert.Contains("scripts\\audit-postgres-schema.ps1", docs, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("./scripts/audit-postgres-schema.sh", docs, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("postgres-schema-snapshot.txt", docs, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("information_schema", docs, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("не выводит значения секретов", docs, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Migrations_Directory_Should_Contain_Snapshot_And_Ordered_Migration_Chain()
    {
        var root = FindRepositoryRoot();
        var migrationsDir = Path.Combine(root, "backend", "src", "VpnPlatform.Infrastructure", "Persistence", "Migrations");
        var migrationFiles = Directory.GetFiles(migrationsDir, "*.cs")
            .Select(Path.GetFileName)
            .Where(x => x is not null && x != "ApplicationDbContextModelSnapshot.cs" && !x.EndsWith(".Designer.cs", StringComparison.OrdinalIgnoreCase))
            .Select(x => x!)
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToArray();

        Assert.True(File.Exists(Path.Combine(migrationsDir, "ApplicationDbContextModelSnapshot.cs")));
        Assert.True(migrationFiles.Length >= 9, $"Unexpectedly small migration chain: {migrationFiles.Length} migrations.");
        Assert.Equal(migrationFiles, migrationFiles.OrderBy(x => x, StringComparer.Ordinal).ToArray());
        Assert.StartsWith("20260429000100_InitialCreate", migrationFiles.First(), StringComparison.Ordinal);
        Assert.Contains(migrationFiles, x => x.Contains("AddPaymentProviderWebhookUrl", StringComparison.Ordinal));
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "README.md"))
                && Directory.Exists(Path.Combine(directory.FullName, "backend"))
                && Directory.Exists(Path.Combine(directory.FullName, "scripts")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Repository root was not found.");
    }
}
