using System.Text.Json;
using Xunit;

namespace VpnPlatform.UnitTests;

public class AdminVpsSmokeReportTests
{
    private static readonly string[] RequiredSections =
    [
        "dashboard",
        "users",
        "payments",
        "tariffs",
        "subscriptions",
        "vpn",
        "nodes",
        "panels",
        "support",
        "audit",
        "bot",
        "releases",
        "faq",
        "content",
        "scenarios",
        "provisioning"
    ];

    [Fact]
    public void Admin_Vps_Smoke_Template_Should_List_All_Admin_Sections_Fail_Closed()
    {
        var root = FindRepositoryRoot();
        using var json = JsonDocument.Parse(File.ReadAllText(Path.Combine(root, "docs", "admin-vps-smoke-report.template.json")));

        Assert.False(json.RootElement.GetProperty("accountBootstrapChecked").GetBoolean());
        Assert.False(json.RootElement.GetProperty("adminLoginPassed").GetBoolean());
        Assert.False(json.RootElement.GetProperty("noJsErrors").GetBoolean());
        Assert.False(json.RootElement.GetProperty("noUnauthorizedAfterLogin").GetBoolean());

        var sections = json.RootElement.GetProperty("sections").EnumerateArray().ToArray();
        Assert.Equal(RequiredSections.Order(StringComparer.Ordinal), sections.Select(x => x.GetProperty("id").GetString()).Order(StringComparer.Ordinal));
        Assert.All(sections, section =>
        {
            Assert.Equal("blocked", section.GetProperty("status").GetString());
            Assert.Equal(0, section.GetProperty("httpStatus").GetInt32());
            Assert.False(section.GetProperty("loaded").GetBoolean());
            Assert.Contains("without secrets", section.GetProperty("evidence").GetString(), StringComparison.OrdinalIgnoreCase);
        });
    }

    [Fact]
    public void Admin_Vps_Smoke_Validator_Should_Require_Sections_Urls_And_Fail_Closed_Gate()
    {
        var root = FindRepositoryRoot();
        var script = File.ReadAllText(Path.Combine(root, "scripts", "validate-admin-vps-smoke-report.ps1"));

        foreach (var section in RequiredSections)
        {
            Assert.Contains(section, script, StringComparison.Ordinal);
        }

        foreach (var expected in new[]
                 {
                     "Assert-ReportHttpUrl",
                     "apiBaseUrl",
                     "adminWebUrl",
                     "accountBootstrapChecked",
                     "adminLoginPassed",
                     "noJsErrors",
                     "noUnauthorizedAfterLogin",
                     "RequireAllPassed",
                     "must be passed when -RequireAllPassed is used"
                 })
        {
            Assert.Contains(expected, script, StringComparison.OrdinalIgnoreCase);
        }

        Assert.Contains("Admin VPS smoke report contains forbidden secret marker", script, StringComparison.Ordinal);
    }

    [Fact]
    public void Admin_Vps_Smoke_Generator_Should_Create_Safe_Blocked_Report()
    {
        var root = FindRepositoryRoot();
        var script = File.ReadAllText(Path.Combine(root, "scripts", "new-admin-vps-smoke-report.ps1"));
        var guide = File.ReadAllText(Path.Combine(root, "docs", "admin-vps-smoke.md"));

        foreach (var expected in new[]
                 {
                     "admin-vps-smoke-report.template.json",
                     "validate-admin-vps-smoke-report.ps1",
                     "ConvertTo-Json -Depth 8",
                     "Set-Content",
                     "-Encoding UTF8",
                     "blocked",
                     "TODO: open",
                     "Output file already exists. Pass -Force",
                     "Get-LatestReleaseId"
                 })
        {
            Assert.Contains(expected, script, StringComparison.OrdinalIgnoreCase);
        }

        foreach (var field in new[] { "OutputPath", "ApiBaseUrl", "AdminWebUrl", "EnvironmentName", "Operator", "ReleaseId" })
        {
            Assert.Contains(field, script, StringComparison.Ordinal);
        }

        Assert.Contains("new-admin-vps-smoke-report.ps1", guide, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("password=", script, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Bearer ", script, StringComparison.Ordinal);
        Assert.DoesNotContain("BEGIN OPENSSH PRIVATE KEY", script, StringComparison.Ordinal);
    }

    [Fact]
    public void Admin_Vps_Browser_Smoke_Should_Run_Only_Explicit_Live_Check_And_Write_Safe_Report()
    {
        var root = FindRepositoryRoot();
        var spec = File.ReadAllText(Path.Combine(root, "frontend", "e2e", "admin-vps-smoke.spec.ts"));
        var config = File.ReadAllText(Path.Combine(root, "frontend", "playwright.vps-smoke.config.ts"));
        var packageJson = File.ReadAllText(Path.Combine(root, "frontend", "package.json"));
        var script = File.ReadAllText(Path.Combine(root, "scripts", "admin-vps-browser-smoke.ps1"));
        var guide = File.ReadAllText(Path.Combine(root, "docs", "admin-vps-smoke.md"));

        foreach (var section in RequiredSections)
        {
            Assert.Contains(section, spec, StringComparison.Ordinal);
        }

        foreach (var expected in new[]
                 {
                     "ADMIN_VPS_SMOKE_API_BASE_URL",
                     "ADMIN_VPS_SMOKE_ADMIN_WEB_URL",
                     "ADMIN_VPS_SMOKE_ADMIN_EMAIL",
                     "ADMIN_VPS_SMOKE_ADMIN_PASSWORD",
                     "ADMIN_VPS_SMOKE_REPORT_PATH",
                     "No credentials, cookies, auth headers, tokens or screenshots are stored",
                     "Завершить сессию",
                     "validate-admin-vps-smoke-report.ps1",
                     "Password: [hidden]"
                 })
        {
            Assert.Contains(expected, spec + config + packageJson + script + guide, StringComparison.OrdinalIgnoreCase);
        }

        Assert.Contains("trace: 'off'", config, StringComparison.Ordinal);
        Assert.Contains("screenshot: 'off'", config, StringComparison.Ordinal);
        Assert.Contains("video: 'off'", config, StringComparison.Ordinal);
        Assert.Contains("e2e:admin-vps-smoke", packageJson, StringComparison.Ordinal);
        Assert.DoesNotContain("console.log(password", spec, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Write-Host \"Password: $env:ADMIN_VPS_SMOKE_ADMIN_PASSWORD", script, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Local_Admin_Vps_Browser_Smoke_Should_Start_Temporary_Sqlite_And_Validate_Report()
    {
        var root = FindRepositoryRoot();
        var script = File.ReadAllText(Path.Combine(root, "scripts", "local-admin-vps-browser-smoke.ps1"));
        var guide = File.ReadAllText(Path.Combine(root, "docs", "admin-vps-smoke.md"));

        foreach (var expected in new[]
                 {
                     "Database__UseEnsureCreatedForLocalSqlite",
                     "Database__SeedDemoData",
                     "AdminBootstrap__Enabled",
                     "fresh-admin@example.test",
                     "VITE_API_BASE_URL",
                     "admin-vps-browser-smoke.ps1",
                     "ADMIN_VPS_SMOKE_ADMIN_PASSWORD",
                     "-AccountBootstrapChecked",
                     "-RequireAllPassed",
                     "Assert-InWorkspace",
                     "Stop-Process",
                     "local admin vps browser smoke ok"
                 })
        {
            Assert.Contains(expected, script, StringComparison.OrdinalIgnoreCase);
        }

        Assert.Contains("local-admin-vps-browser-smoke.ps1", guide, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Write-Host \"Password: $password", script, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Admin_Vps_Smoke_Validator_Should_Reject_Placeholder_Evidence_And_Bad_Status_In_Acceptance_Mode()
    {
        var root = FindRepositoryRoot();
        var script = File.ReadAllText(Path.Combine(root, "scripts", "validate-admin-vps-smoke-report.ps1"));
        var guide = File.ReadAllText(Path.Combine(root, "docs", "admin-vps-smoke.md"));
        var roadmap = File.ReadAllText(Path.Combine(root, "docs", "PRODUCT_COMPLETION_ROADMAP.md"));

        foreach (var expected in new[]
                 {
                     "placeholderEvidenceMarkers",
                     "TODO",
                     "Not checked yet",
                     "safe screenshot name",
                     "browser smoke note",
                     "must contain successful httpStatus when -RequireAllPassed is used",
                     "must contain real evidence without placeholder markers when -RequireAllPassed is used"
                 })
        {
            Assert.Contains(expected, script, StringComparison.OrdinalIgnoreCase);
        }

        Assert.Contains("real evidence", guide, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("placeholder", guide, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("[x] `P0-ADMIN-002C`", roadmap, StringComparison.Ordinal);
    }

    [Fact]
    public void Admin_Vps_Smoke_Docs_Should_Link_To_Roadmap_And_Docs_Index()
    {
        var root = FindRepositoryRoot();
        var docsIndex = File.ReadAllText(Path.Combine(root, "docs", "README.md"));
        var guide = File.ReadAllText(Path.Combine(root, "docs", "admin-vps-smoke.md"));
        var roadmap = File.ReadAllText(Path.Combine(root, "docs", "PRODUCT_COMPLETION_ROADMAP.md"));

        Assert.Contains("admin-vps-smoke.md", docsIndex, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("admin-vps-smoke-report.template.json", guide, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("validate-admin-vps-smoke-report.ps1", guide, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("[x] `P0-ADMIN-003`", roadmap, StringComparison.Ordinal);
        Assert.Contains("admin-vps-smoke-report.template.json", roadmap, StringComparison.OrdinalIgnoreCase);
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "README.md"))
                && File.Exists(Path.Combine(directory.FullName, "CHANGELOG.md"))
                && Directory.Exists(Path.Combine(directory.FullName, "backend")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Repository root was not found for admin VPS smoke report tests.");
    }
}
