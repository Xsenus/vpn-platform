using System.Text.Json;
using Xunit;

namespace VpnPlatform.UnitTests;

public class MobileSmokeDocumentationTests
{
    [Fact]
    public void Mobile_Smoke_Should_Cover_All_Frontend_Apps_With_Screenshots_And_Console_Guards()
    {
        var root = FindRepositoryRoot();
        var packageJson = JsonDocument.Parse(File.ReadAllText(Path.Combine(root, "frontend", "package.json")));
        var config = File.ReadAllText(Path.Combine(root, "frontend", "playwright.config.ts"));
        var publicSpec = File.ReadAllText(Path.Combine(root, "frontend", "e2e", "public.spec.ts"));
        var cabinetSpec = File.ReadAllText(Path.Combine(root, "frontend", "e2e", "cabinet.spec.ts"));
        var adminSpec = File.ReadAllText(Path.Combine(root, "frontend", "e2e", "admin.spec.ts"));
        var guide = File.ReadAllText(Path.Combine(root, "docs", "mobile-smoke.md"));
        var docsIndex = File.ReadAllText(Path.Combine(root, "docs", "README.md"));

        var scripts = packageJson.RootElement.GetProperty("scripts");
        var mobileScript = scripts.GetProperty("e2e:mobile").GetString() ?? string.Empty;

        foreach (var required in new[] { "mobile-public", "mobile-cabinet", "mobile-admin" })
        {
            Assert.Contains(required, mobileScript, StringComparison.OrdinalIgnoreCase);
            Assert.Contains($"name: '{required}'", config, StringComparison.OrdinalIgnoreCase);
        }

        Assert.Contains("Pixel 5", config, StringComparison.OrdinalIgnoreCase);

        foreach (var (spec, screenshotName) in new[]
        {
            (publicSpec, "public-mobile.png"),
            (cabinetSpec, "cabinet-mobile.png"),
            (adminSpec, "admin-mobile.png")
        })
        {
            Assert.Contains("consoleErrors", spec, StringComparison.Ordinal);
            Assert.Contains("page.on('console'", spec, StringComparison.Ordinal);
            Assert.Contains("page.on('pageerror'", spec, StringComparison.Ordinal);
            Assert.Contains("testInfo.project.name.startsWith('mobile-')", spec, StringComparison.Ordinal);
            Assert.Contains(screenshotName, spec, StringComparison.Ordinal);
            Assert.Contains("fullPage: true", spec, StringComparison.Ordinal);
        }

        foreach (var required in new[]
        {
            "P11-ACC-003",
            "npm run e2e:mobile --prefix frontend",
            "public-mobile.png",
            "cabinet-mobile.png",
            "admin-mobile.png",
            "console.error",
            "pageerror"
        })
        {
            Assert.Contains(required, guide, StringComparison.OrdinalIgnoreCase);
        }

        Assert.Contains("mobile-smoke.md", docsIndex, StringComparison.OrdinalIgnoreCase);
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, ".env.example")) && Directory.Exists(Path.Combine(directory.FullName, "frontend")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Repository root was not found for mobile smoke documentation tests.");
    }
}
