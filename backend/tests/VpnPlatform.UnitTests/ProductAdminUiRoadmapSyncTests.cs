using Xunit;

namespace VpnPlatform.UnitTests;

public class ProductAdminUiRoadmapSyncTests
{
    [Fact]
    public void Product_Admin_Ui_Roadmap_Should_Reflect_Current_Product_Status()
    {
        var root = FindRepositoryRoot();
        var productRoadmap = File.ReadAllText(Path.Combine(root, "docs", "product-admin-ui-roadmap.md"));
        var masterRoadmap = File.ReadAllText(Path.Combine(root, "docs", "PRODUCT_COMPLETION_ROADMAP.md"));
        var testResults = File.ReadAllText(Path.Combine(root, "TEST_RESULTS.md"));
        var releases = File.ReadAllText(Path.Combine(root, "backend", "src", "VpnPlatform.Api", "AppReleases", "releases.json"));

        foreach (var expected in new[]
                 {
                     "Дата актуализации: 2026-06-14",
                     "PRODUCT_COMPLETION_ROADMAP.md",
                     "staging-ready baseline",
                     "Backend full suite: `737/737`",
                     "Frontend unit tests: `66/66`",
                     "Playwright public/cabinet/admin/all-screens/mobile/console smoke: `9/9`",
                     "Fresh local SQLite smoke: OK",
                     "Local SQLite VPS smoke dry-run: OK",
                     "Encoding guard: OK",
                     "Secret scan: OK",
                     "2026-08-04-full-project-quality-audit",
                     "0.455.0"
                 })
        {
            Assert.Contains(expected, productRoadmap, StringComparison.OrdinalIgnoreCase);
        }

        foreach (var staleUnchecked in new[]
                 {
                     "- [ ] Проверить главную страницу.",
                     "- [ ] Проверить экран логина.",
                     "- [ ] `npm run typecheck`.",
                     "- [ ] Полный backend test suite."
                 })
        {
            Assert.DoesNotContain(staleUnchecked, productRoadmap, StringComparison.OrdinalIgnoreCase);
        }

        foreach (var liveBlocker in new[]
                 {
                     "Live-платежи всех провайдеров не подтверждены",
                     "Реальная production-like выдача через 3x-ui",
                     "Админка на VPS не проверена",
                     "Production-ready решение не принято",
                     "Заполнить staging/VPS smoke report"
                 })
        {
            Assert.Contains(liveBlocker, productRoadmap, StringComparison.OrdinalIgnoreCase);
        }

        Assert.Contains("[ ] `STATE-011`", masterRoadmap, StringComparison.Ordinal);
        Assert.Contains("[ ] `STATE-012`", masterRoadmap, StringComparison.Ordinal);
        Assert.Contains("[ ] `STATE-013`", masterRoadmap, StringComparison.Ordinal);
        Assert.Contains("2026-08-04-full-project-quality-audit", testResults, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("2026-08-04-full-project-quality-audit", releases, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Product_Admin_Ui_Roadmap_Should_Keep_Current_External_Evidence_Status_Explicit()
    {
        var root = FindRepositoryRoot();
        var productRoadmap = File.ReadAllText(Path.Combine(root, "docs", "product-admin-ui-roadmap.md"));
        var masterRoadmap = File.ReadAllText(Path.Combine(root, "docs", "PRODUCT_COMPLETION_ROADMAP.md"));

        foreach (var expected in new[]
                 {
                     "Backend full suite: `737/737`",
                     "Frontend unit tests: `66/66`",
                     "Fresh local SQLite smoke: OK",
                     "Secret scan: OK",
                     "2026-08-04-full-project-quality-audit",
                     "0.455.0",
                     "staging-ready baseline",
                     "Production-ready",
                     "Live-",
                     "3x-ui",
                     "VPS",
                     "staging/VPS smoke report"
                 })
        {
            Assert.Contains(expected, productRoadmap, StringComparison.OrdinalIgnoreCase);
        }

        foreach (var openMarker in new[]
                 {
                     "[ ] `STATE-011`",
                     "[ ] `STATE-012`",
                     "[ ] `STATE-013`",
                     "[ ] `P11-ACC-002`",
                     "[~] `P9-TST-007`"
                 })
        {
            Assert.Contains(openMarker, masterRoadmap, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void Product_Admin_Ui_Roadmap_External_Evidence_Items_Should_Remain_Unclosed()
    {
        var root = FindRepositoryRoot();
        var productRoadmap = File.ReadAllText(Path.Combine(root, "docs", "product-admin-ui-roadmap.md"));

        foreach (var expected in new[]
                 {
                     new ExpectedStatus("~", "Staging/VPS smoke checklist"),
                     new ExpectedStatus(" ", "Live-"),
                     new ExpectedStatus(" ", "production-like", "3x-ui"),
                     new ExpectedStatus(" ", "VPS", "production admin"),
                     new ExpectedStatus(" ", "Production-ready"),
                     new ExpectedStatus(" ", "production admin", "VPS"),
                     new ExpectedStatus(" ", "3x-ui", "VPN node"),
                     new ExpectedStatus(" ", "production-like order smoke"),
                     new ExpectedStatus(" ", "YooKassa", "PayPal"),
                     new ExpectedStatus(" ", "staging/VPS smoke report")
                 })
        {
            AssertRoadmapLineHasStatus(productRoadmap, expected.Status, expected.Tokens);
        }
    }

    private static void AssertRoadmapLineHasStatus(string roadmap, string expectedStatus, params string[] tokens)
    {
        var expectedPrefix = $"- [{expectedStatus}]";
        Assert.Contains(
            roadmap.Split('\n'),
            line => line.StartsWith(expectedPrefix, StringComparison.Ordinal)
                && tokens.All(token => line.Contains(token, StringComparison.OrdinalIgnoreCase)));
    }

    private sealed record ExpectedStatus(string Status, params string[] Tokens);

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

        throw new InvalidOperationException("Repository root was not found for product admin UI roadmap sync tests.");
    }
}
