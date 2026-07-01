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
                     "Backend full suite: `681/681`",
                     "Frontend unit tests: `66/66`",
                     "Playwright public/cabinet/admin/all-screens/mobile/console smoke: `9/9`",
                     "Fresh local SQLite smoke: OK",
                     "Local SQLite VPS smoke dry-run: OK",
                     "Encoding guard: OK",
                     "Secret scan: OK",
                     "2026-07-02-admin-bootstrap-wrapper-cleanup",
                     "0.386.0"
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
        Assert.Contains("2026-07-02-admin-bootstrap-wrapper-cleanup", testResults, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("2026-07-02-admin-bootstrap-wrapper-cleanup", releases, StringComparison.OrdinalIgnoreCase);
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

        throw new InvalidOperationException("Repository root was not found for product admin UI roadmap sync tests.");
    }
}
