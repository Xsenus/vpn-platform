using Xunit;

namespace VpnPlatform.UnitTests;

public class AdminBootstrapCliScriptTests
{
    [Fact]
    public void Admin_Bootstrap_Script_Should_Run_One_Shot_Command_Without_Printing_Password()
    {
        var root = FindRepositoryRoot();
        var script = File.ReadAllText(Path.Combine(root, "scripts", "admin-bootstrap.ps1"));
        var guide = File.ReadAllText(Path.Combine(root, "docs", "admin-bootstrap.md"));

        Assert.Contains("admin-bootstrap", script, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("AdminBootstrap__Enabled", script, StringComparison.Ordinal);
        Assert.Contains("AdminBootstrap__Email", script, StringComparison.Ordinal);
        Assert.Contains("AdminBootstrap__Password", script, StringComparison.Ordinal);
        Assert.Contains("Password: [hidden]", script, StringComparison.Ordinal);
        Assert.Contains("Dry-run mode: database was not changed", script, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Write-Host \"Password: $Password", script, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("scripts\\admin-bootstrap.ps1", guide, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Password: [hidden]", guide, StringComparison.Ordinal);
    }

    [Fact]
    public void Admin_Bootstrap_Script_Should_Support_Local_Sqlite_And_Production_Postgres()
    {
        var root = FindRepositoryRoot();
        var script = File.ReadAllText(Path.Combine(root, "scripts", "admin-bootstrap.ps1"));
        var guide = File.ReadAllText(Path.Combine(root, "docs", "admin-bootstrap.md"));

        foreach (var required in new[]
                 {
                     "LocalSqlite",
                     "Data Source=data/vpnplatform-local.db",
                     "Database__Provider",
                     "Database__ApplyMigrationsOnStartup",
                     "Database__UseEnsureCreatedForLocalSqlite",
                     "ConnectionStrings__DefaultConnection",
                     "Postgres",
                     "Production"
                 })
        {
            Assert.Contains(required, script, StringComparison.OrdinalIgnoreCase);
            Assert.Contains(required, guide, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void Admin_Bootstrap_Docs_Should_Link_Roadmap_Release_And_Test_Results()
    {
        var root = FindRepositoryRoot();
        var roadmap = File.ReadAllText(Path.Combine(root, "docs", "PRODUCT_COMPLETION_ROADMAP.md"));
        var testResults = File.ReadAllText(Path.Combine(root, "TEST_RESULTS.md"));
        var releases = File.ReadAllText(Path.Combine(root, "backend", "src", "VpnPlatform.Api", "AppReleases", "releases.json"));

        Assert.Contains("[x] `P0-ADMIN-001B`", roadmap, StringComparison.Ordinal);
        Assert.Contains("2026-06-19-admin-bootstrap-wrapper", releases, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("2026-06-19-admin-bootstrap-wrapper", testResults, StringComparison.OrdinalIgnoreCase);
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "README.md")) && Directory.Exists(Path.Combine(directory.FullName, "backend")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Repository root was not found for admin bootstrap CLI tests.");
    }
}
