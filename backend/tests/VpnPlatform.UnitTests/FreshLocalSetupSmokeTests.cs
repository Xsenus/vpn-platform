using Xunit;

namespace VpnPlatform.UnitTests;

public class FreshLocalSetupSmokeTests
{
    [Fact]
    public void Fresh_Local_Smoke_Should_Cover_Clean_Sqlite_Sandbox_Purchase_And_Vpn_Access()
    {
        var root = FindRepositoryRoot();
        var script = File.ReadAllText(Path.Combine(root, "scripts", "fresh-local-smoke.ps1"));
        var guide = File.ReadAllText(Path.Combine(root, "docs", "fresh-local-smoke.md"));
        var docsIndex = File.ReadAllText(Path.Combine(root, "docs", "README.md"));
        var meController = File.ReadAllText(Path.Combine(root, "backend", "src", "VpnPlatform.Api", "Controllers", "Me", "MeController.cs"));

        foreach (var required in new[]
        {
            "Database__UseEnsureCreatedForLocalSqlite",
            "Database__SeedDemoData",
            "/health/live",
            "/health/ready",
            "/api/public/tariffs",
            "/api/public/payments/providers",
            "/api/public/checkout-sessions",
            "/api/auth/register",
            "/api/me/checkout-sessions/",
            "/payments/YooKassa/init",
            "X-YooKassa-Sandbox-Webhook",
            "/api/webhooks/payments/yookassa",
            "/api/me/orders",
            "/api/me/payments",
            "/api/me/subscriptions",
            "/api/me/accesses",
            "vless://",
            "Stop-Process",
            "Assert-InWorkspace"
        })
        {
            Assert.Contains(required, script, StringComparison.OrdinalIgnoreCase);
        }

        foreach (var required in new[]
        {
            "P11-ACC-001",
            "чистого локального запуска",
            "checkout",
            "sandbox-провайдеры",
            "VPN-доступ",
            "fresh-local-smoke.ps1"
        })
        {
            Assert.Contains(required, guide, StringComparison.OrdinalIgnoreCase);
        }

        Assert.Contains("fresh-local-smoke.md", docsIndex, StringComparison.OrdinalIgnoreCase);

        var ordersActionStart = meController.IndexOf("[HttpGet(\"orders\")]", StringComparison.Ordinal);
        var createOrderActionStart = meController.IndexOf("[HttpPost(\"orders\")]", StringComparison.Ordinal);
        Assert.True(ordersActionStart >= 0 && createOrderActionStart > ordersActionStart);
        var ordersAction = meController[ordersActionStart..createOrderActionStart];
        Assert.True(
            ordersAction.IndexOf("ToListAsync(cancellationToken)", StringComparison.Ordinal) <
            ordersAction.IndexOf("OrderByDescending(x => x.CreatedAt)", StringComparison.Ordinal),
            "/api/me/orders must materialize before DateTimeOffset ordering to stay SQLite-compatible.");
    }

    [Fact]
    public void Fresh_Local_Smoke_Should_Cleanup_Default_Tmp()
    {
        var root = FindRepositoryRoot();
        var script = File.ReadAllText(Path.Combine(root, "scripts", "fresh-local-smoke.ps1"));
        var guide = File.ReadAllText(Path.Combine(root, "docs", "fresh-local-smoke.md"));
        var roadmap = File.ReadAllText(Path.Combine(root, "docs", "PRODUCT_COMPLETION_ROADMAP.md"));

        foreach (var expected in new[]
                 {
                     "$tmpRoot = Join-Path $root \"tmp\"",
                     "Test-Path -LiteralPath $tmpRoot",
                     "Get-ChildItem -LiteralPath $tmpRoot -Force",
                     "Remove-Item -LiteralPath $tmpRoot -Force"
                 })
        {
            Assert.Contains(expected, script, StringComparison.OrdinalIgnoreCase);
        }

        Assert.Contains("fresh-local-smoke.ps1", guide, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("пустой `tmp`", guide, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("[x] `P11-ACC-129`", roadmap, StringComparison.Ordinal);
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, ".env.example")) && Directory.Exists(Path.Combine(directory.FullName, "scripts")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Repository root was not found for fresh local smoke tests.");
    }
}
