using System.Text.Json;
using Xunit;

namespace VpnPlatform.UnitTests;

public class NoConsoleErrorsSmokeTests
{
    [Fact]
    public void No_Console_Errors_Smoke_Should_Run_All_Main_Desktop_And_Mobile_Surfaces()
    {
        var root = FindRepositoryRoot();
        var packageJson = JsonDocument.Parse(File.ReadAllText(Path.Combine(root, "frontend", "package.json")));
        var config = File.ReadAllText(Path.Combine(root, "frontend", "playwright.config.ts"));
        var publicSpec = File.ReadAllText(Path.Combine(root, "frontend", "e2e", "public.spec.ts"));
        var cabinetSpec = File.ReadAllText(Path.Combine(root, "frontend", "e2e", "cabinet.spec.ts"));
        var adminSpec = File.ReadAllText(Path.Combine(root, "frontend", "e2e", "admin.spec.ts"));
        var guide = File.ReadAllText(Path.Combine(root, "docs", "no-console-errors-smoke.md"));
        var docsIndex = File.ReadAllText(Path.Combine(root, "docs", "README.md"));

        var consoleScript = packageJson.RootElement.GetProperty("scripts").GetProperty("e2e:console").GetString() ?? string.Empty;
        foreach (var project in new[] { "public-web", "cabinet", "admin-panel", "mobile-public", "mobile-cabinet", "mobile-admin" })
        {
            Assert.Contains($"--project={project}", consoleScript, StringComparison.OrdinalIgnoreCase);
            Assert.Contains(project, config, StringComparison.OrdinalIgnoreCase);
        }

        foreach (var spec in new[] { publicSpec, cabinetSpec, adminSpec })
        {
            Assert.Contains("consoleErrors", spec, StringComparison.Ordinal);
            Assert.Contains("page.on('console'", spec, StringComparison.Ordinal);
            Assert.Contains("message.type() === 'error'", spec, StringComparison.Ordinal);
            Assert.Contains("page.on('pageerror'", spec, StringComparison.Ordinal);
            Assert.Contains("expect(consoleErrors).toEqual([])", spec, StringComparison.Ordinal);
        }

        foreach (var required in new[]
        {
            "P11-ACC-004",
            "npm run e2e:console --prefix frontend",
            "public desktop",
            "cabinet desktop",
            "admin desktop",
            "public mobile",
            "cabinet mobile",
            "admin mobile",
            "console.error=0",
            "pageerror=0"
        })
        {
            Assert.Contains(required, guide, StringComparison.OrdinalIgnoreCase);
        }

        Assert.Contains("no-console-errors-smoke.md", docsIndex, StringComparison.OrdinalIgnoreCase);
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

        throw new InvalidOperationException("Repository root was not found for no console errors smoke tests.");
    }
}
