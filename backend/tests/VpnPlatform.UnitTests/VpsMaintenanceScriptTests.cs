using System.Text.RegularExpressions;
using Xunit;

namespace VpnPlatform.UnitTests;

public class VpsMaintenanceScriptTests
{
    [Fact]
    public void Vps_Maintenance_Script_Should_Default_To_Dry_Run_And_Report_Disk_Memory()
    {
        var root = FindRepositoryRoot();
        var script = File.ReadAllText(Path.Combine(root, "scripts", "vps-maintenance.sh"));
        var docs = File.ReadAllText(Path.Combine(root, "docs", "vps-maintenance.md"));

        Assert.Contains("set -euo pipefail", script, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("DRY_RUN=true", script, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("--apply", script, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("--dry-run", script, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("df -h", script, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("free -h", script, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("du -sh", script, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("== Before ==", script, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("== After ==", script, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("scripts/vps-maintenance.sh --dry-run", docs, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("df -h", docs, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("free -h", docs, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Vps_Maintenance_Script_Should_Protect_Working_Data_And_Current_Release()
    {
        var root = FindRepositoryRoot();
        var script = File.ReadAllText(Path.Combine(root, "scripts", "vps-maintenance.sh"));

        Assert.Contains("safe_rm_rf", script, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("RESOLVED_APP_DIR", script, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Refusing to remove path outside APP_DIR", script, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Refusing to remove protected path", script, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("APP_DIR/shared", script, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("APP_DIR/current", script, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("readlink -f", script, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("keep-current", script, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("[0-9a-fA-F]{7,40}", script, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("-mindepth 1 -maxdepth 1 -type d", script, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("rm -rf \"$APP_DIR\"", script, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("rm -rf /", script, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Vps_Maintenance_Script_Should_Cover_Logs_Apt_Journal_And_Safe_Docker_Prune()
    {
        var root = FindRepositoryRoot();
        var script = File.ReadAllText(Path.Combine(root, "scripts", "vps-maintenance.sh"));
        var docs = File.ReadAllText(Path.Combine(root, "docs", "vps-maintenance.md"));

        Assert.Contains("journalctl --vacuum-time", script, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("apt-get clean", script, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("apt-get autoclean", script, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("docker system df", script, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("docker builder prune", script, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("docker image prune", script, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("docker container prune", script, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("docker volume prune", script, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("docker system prune -a --volumes", script, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Volumes are never pruned", script, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Никогда не выполняет `docker volume prune`", docs, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("production `.env`", docs, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Vps_Maintenance_Roadmap_Release_And_Test_Results_Should_Be_Linked()
    {
        var root = FindRepositoryRoot();
        var roadmap = File.ReadAllText(Path.Combine(root, "docs", "PRODUCT_COMPLETION_ROADMAP.md"));
        var testResults = File.ReadAllText(Path.Combine(root, "TEST_RESULTS.md"));
        var releases = File.ReadAllText(Path.Combine(root, "backend", "src", "VpnPlatform.Api", "AppReleases", "releases.json"));

        Assert.Contains("[x] `P8-CI-004`", roadmap, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("2026-06-13-vps-maintenance-safe-cleanup", releases, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("2026-06-13-vps-maintenance-safe-cleanup", testResults, StringComparison.OrdinalIgnoreCase);
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, ".env.example")) && Directory.Exists(Path.Combine(directory.FullName, "backend")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Repository root was not found for VPS maintenance script tests.");
    }
}
