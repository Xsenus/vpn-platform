using Xunit;

namespace VpnPlatform.UnitTests;

public class VpsProductionSmokeTests
{
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
