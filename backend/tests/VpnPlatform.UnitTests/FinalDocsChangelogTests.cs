using System.Text.Json;
using Xunit;

namespace VpnPlatform.UnitTests;

public class FinalDocsChangelogTests
{
    [Fact]
    public void Final_Runbook_Changelog_And_Readme_Should_Be_Linked()
    {
        var root = FindRepositoryRoot();
        var readme = File.ReadAllText(Path.Combine(root, "README.md"));
        var docsIndex = File.ReadAllText(Path.Combine(root, "docs", "README.md"));
        var roadmap = File.ReadAllText(Path.Combine(root, "docs", "PRODUCT_COMPLETION_ROADMAP.md"));
        var testResults = File.ReadAllText(Path.Combine(root, "TEST_RESULTS.md"));

        Assert.Contains("CHANGELOG.md", readme, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("docs/final-runbook.md", readme, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("npm run e2e:mobile --prefix frontend", readme, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("npm run e2e:console --prefix frontend", readme, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("578/578", readme, StringComparison.Ordinal);
        Assert.Contains("2026-06-19-admin-vps-smoke-preflight-validator", readme, StringComparison.OrdinalIgnoreCase);

        Assert.Contains("../CHANGELOG.md", docsIndex, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("final-runbook.md", docsIndex, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("[x] `P11-ACC-006`", roadmap, StringComparison.Ordinal);
        Assert.Contains("2026-06-14-final-docs-changelog", testResults, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Final_Runbook_Should_Cover_Local_Validation_Deploy_And_Production_Limits()
    {
        var root = FindRepositoryRoot();
        var runbook = File.ReadAllText(Path.Combine(root, "docs", "final-runbook.md"));

        foreach (var expected in new[]
                 {
                     "scripts\\start-local.ps1",
                     "scripts\\stop-local.ps1",
                     "scripts\\fresh-local-smoke.ps1",
                     "dotnet test backend\\VpnPlatform.sln --configuration Release",
                     "npm run e2e:console --prefix frontend",
                     "scripts\\scan-secrets.ps1",
                     ".github/workflows/deploy-vps.yml",
                     "VPS_DEPLOY_MODE",
                     "scripts/post-deploy-smoke.sh",
                     "P11-ACC-008 Production readiness gate"
                 })
        {
            Assert.Contains(expected, runbook, StringComparison.OrdinalIgnoreCase);
        }

        Assert.Contains("production-ready", runbook, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("ротация", runbook, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("3x-ui", runbook, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Changelog_Should_Record_Current_Final_Docs_Release_And_Latest_Seed()
    {
        var root = FindRepositoryRoot();
        var changelog = File.ReadAllText(Path.Combine(root, "CHANGELOG.md"));
        using var releasesJson = JsonDocument.Parse(File.ReadAllText(Path.Combine(root, "backend", "src", "VpnPlatform.Api", "AppReleases", "releases.json")));
        var finalDocsRelease = releasesJson.RootElement
            .EnumerateArray()
            .Single(x => string.Equals(x.GetProperty("releaseId").GetString(), "2026-06-14-final-docs-changelog", StringComparison.Ordinal));

        Assert.Contains("0.103.0 - 2026-06-14", changelog, StringComparison.Ordinal);
        Assert.Contains("docs/final-runbook.md", changelog, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("FinalDocsChangelogTests", changelog, StringComparison.Ordinal);
        Assert.Contains("467/467", changelog, StringComparison.Ordinal);
        Assert.Contains("staging-ready baseline", changelog, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("production-ready", changelog, StringComparison.OrdinalIgnoreCase);

        Assert.Equal("0.103.0", finalDocsRelease.GetProperty("version").GetString());
        Assert.Contains("final-runbook.md", finalDocsRelease.GetProperty("summary").GetString(), StringComparison.OrdinalIgnoreCase);
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

        throw new InvalidOperationException("Repository root was not found for final docs changelog tests.");
    }
}
