using System.Text.Json;
using Xunit;

namespace VpnPlatform.UnitTests;

public class RoadmapCurrentStateTests
{
    private const string CurrentReleaseId = "2026-06-22-admin-vps-bootstrap-evidence-identity-summary";
    private const string CurrentVersion = "0.225.0";

    [Fact]
    public void Roadmap_Current_State_Should_Match_Latest_Local_Evidence()
    {
        var root = FindRepositoryRoot();
        var roadmap = File.ReadAllText(Path.Combine(root, "docs", "PRODUCT_COMPLETION_ROADMAP.md"));

        Assert.Contains("Дата актуализации: 2026-06-14", roadmap, StringComparison.Ordinal);
        Assert.Contains("[x] `STATE-001`", roadmap, StringComparison.Ordinal);
        Assert.Contains("590/590", roadmap, StringComparison.Ordinal);
        Assert.Contains("[x] `STATE-002`", roadmap, StringComparison.Ordinal);
        Assert.Contains("66/66", roadmap, StringComparison.Ordinal);
        Assert.Contains("9/9", roadmap, StringComparison.Ordinal);
        Assert.Contains(CurrentReleaseId, roadmap, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(CurrentVersion, roadmap, StringComparison.Ordinal);
        Assert.Contains("[x] `STATE-014`", roadmap, StringComparison.Ordinal);

        foreach (var stillOpen in new[]
                 {
                     "[ ] `STATE-011`",
                     "[ ] `STATE-012`",
                     "[ ] `STATE-013`",
                     "[ ] `P11-ACC-002`",
                     "[~] `P9-TST-007`"
                 })
        {
            Assert.Contains(stillOpen, roadmap, StringComparison.Ordinal);
        }

        Assert.Contains("production-ready", roadmap, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("staging-ready baseline", roadmap, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Current_State_Should_Be_Linked_From_Docs_Changelog_Test_Results_And_Release_Seed()
    {
        var root = FindRepositoryRoot();
        var readme = File.ReadAllText(Path.Combine(root, "README.md"));
        var changelog = File.ReadAllText(Path.Combine(root, "CHANGELOG.md"));
        var testResults = File.ReadAllText(Path.Combine(root, "TEST_RESULTS.md"));
        var finalRunbook = File.ReadAllText(Path.Combine(root, "docs", "final-runbook.md"));
        var releaseDecision = File.ReadAllText(Path.Combine(root, "docs", "release-decision.md"));

        foreach (var document in new[] { readme, changelog, testResults, finalRunbook, releaseDecision })
        {
            Assert.Contains(CurrentReleaseId, document, StringComparison.OrdinalIgnoreCase);
            Assert.Contains(CurrentVersion, document, StringComparison.Ordinal);
        }

        Assert.Contains("RoadmapCurrentStateTests", changelog, StringComparison.Ordinal);
        Assert.Contains("RoadmapCurrentStateTests", testResults, StringComparison.Ordinal);
        Assert.Contains("590/590", readme, StringComparison.Ordinal);
        Assert.Contains("590/590", finalRunbook, StringComparison.Ordinal);
        Assert.Contains("590/590", releaseDecision, StringComparison.Ordinal);

        using var releasesJson = JsonDocument.Parse(File.ReadAllText(Path.Combine(
            root,
            "backend",
            "src",
            "VpnPlatform.Api",
            "AppReleases",
            "releases.json")));

        var releases = releasesJson.RootElement.EnumerateArray().ToArray();
        var current = releases.Single(x => string.Equals(
            x.GetProperty("releaseId").GetString(),
            CurrentReleaseId,
            StringComparison.Ordinal));
        var latest = releases
            .Where(x => x.GetProperty("isActive").GetBoolean())
            .OrderByDescending(x => x.GetProperty("releasedAt").GetDateTimeOffset())
            .First();

        Assert.Equal(CurrentVersion, current.GetProperty("version").GetString());
        Assert.Equal(CurrentReleaseId, latest.GetProperty("releaseId").GetString());
        Assert.Contains("STATE-014", testResults, StringComparison.Ordinal);
        Assert.Contains("STATE-014", File.ReadAllText(Path.Combine(root, "docs", "PRODUCT_COMPLETION_ROADMAP.md")), StringComparison.Ordinal);
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

        throw new InvalidOperationException("Repository root was not found for roadmap current state tests.");
    }
}
