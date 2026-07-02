using System.Text.Json;
using Xunit;

namespace VpnPlatform.UnitTests;

public class VpnLiveSmokeReportTests
{
    private static readonly string[] RequiredChecks =
    [
        "panel-connection",
        "inbound-sync",
        "node-ready",
        "order-create",
        "payment-webhook",
        "subscription-activated",
        "vpn-client-created",
        "access-uri-qr",
        "fail-closed-disabled-inbound"
    ];

    [Fact]
    public void Vpn_Live_Smoke_Template_Should_List_All_Checks_Fail_Closed()
    {
        var root = FindRepositoryRoot();
        using var json = JsonDocument.Parse(File.ReadAllText(Path.Combine(root, "docs", "vpn-live-smoke-report.template.json")));

        Assert.False(json.RootElement.GetProperty("panelConnected").GetBoolean());
        Assert.False(json.RootElement.GetProperty("inboundSynced").GetBoolean());
        Assert.False(json.RootElement.GetProperty("nodeReady").GetBoolean());
        Assert.False(json.RootElement.GetProperty("productionProvisioningEnabled").GetBoolean());
        Assert.False(json.RootElement.GetProperty("noSandboxFallback").GetBoolean());
        Assert.False(json.RootElement.GetProperty("failClosedChecked").GetBoolean());
        Assert.Equal("tmp/vpn-live-smoke-report.json", json.RootElement.GetProperty("smokeReportPath").GetString());

        var checks = json.RootElement.GetProperty("checks").EnumerateArray().ToArray();
        Assert.Equal(RequiredChecks.Order(StringComparer.Ordinal), checks.Select(x => x.GetProperty("id").GetString()).Order(StringComparer.Ordinal));
        Assert.All(checks, check =>
        {
            Assert.Equal("blocked", check.GetProperty("status").GetString());
            Assert.Contains("TODO:", check.GetProperty("evidence").GetString(), StringComparison.OrdinalIgnoreCase);
        });
    }

    [Fact]
    public void Vpn_Live_Smoke_Validator_Should_Require_Urls_Gates_And_No_Vpn_Secrets()
    {
        var root = FindRepositoryRoot();
        var script = File.ReadAllText(Path.Combine(root, "scripts", "validate-vpn-live-smoke-report.ps1"));

        foreach (var check in RequiredChecks)
        {
            Assert.Contains(check, script, StringComparison.Ordinal);
        }

        foreach (var expected in new[]
                 {
                     "Assert-ReportHttpUrl",
                     "apiBaseUrl",
                     "adminWebUrl",
                     "x3uiPanelUrl",
                     "panelConnected",
                     "inboundSynced",
                     "nodeReady",
                     "productionProvisioningEnabled",
                     "noSandboxFallback",
                     "failClosedChecked",
                     "smokeReportPath",
                     "RequireAllPassed",
                     "must be passed when -RequireAllPassed is used"
                 })
        {
            Assert.Contains(expected, script, StringComparison.OrdinalIgnoreCase);
        }

        foreach (var forbidden in new[] { "vless://", "vmess://", "trojan://", "BEGIN OPENSSH PRIVATE KEY" })
        {
            Assert.Contains(forbidden, script, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void Vpn_Live_Smoke_Generator_Should_Create_Safe_Blocked_Report()
    {
        var root = FindRepositoryRoot();
        var script = File.ReadAllText(Path.Combine(root, "scripts", "new-vpn-live-smoke-report.ps1"));
        var guide = File.ReadAllText(Path.Combine(root, "docs", "vpn-live-smoke.md"));

        foreach (var expected in new[]
                 {
                     "vpn-live-smoke-report.template.json",
                     "validate-vpn-live-smoke-report.ps1",
                     "ConvertTo-Json -Depth 8",
                     "Set-Content",
                     "-Encoding UTF8",
                     "blocked",
                     "TODO: run live VPN smoke step",
                     "Output file already exists. Pass -Force",
                     "Get-LatestReleaseId",
                     "Assert-KnownReleaseId",
                     "DateTimeOffset]::Parse",
                     "CultureInfo]::InvariantCulture",
                     "ReleaseId must exist in backend/src/VpnPlatform.Api/AppReleases/releases.json",
                     "smokeReportPath"
                 })
        {
            Assert.Contains(expected, script, StringComparison.OrdinalIgnoreCase);
        }

        foreach (var field in new[] { "OutputPath", "ApiBaseUrl", "AdminWebUrl", "X3uiPanelUrl", "EnvironmentName", "Operator", "ReleaseId" })
        {
            Assert.Contains(field, script, StringComparison.Ordinal);
        }

        Assert.Contains("new-vpn-live-smoke-report.ps1", guide, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("test-vpn-live-smoke-report-generator-release-guard.ps1", guide, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("password=", script, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Bearer ", script, StringComparison.Ordinal);
        Assert.DoesNotContain("BEGIN OPENSSH PRIVATE KEY", script, StringComparison.Ordinal);
    }

    [Fact]
    public void Vpn_Live_Smoke_Report_Should_Self_Link_To_Validated_Report_Path()
    {
        var root = FindRepositoryRoot();
        var template = File.ReadAllText(Path.Combine(root, "docs", "vpn-live-smoke-report.template.json"));
        var generator = File.ReadAllText(Path.Combine(root, "scripts", "new-vpn-live-smoke-report.ps1"));
        var validator = File.ReadAllText(Path.Combine(root, "scripts", "validate-vpn-live-smoke-report.ps1"));
        var regression = File.ReadAllText(Path.Combine(root, "scripts", "test-vpn-live-smoke-report-self-link-guard.ps1"));
        var guide = File.ReadAllText(Path.Combine(root, "docs", "vpn-live-smoke.md"));
        var roadmap = File.ReadAllText(Path.Combine(root, "docs", "PRODUCT_COMPLETION_ROADMAP.md"));

        Assert.Contains("\"smokeReportPath\"", template, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("$report.smokeReportPath = $fullOutputPath", generator, StringComparison.Ordinal);
        Assert.Contains("Resolve-WorkspacePath", validator, StringComparison.Ordinal);
        Assert.Contains("smokeReportPath must match ReportPath", validator, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("vpn live smoke report self-link guard valid", regression, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("tmp/other-vpn-live-smoke-report.json", regression, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("VPN live smoke reports include `smokeReportPath`", guide, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("[x] `P0-VPN-011`", roadmap, StringComparison.Ordinal);
    }

    [Fact]
    public void Vpn_Live_Smoke_Generator_Should_Reject_Unknown_Manual_ReleaseId()
    {
        var root = FindRepositoryRoot();
        var script = File.ReadAllText(Path.Combine(root, "scripts", "new-vpn-live-smoke-report.ps1"));
        var regression = File.ReadAllText(Path.Combine(root, "scripts", "test-vpn-live-smoke-report-generator-release-guard.ps1"));
        var roadmap = File.ReadAllText(Path.Combine(root, "docs", "PRODUCT_COMPLETION_ROADMAP.md"));

        foreach (var expected in new[]
                 {
                     "Assert-KnownReleaseId",
                     "AppReleases/releases.json",
                     "ReleaseId must exist in backend/src/VpnPlatform.Api/AppReleases/releases.json"
                 })
        {
            Assert.Contains(expected, script, StringComparison.OrdinalIgnoreCase);
        }

        Assert.Contains("missing-release-id-for-regression", regression, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Generator accepted unknown ReleaseId", regression, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Generator created report artifact after unknown ReleaseId failure", regression, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("vpn live smoke generator release guard valid", regression, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("[x] `P0-VPN-008`", roadmap, StringComparison.Ordinal);
    }

    [Fact]
    public void Vpn_Live_Smoke_Generator_Release_Guard_Should_Cleanup_Default_Tmp()
    {
        var root = FindRepositoryRoot();
        var regression = File.ReadAllText(Path.Combine(root, "scripts", "test-vpn-live-smoke-report-generator-release-guard.ps1"));
        var guide = File.ReadAllText(Path.Combine(root, "docs", "vpn-live-smoke.md"));
        var roadmap = File.ReadAllText(Path.Combine(root, "docs", "PRODUCT_COMPLETION_ROADMAP.md"));

        foreach (var expected in new[]
                 {
                     "Remove-Item -LiteralPath $reportPath -Force",
                     "Get-ChildItem -LiteralPath $tmpDirectory -Force",
                     "Remove-Item -LiteralPath $tmpDirectory -Force"
                 })
        {
            Assert.Contains(expected, regression, StringComparison.OrdinalIgnoreCase);
        }

        Assert.Contains("VPN live smoke generator release guard removes its autogenerated empty `tmp` directory after a local run", guide, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("[x] `P0-VPN-009`", roadmap, StringComparison.Ordinal);
    }

    [Fact]
    public void Vpn_Live_Smoke_Validator_Should_Reject_Stale_Release_In_Acceptance_Mode()
    {
        var root = FindRepositoryRoot();
        var script = File.ReadAllText(Path.Combine(root, "scripts", "validate-vpn-live-smoke-report.ps1"));
        var regression = File.ReadAllText(Path.Combine(root, "scripts", "test-vpn-live-smoke-report-latest-release-guard.ps1"));
        var guide = File.ReadAllText(Path.Combine(root, "docs", "vpn-live-smoke.md"));
        var roadmap = File.ReadAllText(Path.Combine(root, "docs", "PRODUCT_COMPLETION_ROADMAP.md"));

        foreach (var expected in new[]
                 {
                     "Get-LatestActiveReleaseId",
                     "AppReleases/releases.json",
                     "must match latest active release",
                     "-RequireAllPassed"
                 })
        {
            Assert.Contains(expected, script, StringComparison.OrdinalIgnoreCase);
        }

        Assert.Contains("stale-release-id", regression, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("smokeReportPath", regression, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("must match latest active release", regression, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("latest active release", guide, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("[x] `P0-VPN-007`", roadmap, StringComparison.Ordinal);
    }

    [Fact]
    public void Vpn_Live_Smoke_Latest_Release_Guard_Should_Cleanup_Default_Tmp()
    {
        var root = FindRepositoryRoot();
        var regression = File.ReadAllText(Path.Combine(root, "scripts", "test-vpn-live-smoke-report-latest-release-guard.ps1"));
        var guide = File.ReadAllText(Path.Combine(root, "docs", "vpn-live-smoke.md"));
        var roadmap = File.ReadAllText(Path.Combine(root, "docs", "PRODUCT_COMPLETION_ROADMAP.md"));

        foreach (var expected in new[]
                 {
                     "Remove-Item -LiteralPath $reportPath -Force",
                     "Get-ChildItem -LiteralPath $tmpDirectory -Force",
                     "Remove-Item -LiteralPath $tmpDirectory -Force"
                 })
        {
            Assert.Contains(expected, regression, StringComparison.OrdinalIgnoreCase);
        }

        Assert.Contains("VPN live smoke latest release guard removes its autogenerated `tmp/vpn-live-smoke-stale-release-guard.json` report and empty `tmp` directory after a local run", guide, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("[x] `P0-VPN-010`", roadmap, StringComparison.Ordinal);
    }

    [Fact]
    public void Vpn_Live_Smoke_Docs_Should_Link_To_Roadmap_And_Docs_Index()
    {
        var root = FindRepositoryRoot();
        var docsIndex = File.ReadAllText(Path.Combine(root, "docs", "README.md"));
        var guide = File.ReadAllText(Path.Combine(root, "docs", "vpn-live-smoke.md"));
        var roadmap = File.ReadAllText(Path.Combine(root, "docs", "PRODUCT_COMPLETION_ROADMAP.md"));

        Assert.Contains("vpn-live-smoke.md", docsIndex, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("vpn-live-smoke-report.template.json", guide, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("validate-vpn-live-smoke-report.ps1", guide, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("[x] `P0-VPN-006`", roadmap, StringComparison.Ordinal);
        Assert.Contains("vpn-live-smoke-report.template.json", roadmap, StringComparison.OrdinalIgnoreCase);
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

        throw new InvalidOperationException("Repository root was not found for VPN live smoke report tests.");
    }
}
