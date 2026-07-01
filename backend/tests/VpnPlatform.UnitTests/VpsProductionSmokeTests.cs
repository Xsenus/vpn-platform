using System.Text.Json;
using Xunit;

namespace VpnPlatform.UnitTests;

public class VpsProductionSmokeTests
{
    private static readonly string[] RequiredReportSteps =
    [
        "health-live",
        "health-ready",
        "web-public",
        "web-cabinet",
        "web-admin",
        "admin-login",
        "public-checkout",
        "payment-init",
        "payment-confirmation",
        "subscription-active",
        "vpn-access",
        "latest-release"
    ];

    [Fact]
    public void Vps_Production_Smoke_Script_Should_Cover_Full_Order_Payment_Subscription_And_Access_Flow()
    {
        var root = FindRepositoryRoot();
        var script = File.ReadAllText(Path.Combine(root, "scripts", "vps-production-smoke.ps1"));

        foreach (var expected in new[]
                 {
                     "ApiBaseUrl",
                     "PublicWebUrl",
                     "CabinetWebUrl",
                     "AdminWebUrl",
                     "AdminEmail",
                     "AdminPassword",
                     "RequireWebApps",
                     "AllowSandboxWebhook",
                     "/health/live",
                     "/health/ready",
                     "/api/auth/login",
                     "/api/admin/dashboard/summary",
                     "/api/public/tariffs",
                     "/api/public/payments/providers",
                     "/api/public/checkout-sessions",
                     "/api/auth/register",
                     "/api/me/checkout-sessions/",
                     "/payments/$PaymentProvider/init",
                     "/api/webhooks/payments/",
                     "X-YooKassa-Sandbox-Webhook",
                     "/api/me/orders",
                     "/api/me/payments",
                     "/api/me/subscriptions",
                     "/api/me/accesses",
                     "/api/app-version/latest",
                     "vless://"
                 })
        {
            Assert.Contains(expected, script, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void Vps_Production_Smoke_Should_Be_Fail_Closed_For_Production_Sandbox_Webhook()
    {
        var root = FindRepositoryRoot();
        var script = File.ReadAllText(Path.Combine(root, "scripts", "vps-production-smoke.ps1"));

        Assert.Contains("if (-not $AllowSandboxWebhook)", script, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("partial ok", script, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("if ([string]$live.environment -eq \"Production\")", script, StringComparison.Ordinal);
        Assert.Contains("-AllowSandboxWebhook is forbidden", script, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("complete-provider-payment-or-run-with-AllowSandboxWebhook-on-non-production-sandbox", script, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Vps_Production_Smoke_Documentation_Should_Explain_Local_Dry_Run_Live_Run_And_Secrets()
    {
        var root = FindRepositoryRoot();
        var guide = File.ReadAllText(Path.Combine(root, "docs", "vps-production-smoke.md"));
        var docsIndex = File.ReadAllText(Path.Combine(root, "docs", "README.md"));

        foreach (var expected in new[]
                 {
                     "P11-ACC-002",
                     "scripts/vps-production-smoke.ps1",
                     "dry-run",
                     "SQLite",
                     "83.147.222.145",
                     "partial ok",
                     "Succeeded",
                     "active subscription",
                     "VPN access",
                     "vless://",
                     "рота",
                     "2026-06-14-vps-production-smoke-runner"
                 })
        {
            Assert.Contains(expected, guide, StringComparison.OrdinalIgnoreCase);
        }

        Assert.Contains("vps-production-smoke.md", docsIndex, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Vps_Production_Smoke_Report_Template_Should_List_Full_Flow_Fail_Closed()
    {
        var root = FindRepositoryRoot();
        using var json = JsonDocument.Parse(File.ReadAllText(Path.Combine(root, "docs", "vps-production-smoke-report.template.json")));

        foreach (var booleanName in new[]
                 {
                     "liveHealthPassed",
                     "readyHealthPassed",
                     "adminLoginPassed",
                     "checkoutCreated",
                     "paymentInitialized",
                     "paymentConfirmed",
                     "subscriptionActivated",
                     "vpnAccessIssued",
                     "latestReleaseMatched",
                     "noJsErrors",
                     "noSecretsInEvidence"
                 })
        {
            Assert.False(json.RootElement.GetProperty(booleanName).GetBoolean());
        }

        var steps = json.RootElement.GetProperty("steps").EnumerateArray().ToArray();
        Assert.Equal(RequiredReportSteps.Order(StringComparer.Ordinal), steps.Select(x => x.GetProperty("id").GetString()).Order(StringComparer.Ordinal));
        Assert.All(steps, step =>
        {
            Assert.Equal("blocked", step.GetProperty("status").GetString());
            Assert.Equal(0, step.GetProperty("httpStatus").GetInt32());
            Assert.Contains("TODO:", step.GetProperty("evidence").GetString(), StringComparison.OrdinalIgnoreCase);
        });
    }

    [Fact]
    public void Vps_Production_Smoke_Report_Validator_Should_Require_Full_Flow_And_Forbid_Secrets()
    {
        var root = FindRepositoryRoot();
        var script = File.ReadAllText(Path.Combine(root, "scripts", "validate-vps-production-smoke-report.ps1"));

        foreach (var step in RequiredReportSteps)
        {
            Assert.Contains(step, script, StringComparison.Ordinal);
        }

        foreach (var expected in new[]
                 {
                     "Assert-ReportHttpUrl",
                     "apiBaseUrl",
                     "publicWebUrl",
                     "cabinetWebUrl",
                     "adminWebUrl",
                     "RequireAllPassed",
                     "must be true when -RequireAllPassed is used",
                     "must be passed when -RequireAllPassed is used",
                     "VPS production smoke report contains forbidden secret marker",
                     "vless://",
                     "vmess://",
                     "trojan://"
                 })
        {
            Assert.Contains(expected, script, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void Vps_Production_Smoke_Report_Generator_Should_Create_Safe_Blocked_Report()
    {
        var root = FindRepositoryRoot();
        var script = File.ReadAllText(Path.Combine(root, "scripts", "new-vps-production-smoke-report.ps1"));
        var guide = File.ReadAllText(Path.Combine(root, "docs", "vps-production-smoke.md"));

        foreach (var expected in new[]
                 {
                     "vps-production-smoke-report.template.json",
                     "validate-vps-production-smoke-report.ps1",
                     "ConvertTo-Json -Depth 8",
                     "Set-Content",
                     "-Encoding UTF8",
                     "blocked",
                     "TODO: run",
                     "Output file already exists. Pass -Force",
                     "Get-LatestReleaseId",
                     "Assert-KnownReleaseId",
                     "DateTimeOffset]::Parse",
                     "CultureInfo]::InvariantCulture",
                     "ReleaseId must exist in backend/src/VpnPlatform.Api/AppReleases/releases.json"
                 })
        {
            Assert.Contains(expected, script, StringComparison.OrdinalIgnoreCase);
        }

        foreach (var field in new[] { "OutputPath", "ApiBaseUrl", "PublicWebUrl", "CabinetWebUrl", "AdminWebUrl", "EnvironmentName", "Operator", "ReleaseId" })
        {
            Assert.Contains(field, script, StringComparison.Ordinal);
        }

        Assert.Contains("new-vps-production-smoke-report.ps1", guide, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("test-vps-production-smoke-report-generator-release-guard.ps1", guide, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("password=", script, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Bearer ", script, StringComparison.Ordinal);
        Assert.DoesNotContain("BEGIN OPENSSH PRIVATE KEY", script, StringComparison.Ordinal);
    }

    [Fact]
    public void Vps_Production_Smoke_Report_Generator_Should_Reject_Unknown_Manual_ReleaseId()
    {
        var root = FindRepositoryRoot();
        var script = File.ReadAllText(Path.Combine(root, "scripts", "new-vps-production-smoke-report.ps1"));
        var regression = File.ReadAllText(Path.Combine(root, "scripts", "test-vps-production-smoke-report-generator-release-guard.ps1"));
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
        Assert.Contains("vps production smoke generator release guard valid", regression, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("[x] `P11-ACC-075`", roadmap, StringComparison.Ordinal);
    }

    [Fact]
    public void Vps_Production_Smoke_Report_Validator_Should_Reject_Stale_Release_In_Acceptance_Mode()
    {
        var root = FindRepositoryRoot();
        var script = File.ReadAllText(Path.Combine(root, "scripts", "validate-vps-production-smoke-report.ps1"));
        var regression = File.ReadAllText(Path.Combine(root, "scripts", "test-vps-production-smoke-report-latest-release-guard.ps1"));
        var guide = File.ReadAllText(Path.Combine(root, "docs", "vps-production-smoke.md"));
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
        Assert.Contains("must match latest active release", regression, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("latest active release", guide, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("[x] `P11-ACC-065`", roadmap, StringComparison.Ordinal);
    }

    [Fact]
    public void Vps_Production_Smoke_Report_Docs_Should_Link_To_Roadmap_And_Docs_Index()
    {
        var root = FindRepositoryRoot();
        var docsIndex = File.ReadAllText(Path.Combine(root, "docs", "README.md"));
        var guide = File.ReadAllText(Path.Combine(root, "docs", "vps-production-smoke.md"));
        var roadmap = File.ReadAllText(Path.Combine(root, "docs", "PRODUCT_COMPLETION_ROADMAP.md"));

        Assert.Contains("vps-production-smoke-report.template.json", docsIndex, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("vps-production-smoke-report.template.json", guide, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("validate-vps-production-smoke-report.ps1", guide, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("[x] `P11-ACC-064`", roadmap, StringComparison.Ordinal);
        Assert.Contains("vps-production-smoke-report.template.json", roadmap, StringComparison.OrdinalIgnoreCase);
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

        throw new InvalidOperationException("Repository root was not found for VPS production smoke tests.");
    }
}
