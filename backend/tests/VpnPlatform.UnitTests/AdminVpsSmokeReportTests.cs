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
        Assert.Contains("@", json.RootElement.GetProperty("adminEmail").GetString(), StringComparison.Ordinal);
        Assert.Contains("admin-vps-smoke-report.json", json.RootElement.GetProperty("smokeReportPath").GetString(), StringComparison.OrdinalIgnoreCase);

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

        foreach (var expected in new[]
                 {
                     "Assert-ReportHttpUrl",
                     "admin-vps-smoke-sections.json",
                     "Get-SectionsContract",
                     "sections contract",
                     "apiBaseUrl",
                     "adminWebUrl",
                     "adminEmail",
                     "smokeReportPath",
                     "accountBootstrapChecked",
                     "adminLoginPassed",
                     "noJsErrors",
                     "noUnauthorizedAfterLogin",
                     "RequireAllPassed",
                     "mismatch for smokeReportPath",
                     "must be passed when -RequireAllPassed is used",
                     "route must match sections contract"
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
                     "WriteAllText",
                     "UTF8Encoding",
                     "smokeReportPath",
                     "blocked",
                     "TODO: open",
                     "Output file already exists. Pass -Force",
                     "Get-LatestReleaseId",
                     "DateTimeOffset]::Parse",
                     "CultureInfo]::InvariantCulture"
                 })
        {
            Assert.Contains(expected, script, StringComparison.OrdinalIgnoreCase);
        }

        foreach (var field in new[] { "OutputPath", "ApiBaseUrl", "AdminWebUrl", "AdminEmail", "EnvironmentName", "Operator", "ReleaseId" })
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
                     "Get-LatestReleaseId",
                     "DateTimeOffset]::Parse",
                     "CultureInfo]::InvariantCulture",
                     "Release id:",
                     "No credentials, cookies, auth headers, tokens or screenshots are stored",
                     "adminEmail",
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
    public void Admin_Vps_Smoke_Sections_Contract_Should_Link_Manifest_Template_And_Browser_Specs()
    {
        var root = FindRepositoryRoot();
        using var contract = JsonDocument.Parse(File.ReadAllText(Path.Combine(root, "docs", "admin-vps-smoke-sections.json")));
        var validator = File.ReadAllText(Path.Combine(root, "scripts", "validate-admin-vps-smoke-sections-contract.ps1"));
        var regression = File.ReadAllText(Path.Combine(root, "scripts", "test-admin-vps-smoke-sections-contract.ps1"));
        var spec = File.ReadAllText(Path.Combine(root, "frontend", "e2e", "admin-vps-smoke.spec.ts"));
        var guide = File.ReadAllText(Path.Combine(root, "docs", "admin-vps-smoke.md"));
        var roadmap = File.ReadAllText(Path.Combine(root, "docs", "PRODUCT_COMPLETION_ROADMAP.md"));

        Assert.Equal("admin-vps-smoke-sections", contract.RootElement.GetProperty("contractId").GetString());
        var sections = contract.RootElement.GetProperty("sections").EnumerateArray().ToArray();
        Assert.Equal(RequiredSections.Order(StringComparer.Ordinal), sections.Select(x => x.GetProperty("id").GetString()).Order(StringComparer.Ordinal));
        Assert.All(sections, section =>
        {
            var id = section.GetProperty("id").GetString();
            Assert.Equal($"/admin/#{id}", section.GetProperty("route").GetString());
        });

        foreach (var expected in new[]
                 {
                     "admin-vps-smoke-sections.json",
                     "admin-vps-smoke-report.template.json",
                     "validate-admin-vps-smoke-report.ps1",
                     "admin-vps-smoke.spec.ts",
                     "all-screens.spec.ts",
                     "route: section.route",
                     "admin vps smoke sections contract valid"
                 })
        {
            Assert.Contains(expected, validator + spec + guide, StringComparison.OrdinalIgnoreCase);
        }

        foreach (var expected in new[]
                 {
                     "admin-vps-smoke-sections-contract-regression-test",
                     "duplicate-section",
                     "bad-route",
                     "template-missing-section",
                     "browser-spec-no-manifest",
                     "all-screens-missing-section",
                     "admin vps smoke sections contract regression passed"
                 })
        {
            Assert.Contains(expected, regression, StringComparison.OrdinalIgnoreCase);
        }

        Assert.Contains("test-admin-vps-smoke-sections-contract.ps1", guide, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("[x] `P0-ADMIN-002K`", roadmap, StringComparison.Ordinal);
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
                     "admin-vps-smoke.ps1",
                     "PreflightReportPath",
                     "ADMIN_VPS_SMOKE_ADMIN_PASSWORD",
                     "-AccountBootstrapChecked",
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
    public void Admin_Vps_Smoke_Preflight_Should_Check_Live_Inputs_Without_Printing_Secrets()
    {
        var root = FindRepositoryRoot();
        var script = File.ReadAllText(Path.Combine(root, "scripts", "admin-vps-smoke-preflight.ps1"));
        var guide = File.ReadAllText(Path.Combine(root, "docs", "admin-vps-smoke.md"));
        var roadmap = File.ReadAllText(Path.Combine(root, "docs", "PRODUCT_COMPLETION_ROADMAP.md"));

        foreach (var expected in new[]
                 {
                     "ADMIN_VPS_SMOKE_API_BASE_URL",
                     "ADMIN_VPS_SMOKE_ADMIN_WEB_URL",
                     "ADMIN_VPS_SMOKE_ADMIN_EMAIL",
                     "ADMIN_VPS_SMOKE_ADMIN_PASSWORD",
                     "passwordEnvPresent",
                     "readyForLiveSmoke",
                     "e2e:admin-vps-smoke",
                     "validate-admin-vps-smoke-report.ps1",
                     "validate-admin-vps-smoke-preflight-report.ps1",
                     "preflight-validator",
                     "Get-LatestReleaseId",
                     "DateTimeOffset]::Parse",
                     "CultureInfo]::InvariantCulture",
                     "manual-admin-vps-smoke-preflight",
                     "-RequireReady",
                     "present [hidden]",
                     "admin-vps-smoke-preflight-report.json"
                 })
        {
            Assert.Contains(expected, script, StringComparison.OrdinalIgnoreCase);
        }

        Assert.DoesNotContain("[string]$Password", script, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Write-Host \"Password: $env:ADMIN_VPS_SMOKE_ADMIN_PASSWORD", script, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("admin-vps-smoke-preflight.ps1", guide, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("[x] `P0-ADMIN-002D`", roadmap, StringComparison.Ordinal);
    }

    [Fact]
    public void Admin_Vps_Smoke_Preflight_Validator_Should_Fail_Closed_On_Readiness_And_Secrets()
    {
        var root = FindRepositoryRoot();
        var script = File.ReadAllText(Path.Combine(root, "scripts", "validate-admin-vps-smoke-preflight-report.ps1"));
        var preflight = File.ReadAllText(Path.Combine(root, "scripts", "admin-vps-smoke-preflight.ps1"));
        var guide = File.ReadAllText(Path.Combine(root, "docs", "admin-vps-smoke.md"));
        var roadmap = File.ReadAllText(Path.Combine(root, "docs", "PRODUCT_COMPLETION_ROADMAP.md"));

        foreach (var expected in new[]
                 {
                     "RequireReady",
                     "passwordEnvPresent",
                     "readyForLiveSmoke",
                     "api-base-url",
                     "admin-web-url",
                     "admin-email",
                     "preflight-validator",
                     "contains forbidden secret marker",
                     "field is empty: releaseId",
                     "must be true when -RequireReady is used",
                     "must be passed when -RequireReady is used",
                     "admin vps smoke preflight report valid"
                 })
        {
            Assert.Contains(expected, script + preflight + guide, StringComparison.OrdinalIgnoreCase);
        }

        Assert.DoesNotContain("[string]$Password", script, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("validate-admin-vps-smoke-preflight-report.ps1", guide, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("[x] `P0-ADMIN-002E`", roadmap, StringComparison.Ordinal);
    }

    [Fact]
    public void Admin_Vps_Smoke_Preflight_Validator_Regression_Should_Cover_Tamper_Scenarios()
    {
        var root = FindRepositoryRoot();
        var script = File.ReadAllText(Path.Combine(root, "scripts", "test-admin-vps-smoke-preflight-validator.ps1"));
        var guide = File.ReadAllText(Path.Combine(root, "docs", "admin-vps-smoke.md"));
        var roadmap = File.ReadAllText(Path.Combine(root, "docs", "PRODUCT_COMPLETION_ROADMAP.md"));

        foreach (var expected in new[]
                 {
                     "admin-vps-smoke-preflight-validator-regression-test",
                     "validate-admin-vps-smoke-preflight-report.ps1",
                     "Assert-FailsWith",
                     "empty-release-id",
                     "bad-ready-flag",
                     "failed-check",
                     "missing-check",
                     "duplicate-check",
                     "secret-marker",
                     "LocalAdminPassword123!",
                     "Admin VPS smoke preflight regression report leaked password",
                     "admin vps smoke preflight validator regression passed"
                 })
        {
            Assert.Contains(expected, script, StringComparison.OrdinalIgnoreCase);
        }

        Assert.Contains("test-admin-vps-smoke-preflight-validator.ps1", guide, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("[x] `P0-ADMIN-002F`", roadmap, StringComparison.Ordinal);
    }

    [Fact]
    public void Admin_Vps_Smoke_Report_Validator_Regression_Should_Cover_Tamper_Scenarios()
    {
        var root = FindRepositoryRoot();
        var script = File.ReadAllText(Path.Combine(root, "scripts", "test-admin-vps-smoke-report-validator.ps1"));
        var guide = File.ReadAllText(Path.Combine(root, "docs", "admin-vps-smoke.md"));
        var roadmap = File.ReadAllText(Path.Combine(root, "docs", "PRODUCT_COMPLETION_ROADMAP.md"));

        foreach (var expected in new[]
                 {
                     "admin-vps-smoke-report-validator-regression-test",
                     "validate-admin-vps-smoke-report.ps1",
                     "Assert-FailsWith",
                     "smokeReportPath",
                     "mismatched-smoke-report-path",
                     "bad-http-status",
                     "bad-route",
                     "placeholder-evidence",
                     "failed-status",
                     "missing-section",
                     "false-gate",
                     "secret-marker",
                     "Synthetic sanitized validator regression report",
                     "admin vps smoke report validator regression passed"
                 })
        {
            Assert.Contains(expected, script, StringComparison.OrdinalIgnoreCase);
        }

        Assert.Contains("test-admin-vps-smoke-report-validator.ps1", guide, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("[x] `P0-ADMIN-002G`", roadmap, StringComparison.Ordinal);
    }

    [Fact]
    public void Admin_Vps_Smoke_Flow_Should_Run_Preflight_Before_Browser_Smoke()
    {
        var root = FindRepositoryRoot();
        var script = File.ReadAllText(Path.Combine(root, "scripts", "admin-vps-smoke.ps1"));
        var localSmoke = File.ReadAllText(Path.Combine(root, "scripts", "local-admin-vps-browser-smoke.ps1"));
        var guide = File.ReadAllText(Path.Combine(root, "docs", "admin-vps-smoke.md"));
        var roadmap = File.ReadAllText(Path.Combine(root, "docs", "PRODUCT_COMPLETION_ROADMAP.md"));

        foreach (var expected in new[]
                 {
                     "admin-vps-smoke-preflight.ps1",
                     "admin-vps-browser-smoke.ps1",
                     "validate-admin-vps-smoke-report.ps1",
                     "validate-admin-vps-smoke-preflight-report.ps1",
                     "validate-admin-vps-smoke-evidence.ps1",
                     "Get-LatestReleaseId",
                     "DateTimeOffset]::Parse",
                     "CultureInfo]::InvariantCulture",
                     "releaseValue",
                     "-RequirePassword",
                     "-RequireAllPassed",
                     "Password: [hidden]",
                     "Release id:",
                     "Admin VPS smoke flow completed",
                     "admin-vps-smoke-preflight-report.json"
                 })
        {
            Assert.Contains(expected, script, StringComparison.OrdinalIgnoreCase);
        }

        Assert.True(
            script.IndexOf("admin-vps-smoke-preflight.ps1", StringComparison.OrdinalIgnoreCase)
            < script.IndexOf("admin-vps-browser-smoke.ps1", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain("[string]$Password", script, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("ADMIN_VPS_SMOKE_ADMIN_PASSWORD\"", script, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("admin-vps-smoke.ps1", localSmoke, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("admin-vps-smoke.ps1", guide, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("[x] `P0-ADMIN-002H`", roadmap, StringComparison.Ordinal);
    }

    [Fact]
    public void Admin_Vps_Smoke_Evidence_Validator_Should_Link_Preflight_And_Smoke_Reports()
    {
        var root = FindRepositoryRoot();
        var validator = File.ReadAllText(Path.Combine(root, "scripts", "validate-admin-vps-smoke-evidence.ps1"));
        var regression = File.ReadAllText(Path.Combine(root, "scripts", "test-admin-vps-smoke-evidence-validator.ps1"));
        var flow = File.ReadAllText(Path.Combine(root, "scripts", "admin-vps-smoke.ps1"));
        var guide = File.ReadAllText(Path.Combine(root, "docs", "admin-vps-smoke.md"));
        var roadmap = File.ReadAllText(Path.Combine(root, "docs", "PRODUCT_COMPLETION_ROADMAP.md"));

        foreach (var expected in new[]
                 {
                     "validate-admin-vps-smoke-preflight-report.ps1",
                     "validate-admin-vps-smoke-report.ps1",
                     "RequireReady",
                     "RequireAllPassed",
                     "apiBaseUrl",
                     "adminWebUrl",
                     "adminEmail",
                     "environmentName",
                     "operator",
                     "sectionsContractPath",
                     "sectionsContractPath = $sectionsContractPath",
                     "preflightReportSha256",
                     "smokeReportSha256",
                     "ExpectedPreflightReportSha256",
                     "ExpectedSmokeReportSha256",
                     "Assert-ExpectedSha256",
                     "does not match expected SHA256",
                     "smokeReportPath",
                     "preflightReportPath",
                     "preflightReportPath = $preflightFullPath",
                     "releaseId",
                     "preflight releaseId is required",
                     "generatedAt must not be after smoke completedAt",
                     "smoke startedAt must not be before preflight generatedAt",
                     "admin vps smoke evidence valid"
                 })
        {
            Assert.Contains(expected, validator + flow, StringComparison.OrdinalIgnoreCase);
        }

        foreach (var expected in new[]
                 {
                     "admin-vps-smoke-evidence-validator-regression-test",
                     "mismatched-api-url",
                     "mismatched-expected-preflight-sha256",
                     "mismatched-admin-email",
                     "mismatched-smoke-report-path",
                     "mismatched-preflight-report-path",
                     "mismatched-release-id",
                     "missing-preflight-release-id",
                     "preflight-after-smoke",
                     "smoke-started-before-preflight",
                     "failed-smoke-report",
                     "Valid smoke evidence output must include preflightReportPath",
                     "Valid smoke evidence output must include sectionsContractPath",
                     "Valid smoke evidence output must include preflightReportSha256",
                     "Valid smoke evidence output must include smokeReportSha256",
                     "Valid smoke evidence output with expected SHA256 must pass",
                     "admin vps smoke evidence validator regression passed"
                 })
        {
            Assert.Contains(expected, regression, StringComparison.OrdinalIgnoreCase);
        }

        Assert.Contains("validate-admin-vps-smoke-evidence.ps1", guide, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("test-admin-vps-smoke-evidence-validator.ps1", guide, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("[x] `P0-ADMIN-002J`", roadmap, StringComparison.Ordinal);
    }

    [Fact]
    public void Admin_Vps_Smoke_Flow_Regression_Should_Fail_Closed_Before_Browser_Smoke()
    {
        var root = FindRepositoryRoot();
        var script = File.ReadAllText(Path.Combine(root, "scripts", "test-admin-vps-smoke-flow-wrapper.ps1"));
        var guide = File.ReadAllText(Path.Combine(root, "docs", "admin-vps-smoke.md"));
        var roadmap = File.ReadAllText(Path.Combine(root, "docs", "PRODUCT_COMPLETION_ROADMAP.md"));

        foreach (var expected in new[]
                 {
                     "admin-vps-smoke-flow-wrapper-regression-test",
                     "admin-vps-smoke.ps1",
                     "Invoke-WrapperFailure",
                     "missing-password",
                     "bad-api-url",
                     "missing-frontend",
                     "password-env-present",
                     "apiBaseUrl must be an absolute",
                     "api-base-url",
                     "frontend-directory",
                     "readyForLiveSmoke must be true",
                     "Admin VPS browser smoke is ready to run.",
                     "e2e:admin-vps-smoke",
                     "Smoke report should not exist after failed preflight",
                     "Preflight report releaseId should be resolved",
                     "leaked password",
                     "admin vps smoke flow wrapper regression passed"
                 })
        {
            Assert.Contains(expected, script, StringComparison.OrdinalIgnoreCase);
        }

        Assert.Contains("test-admin-vps-smoke-flow-wrapper.ps1", guide, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("[x] `P0-ADMIN-002I`", roadmap, StringComparison.Ordinal);
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
