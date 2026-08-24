using Microsoft.EntityFrameworkCore;
using Npgsql;
using VpnPlatform.Infrastructure.Persistence;
using Xunit;

namespace VpnPlatform.UnitTests;

public class PostgresMigrationRunnerTests
{
    [Fact]
    public void PgDump_Command_Should_Keep_Password_Out_Of_Arguments()
    {
        const string password = "secret value;with=symbols";
        var connection = new NpgsqlConnectionStringBuilder
        {
            Host = "db.internal",
            Port = 5544,
            Database = "vpnplatform",
            Username = "vpnplatform_user",
            Password = password,
            SslMode = SslMode.VerifyFull
        };

        var startInfo = PostgresMigrationRunner.BuildPgDumpStartInfo(connection, "/var/backups/vpn-platform.dump");
        var arguments = startInfo.ArgumentList.ToArray();

        Assert.Equal("pg_dump", startInfo.FileName);
        Assert.Contains("--host=db.internal", arguments);
        Assert.Contains("--port=5544", arguments);
        Assert.Contains("--username=vpnplatform_user", arguments);
        Assert.Contains("--dbname=vpnplatform", arguments);
        Assert.Contains("--no-password", arguments);
        Assert.Contains("--file=/var/backups/vpn-platform.dump", arguments);
        Assert.DoesNotContain(arguments, value => value.Contains(password, StringComparison.Ordinal));
        Assert.Equal(password, startInfo.Environment["PGPASSWORD"]);
        Assert.Equal("15", startInfo.Environment["PGCONNECT_TIMEOUT"]);
        Assert.Equal("verify-full", startInfo.Environment["PGSSLMODE"]);
        Assert.False(startInfo.UseShellExecute);
    }

    [Fact]
    public void PgRestore_Command_Should_Validate_The_Exact_Backup_Path()
    {
        var startInfo = PostgresMigrationRunner.BuildPgRestoreStartInfo("/var/backups/vpn platform.dump");

        Assert.Equal("pg_restore", startInfo.FileName);
        Assert.Equal(new[] { "--list", "/var/backups/vpn platform.dump" }, startInfo.ArgumentList);
        Assert.False(startInfo.UseShellExecute);
    }

    [Fact]
    public async Task Runner_Should_Reject_NonPostgres_Database_Before_Backup()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite("Data Source=:memory:")
            .Options;
        await using var db = new ApplicationDbContext(options);
        var runner = new PostgresMigrationRunner(db);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => runner.RunAsync(Path.GetFullPath("backups"), CancellationToken.None));

        Assert.Contains("PostgreSQL only", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Deployment_Workflow_Should_Require_Explicit_Backup_And_Migration_Flag()
    {
        var root = FindRepositoryRoot();
        var workflow = File.ReadAllText(Path.Combine(root, ".github", "workflows", "deploy-vps.yml"));

        Assert.Contains("apply_database_migrations:", workflow, StringComparison.Ordinal);
        Assert.Contains("default: false", workflow, StringComparison.Ordinal);
        Assert.Contains("command -v pg_dump", workflow, StringComparison.Ordinal);
        Assert.Contains("command -v pg_restore", workflow, StringComparison.Ordinal);
        Assert.Contains("systemctl stop vpn-platform-api", workflow, StringComparison.Ordinal);
        Assert.Contains("database-migrate", workflow, StringComparison.Ordinal);
        Assert.Contains("DatabaseMaintenance__BackupDirectory", workflow, StringComparison.Ordinal);
        Assert.Contains("systemctl restart vpn-platform-api", workflow, StringComparison.Ordinal);
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "README.md"))
                && Directory.Exists(Path.Combine(directory.FullName, "backend")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Repository root was not found.");
    }
}
