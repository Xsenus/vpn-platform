using Xunit;

namespace VpnPlatform.UnitTests;

public class AllScreensBrowserSmokeTests
{
    [Fact]
    public void All_Screens_Playwright_Project_Should_Cover_Public_Cabinet_And_Admin_Surfaces()
    {
        var root = FindRepositoryRoot();
        var spec = File.ReadAllText(Path.Combine(root, "frontend", "e2e", "all-screens.spec.ts"));
        var publicApp = File.ReadAllText(Path.Combine(root, "frontend", "apps", "public-web", "src", "App.tsx"));
        var publicRoute = File.ReadAllText(Path.Combine(root, "frontend", "apps", "public-web", "src", "public-route.ts"));
        var adminApp = File.ReadAllText(Path.Combine(root, "frontend", "apps", "admin-panel", "src", "App.tsx"));
        var adminMetadata = File.ReadAllText(Path.Combine(root, "frontend", "apps", "admin-panel", "src", "admin-page-metadata.ts"));
        var adminSpec = File.ReadAllText(Path.Combine(root, "frontend", "e2e", "admin.spec.ts"));
        var sharedUi = File.ReadAllText(Path.Combine(root, "frontend", "packages", "ui", "src", "index.tsx"));
        var config = File.ReadAllText(Path.Combine(root, "frontend", "playwright.config.ts"));
        var packageJson = File.ReadAllText(Path.Combine(root, "frontend", "package.json"));

        foreach (var expected in new[]
                 {
                     "all public routes render",
                     "cabinet auth and dashboard",
                     "every admin section",
                     "console.error",
                     "pageerror",
                     "expectNonBlankPage",
                     "expectPageQuality",
                     "accessible name",
                     "duplicate id",
                     "main landmark",
                     "publicRoutes",
                     "adminSections",
                     "'/tariffs'",
                     "'/faq'",
                     "'/help'",
                     "'/account'",
                     "'/missing-page'",
                     "dashboard",
                     "payments",
                     "panels",
                     "provisioning"
                 })
        {
            Assert.Contains(expected, spec, StringComparison.OrdinalIgnoreCase);
        }

        Assert.Contains("name: 'all-screens'", config, StringComparison.Ordinal);
        Assert.Contains("all-screens\\.spec\\.ts", config, StringComparison.Ordinal);
        Assert.Contains("\"e2e:all-screens\"", packageJson, StringComparison.Ordinal);
        Assert.Contains("--project=all-screens", packageJson, StringComparison.Ordinal);
        Assert.Contains("path=\"*\"", publicApp, StringComparison.Ordinal);
        Assert.Contains("Страница не найдена", publicApp, StringComparison.Ordinal);
        Assert.Contains("Ошибка 404", publicApp, StringComparison.Ordinal);
        Assert.Contains("PublicRouteEffects", publicApp, StringComparison.Ordinal);
        Assert.Contains("main-content", publicApp, StringComparison.Ordinal);
        Assert.Contains("window.scrollTo", publicApp, StringComparison.Ordinal);
        foreach (var title in new[]
                 {
                     "Тарифы — VPN Platform",
                     "Помощь — VPN Platform",
                     "FAQ — VPN Platform",
                     "Аккаунт — VPN Platform",
                     "Страница не найдена — VPN Platform"
                 })
        {
            Assert.Contains(title, publicRoute, StringComparison.Ordinal);
        }
        foreach (var title in new[]
                 {
                     "Проверка сессии — Админ-панель VPN Platform",
                     "Вход — Админ-панель VPN Platform",
                     "${input.sectionLabel} — Админ-панель VPN Platform"
                 })
        {
            Assert.Contains(title, adminMetadata, StringComparison.Ordinal);
        }
        Assert.Contains("adminSectionTitles", spec, StringComparison.Ordinal);
        Assert.Contains("window.history.pushState", adminApp, StringComparison.Ordinal);
        Assert.Contains("window.addEventListener('popstate'", adminApp, StringComparison.Ordinal);
        Assert.Contains("window.location.hash && parseAdminSectionHref(window.location.hash) === null", adminApp, StringComparison.Ordinal);
        Assert.Contains("goToAdminSection('users')", adminApp, StringComparison.Ordinal);
        Assert.Contains("goToAdminSection('subscriptions')", adminApp, StringComparison.Ordinal);
        Assert.Contains("admin section history restores content, metadata and focus", adminSpec, StringComparison.Ordinal);
        Assert.Contains("admin invalid hashes recover to a canonical dashboard route", adminSpec, StringComparison.Ordinal);
        Assert.Contains("admin order links keep section history and focus operable", adminSpec, StringComparison.Ordinal);
        Assert.Contains("if (updateHash) window.history.replaceState", sharedUi, StringComparison.Ordinal);
        Assert.Contains("SkipLink href=\"#admin-session-recovery\" updateHash={false}", adminApp, StringComparison.Ordinal);
        Assert.Contains("SkipLink href=\"#admin-login\" updateHash={false}", adminApp, StringComparison.Ordinal);
        Assert.Contains("SkipLink href=\"#admin-content\" updateHash={false}", adminApp, StringComparison.Ordinal);
        Assert.Contains("admin skip links preserve the current section route", adminSpec, StringComparison.Ordinal);
    }

    [Fact]
    public void All_Screens_Documentation_Should_Be_Linked_To_Roadmap_Changelog_And_TestResults()
    {
        var root = FindRepositoryRoot();
        var guide = File.ReadAllText(Path.Combine(root, "docs", "all-screens-browser-smoke.md"));
        var docsIndex = File.ReadAllText(Path.Combine(root, "docs", "README.md"));
        var roadmap = File.ReadAllText(Path.Combine(root, "docs", "PRODUCT_COMPLETION_ROADMAP.md"));
        var changelog = File.ReadAllText(Path.Combine(root, "CHANGELOG.md"));
        var testResults = File.ReadAllText(Path.Combine(root, "TEST_RESULTS.md"));
        var releases = File.ReadAllText(Path.Combine(root, "backend", "src", "VpnPlatform.Api", "AppReleases", "releases.json"));

        foreach (var expected in new[]
                 {
                     "STATE-010",
                     "npm run e2e:all-screens --prefix frontend",
                     "public web routes",
                     "cabinet auth screen",
                     "all admin sections",
                     "console.error",
                     "pageerror",
                     "2026-06-14-all-screens-browser-smoke"
                 })
        {
            Assert.Contains(expected, guide, StringComparison.OrdinalIgnoreCase);
        }

        Assert.Contains("all-screens-browser-smoke.md", docsIndex, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("[x] `STATE-010`", roadmap, StringComparison.Ordinal);
        Assert.Contains("2026-06-14-all-screens-browser-smoke", changelog, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("2026-06-14-all-screens-browser-smoke", testResults, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("2026-06-14-all-screens-browser-smoke", releases, StringComparison.OrdinalIgnoreCase);
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "README.md"))
                && Directory.Exists(Path.Combine(directory.FullName, "frontend"))
                && Directory.Exists(Path.Combine(directory.FullName, "backend")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Repository root was not found for all screens browser smoke tests.");
    }
}
