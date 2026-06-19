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

    [Fact]
    public void Admin_Vps_Bootstrap_Smoke_Wrapper_Should_Run_Reset_Then_Smoke_Without_Printing_Password()
    {
        var root = FindRepositoryRoot();
        var script = File.ReadAllText(Path.Combine(root, "scripts", "admin-vps-bootstrap-smoke.ps1"));
        var guide = File.ReadAllText(Path.Combine(root, "docs", "admin-bootstrap.md"));
        var smokeGuide = File.ReadAllText(Path.Combine(root, "docs", "admin-vps-smoke.md"));
        var roadmap = File.ReadAllText(Path.Combine(root, "docs", "PRODUCT_COMPLETION_ROADMAP.md"));

        foreach (var expected in new[]
                 {
                     "admin-bootstrap.ps1",
                     "admin-vps-smoke.ps1",
                     "admin-vps-bootstrap-smoke-readiness.ps1",
                     "validate-admin-vps-bootstrap-smoke-report.ps1",
                     "validate-admin-vps-bootstrap-smoke-evidence.ps1",
                     "ADMIN_VPS_BOOTSTRAP_SMOKE_ADMIN_PASSWORD",
                     "AdminBootstrap__Password",
                     "ADMIN_VPS_SMOKE_ADMIN_PASSWORD",
                     "previousBootstrapPassword",
                     "Set-ProcessEnv",
                     "Env:\\$Name",
                     "Get-LatestReleaseId",
                     "releaseValue",
                     "Release id:",
                     "ConfirmBootstrapReset",
                     "Pass -ConfirmBootstrapReset",
                     "Connection string is required",
                     "AccountBootstrapChecked",
                     "Password: [hidden]",
                     "Dry-run mode: admin VPS smoke was not started",
                     "BootstrapSmokeReportPath",
                     "ReadinessReportPath",
                     "admin-vps-bootstrap-smoke-report.json",
                     "admin-vps-bootstrap-smoke-readiness-report.json",
                     "Validated bootstrap smoke report",
                     "Admin VPS bootstrap+smoke flow completed"
                 })
        {
            Assert.Contains(expected, script, StringComparison.OrdinalIgnoreCase);
        }

        Assert.True(
            script.IndexOf("admin-bootstrap.ps1", StringComparison.OrdinalIgnoreCase)
            < script.IndexOf("admin-vps-smoke.ps1", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain("[string]$Password", script, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Write-Host \"Password: $password", script, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("admin-vps-bootstrap-smoke.ps1", guide + smokeGuide, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("[x] `P0-ADMIN-001C`", roadmap, StringComparison.Ordinal);
    }

    [Fact]
    public void Local_Admin_Vps_Bootstrap_Smoke_Should_Use_Cli_Bootstrap_Before_Api_Login()
    {
        var root = FindRepositoryRoot();
        var script = File.ReadAllText(Path.Combine(root, "scripts", "local-admin-vps-bootstrap-smoke.ps1"));
        var smokeGuide = File.ReadAllText(Path.Combine(root, "docs", "admin-vps-smoke.md"));
        var roadmap = File.ReadAllText(Path.Combine(root, "docs", "PRODUCT_COMPLETION_ROADMAP.md"));

        foreach (var expected in new[]
                 {
                     "admin-bootstrap.ps1",
                     "admin-vps-bootstrap-smoke.ps1",
                     "fresh-bootstrap-admin@example.test",
                     "AdminBootstrap__Enabled",
                     "\"false\"",
                     "Database__UseEnsureCreatedForLocalSqlite",
                     "ADMIN_VPS_BOOTSTRAP_SMOKE_ADMIN_PASSWORD",
                     "BootstrapSmokeReportPath",
                     "ReadinessReportPath",
                     "Assert-InWorkspace",
                     "Stop-ProcessTree",
                     "local admin vps bootstrap smoke ok"
                 })
        {
            Assert.Contains(expected, script, StringComparison.OrdinalIgnoreCase);
        }

        Assert.True(
            script.IndexOf("admin-bootstrap.ps1", StringComparison.OrdinalIgnoreCase)
            < script.IndexOf("dotnet", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain("Write-Host \"Password: $password", script, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("local-admin-vps-bootstrap-smoke.ps1", smokeGuide, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("[x] `P0-ADMIN-001C`", roadmap, StringComparison.Ordinal);
    }

    [Fact]
    public void Admin_Vps_Bootstrap_Smoke_Report_Should_Link_Reset_And_Smoke_Evidence()
    {
        var root = FindRepositoryRoot();
        var validator = File.ReadAllText(Path.Combine(root, "scripts", "validate-admin-vps-bootstrap-smoke-report.ps1"));
        var wrapper = File.ReadAllText(Path.Combine(root, "scripts", "admin-vps-bootstrap-smoke.ps1"));
        var localSmoke = File.ReadAllText(Path.Combine(root, "scripts", "local-admin-vps-bootstrap-smoke.ps1"));
        var guide = File.ReadAllText(Path.Combine(root, "docs", "admin-bootstrap.md"));
        var smokeGuide = File.ReadAllText(Path.Combine(root, "docs", "admin-vps-smoke.md"));
        var roadmap = File.ReadAllText(Path.Combine(root, "docs", "PRODUCT_COMPLETION_ROADMAP.md"));

        foreach (var expected in new[]
                 {
                     "RequirePassed",
                     "validate-admin-vps-smoke-evidence.ps1",
                     "bootstrapResetConfirmed",
                     "localSqlite",
                     "dryRun",
                     "accountBootstrapChecked",
                     "passwordEnvName",
                     "passwordEnvPresent",
                     "mismatch for $Name",
                     "preflight releaseId",
                     "smoke releaseId",
                     "smokeReportPath",
                     "preflightReportPath",
                     "contains forbidden secret marker",
                     "admin vps bootstrap smoke report valid"
                 })
        {
            Assert.Contains(expected, validator + wrapper, StringComparison.OrdinalIgnoreCase);
        }

        Assert.Contains("admin-vps-bootstrap-smoke-report.json", localSmoke + wrapper, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("validate-admin-vps-bootstrap-smoke-report.ps1", guide + smokeGuide, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("[x] `P0-ADMIN-001E`", roadmap, StringComparison.Ordinal);
    }

    [Fact]
    public void Admin_Vps_Bootstrap_Smoke_Readiness_Should_Fail_Closed_Before_Reset()
    {
        var root = FindRepositoryRoot();
        var readiness = File.ReadAllText(Path.Combine(root, "scripts", "admin-vps-bootstrap-smoke-readiness.ps1"));
        var validator = File.ReadAllText(Path.Combine(root, "scripts", "validate-admin-vps-bootstrap-smoke-readiness-report.ps1"));
        var regression = File.ReadAllText(Path.Combine(root, "scripts", "test-admin-vps-bootstrap-smoke-readiness.ps1"));
        var wrapper = File.ReadAllText(Path.Combine(root, "scripts", "admin-vps-bootstrap-smoke.ps1"));
        var guide = File.ReadAllText(Path.Combine(root, "docs", "admin-bootstrap.md"));
        var smokeGuide = File.ReadAllText(Path.Combine(root, "docs", "admin-vps-smoke.md"));
        var roadmap = File.ReadAllText(Path.Combine(root, "docs", "PRODUCT_COMPLETION_ROADMAP.md"));

        foreach (var expected in new[]
                 {
                     "readyForBootstrapSmoke",
                     "passwordEnvPresent",
                     "passwordLengthOk",
                     "connectionStringPresent",
                     "confirmBootstrapReset",
                     "local-or-confirm-reset",
                     "connection-string",
                     "ReadinessReportPath",
                     "admin-vps-bootstrap-smoke-readiness-report.json",
                     "admin vps bootstrap smoke readiness report valid",
                     "contains forbidden secret marker",
                     "WriteAllText",
                     "UTF8Encoding"
                 })
        {
            Assert.Contains(expected, readiness + validator + wrapper, StringComparison.OrdinalIgnoreCase);
        }

        foreach (var expected in new[]
                 {
                     "local-ready",
                     "missing-password",
                     "missing-confirm-bootstrap-reset",
                     "missing-connection-string",
                     "leaked password",
                     "leaked connection string",
                     "UTF-8 BOM",
                     "admin vps bootstrap smoke readiness regression passed"
                 })
        {
            Assert.Contains(expected, regression, StringComparison.OrdinalIgnoreCase);
        }

        Assert.True(
            wrapper.IndexOf("& $readinessScript", StringComparison.OrdinalIgnoreCase)
            < wrapper.IndexOf("& $bootstrapScript", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain("[string]$Password", readiness, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Write-Host \"Password: $password", readiness, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("test-admin-vps-bootstrap-smoke-readiness.ps1", guide + smokeGuide, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("[x] `P0-ADMIN-001F`", roadmap, StringComparison.Ordinal);
    }

    [Fact]
    public void Admin_Vps_Bootstrap_Smoke_Evidence_Should_Link_Readiness_And_Final_Report()
    {
        var root = FindRepositoryRoot();
        var evidenceValidator = File.ReadAllText(Path.Combine(root, "scripts", "validate-admin-vps-bootstrap-smoke-evidence.ps1"));
        var regression = File.ReadAllText(Path.Combine(root, "scripts", "test-admin-vps-bootstrap-smoke-evidence-validator.ps1"));
        var wrapper = File.ReadAllText(Path.Combine(root, "scripts", "admin-vps-bootstrap-smoke.ps1"));
        var guide = File.ReadAllText(Path.Combine(root, "docs", "admin-bootstrap.md"));
        var smokeGuide = File.ReadAllText(Path.Combine(root, "docs", "admin-vps-smoke.md"));
        var roadmap = File.ReadAllText(Path.Combine(root, "docs", "PRODUCT_COMPLETION_ROADMAP.md"));

        foreach (var expected in new[]
                 {
                     "ReadinessReportPath",
                     "BootstrapSmokeReportPath",
                     "validate-admin-vps-bootstrap-smoke-readiness-report.ps1",
                     "validate-admin-vps-bootstrap-smoke-report.ps1",
                     "readyForBootstrapSmoke",
                     "bootstrapResetConfirmed",
                     "passwordEnvPresent",
                     "releaseId",
                     "Assert-Same",
                     "mismatch for $Name",
                     "bootstrap report must be generated after readiness report",
                     "admin vps bootstrap smoke evidence valid"
                 })
        {
            Assert.Contains(expected, evidenceValidator + wrapper, StringComparison.OrdinalIgnoreCase);
        }

        foreach (var expected in new[]
                 {
                     "valid",
                     "mismatched-admin-url",
                     "readiness-not-ready",
                     "mismatched-release-id",
                     "mismatched-smoke-release-id",
                     "bad-timing",
                     "bad-smoke-route",
                     "route must match sections contract",
                     "admin vps bootstrap smoke evidence validator regression passed"
                 })
        {
            Assert.Contains(expected, regression, StringComparison.OrdinalIgnoreCase);
        }

        Assert.True(
            wrapper.IndexOf("validate-admin-vps-bootstrap-smoke-report.ps1", StringComparison.OrdinalIgnoreCase)
            < wrapper.IndexOf("validate-admin-vps-bootstrap-smoke-evidence.ps1", StringComparison.OrdinalIgnoreCase));
        Assert.Contains("validate-admin-vps-bootstrap-smoke-evidence.ps1", guide + smokeGuide, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("test-admin-vps-bootstrap-smoke-evidence-validator.ps1", guide + smokeGuide, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("[x] `P0-ADMIN-001G`", roadmap, StringComparison.Ordinal);
    }

    [Fact]
    public void Admin_Vps_Bootstrap_Smoke_Wrapper_Regression_Should_Fail_Closed_Before_Smoke()
    {
        var root = FindRepositoryRoot();
        var script = File.ReadAllText(Path.Combine(root, "scripts", "test-admin-vps-bootstrap-smoke-wrapper.ps1"));
        var wrapper = File.ReadAllText(Path.Combine(root, "scripts", "admin-vps-bootstrap-smoke.ps1"));
        var guide = File.ReadAllText(Path.Combine(root, "docs", "admin-bootstrap.md"));
        var smokeGuide = File.ReadAllText(Path.Combine(root, "docs", "admin-vps-smoke.md"));
        var roadmap = File.ReadAllText(Path.Combine(root, "docs", "PRODUCT_COMPLETION_ROADMAP.md"));

        foreach (var expected in new[]
                 {
                     "admin-vps-bootstrap-smoke-wrapper-regression-test",
                     "admin-vps-bootstrap-smoke.ps1",
                     "Invoke-BootstrapSmokeScenario",
                     "missing-password",
                     "missing-confirm-bootstrap-reset",
                     "missing-connection-string",
                     "dry-run-no-smoke",
                     "admin-vps-bootstrap-smoke-report.json",
                     "admin-vps-bootstrap-smoke-readiness-report.json",
                     "ADMIN_VPS_BOOTSTRAP_SMOKE_ADMIN_PASSWORD",
                     "ADMIN_VPS_SMOKE_ADMIN_PASSWORD",
                     "Pass -ConfirmBootstrapReset",
                     "Connection string is required for non-local admin bootstrap/reset",
                     "Dry-run mode: admin VPS smoke was not started",
                     "Admin VPS smoke flow is ready to run.",
                     "Smoke artifact should not exist",
                     "Readiness report releaseId should be resolved",
                     "readinessReleaseId",
                     "leaked password",
                     "admin vps bootstrap smoke wrapper regression passed"
                 })
        {
            Assert.Contains(expected, script, StringComparison.OrdinalIgnoreCase);
        }

        Assert.DoesNotContain("[string]$Password", wrapper, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("test-admin-vps-bootstrap-smoke-wrapper.ps1", guide + smokeGuide, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("[x] `P0-ADMIN-001D`", roadmap, StringComparison.Ordinal);
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
