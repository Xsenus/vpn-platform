using Xunit;

namespace VpnPlatform.UnitTests;

public class DatabaseBackupRestoreScriptsTests
{
    [Fact]
    public void PostgreSql_Backup_And_Restore_Scripts_Should_Be_Safe_And_Cross_Platform()
    {
        var root = FindRepositoryRoot();
        var backupSh = File.ReadAllText(Path.Combine(root, "scripts", "backup-db.sh"));
        var restoreSh = File.ReadAllText(Path.Combine(root, "scripts", "restore-db.sh"));
        var backupPs = File.ReadAllText(Path.Combine(root, "scripts", "backup-db.ps1"));
        var restorePs = File.ReadAllText(Path.Combine(root, "scripts", "restore-db.ps1"));
        var applyMigrations = File.ReadAllText(Path.Combine(root, "scripts", "apply-migrations.sh"));
        var gitignore = File.ReadAllText(Path.Combine(root, ".gitignore"));
        var docs = File.ReadAllText(Path.Combine(root, "docs", "postgres-backup-restore.md"));

        foreach (var backupScript in new[] { backupSh, backupPs })
        {
            Assert.Contains("DATABASE_URL", backupScript, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("pg_dump", backupScript, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("format=custom", backupScript, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("no-owner", backupScript, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("no-privileges", backupScript, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("BACKUP_RETENTION_DAYS", backupScript, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("pg_restore", backupScript, StringComparison.OrdinalIgnoreCase);
            Assert.Contains(".list", backupScript, StringComparison.OrdinalIgnoreCase);
        }

        foreach (var restoreScript in new[] { restoreSh, restorePs })
        {
            Assert.Contains("BACKUP_FILE", restoreScript, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("RESTORE_DATABASE_URL", restoreScript, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("RESTORE_ALLOW_DATABASE_URL_MATCH", restoreScript, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("pg_restore", restoreScript, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("clean", restoreScript, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("if-exists", restoreScript, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("no-owner", restoreScript, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("no-privileges", restoreScript, StringComparison.OrdinalIgnoreCase);
        }

        Assert.Contains("scripts/backup-db.sh", applyMigrations, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("BACKUP_RETENTION_DAYS", applyMigrations, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("backups/", gitignore, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("vpnplatform_restore_check", docs, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("scripts/restore-db.sh", docs, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("scripts\\restore-db.ps1", docs, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("RESTORE_ALLOW_DATABASE_URL_MATCH=true", docs, StringComparison.OrdinalIgnoreCase);
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, ".env.example"))
                && Directory.Exists(Path.Combine(directory.FullName, "backend"))
                && Directory.Exists(Path.Combine(directory.FullName, "scripts")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Repository root was not found for backup/restore script tests.");
    }
}
