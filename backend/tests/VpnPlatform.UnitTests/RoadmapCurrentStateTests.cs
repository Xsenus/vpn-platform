using System.Text.Json;
using System.Text.RegularExpressions;
using System.Globalization;
using Xunit;

namespace VpnPlatform.UnitTests;

public class RoadmapCurrentStateTests
{
    private const string CurrentReleaseId = "2026-07-02-product-roadmap-external-evidence-open-guard";
    private const string CurrentVersion = "0.422.0";

    [Fact]
    public void Roadmap_Current_State_Should_Match_Latest_Local_Evidence()
    {
        var root = FindRepositoryRoot();
        var roadmap = File.ReadAllText(Path.Combine(root, "docs", "PRODUCT_COMPLETION_ROADMAP.md"));

        Assert.Contains("Дата актуализации: 2026-06-14", roadmap, StringComparison.Ordinal);
        Assert.Contains("[x] `STATE-001`", roadmap, StringComparison.Ordinal);
        Assert.Contains("715/715", roadmap, StringComparison.Ordinal);
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
    public void Roadmap_External_Evidence_Items_Should_Remain_Unclosed_Until_Real_Evidence()
    {
        var root = FindRepositoryRoot();
        var roadmap = File.ReadAllText(Path.Combine(root, "docs", "PRODUCT_COMPLETION_ROADMAP.md"));
        var markers = Regex.Matches(
                roadmap,
                @"(?m)^- \[(?<status>x| |~|!)\] `(?<id>[^`]+)`")
            .Cast<Match>()
            .ToDictionary(
                x => x.Groups["id"].Value,
                x => x.Groups["status"].Value,
                StringComparer.Ordinal);

        var expectedOpenItems = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["STATE-011"] = " ",
            ["STATE-012"] = " ",
            ["STATE-013"] = " ",
            ["P0-ADMIN-001"] = " ",
            ["P0-ADMIN-002"] = " ",
            ["P0-VPN-001"] = " ",
            ["P0-VPN-002"] = " ",
            ["P0-VPN-003"] = " ",
            ["P0-VPN-004"] = " ",
            ["P0-VPN-005"] = " ",
            ["P0-PAY-002"] = " ",
            ["P0-PAY-003"] = " ",
            ["P0-PAY-004"] = " ",
            ["P0-PAY-005"] = " ",
            ["P0-PAY-006"] = " ",
            ["P0-PAY-007"] = " ",
            ["P0-PAY-008"] = " ",
            ["P0-PAY-009"] = " ",
            ["P9-TST-007"] = "~",
            ["P11-ACC-002"] = " "
        };

        foreach (var (id, expectedStatus) in expectedOpenItems)
        {
            Assert.True(markers.TryGetValue(id, out var actualStatus), $"Roadmap marker {id} must exist.");
            Assert.Equal(expectedStatus, actualStatus);
            Assert.DoesNotContain($"[x] `{id}`", roadmap, StringComparison.Ordinal);
        }

        var headerLine = roadmap
            .Split('\n')
            .FirstOrDefault(x => x.Contains("staging-ready baseline", StringComparison.OrdinalIgnoreCase)
                && x.Contains("P11-ACC-002", StringComparison.OrdinalIgnoreCase));

        Assert.NotNull(headerLine);
        foreach (var expectedHeaderToken in new[]
                 {
                     "STATE-011",
                     "STATE-012",
                     "STATE-013",
                     "P0-ADMIN-001",
                     "P0-ADMIN-002",
                     "P0-VPN-*",
                     "P0-PAY-*",
                     "P9-TST-007",
                     "P11-ACC-002"
                 })
        {
            Assert.Contains(expectedHeaderToken, headerLine, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void Roadmap_Header_Progress_Should_Match_Checklist_Markers_Percent_And_Remaining()
    {
        var root = FindRepositoryRoot();
        var roadmap = File.ReadAllText(Path.Combine(root, "docs", "PRODUCT_COMPLETION_ROADMAP.md"));

        var markerMatches = Regex.Matches(
            roadmap,
            @"(?m)^- \[(?<status>x| |~|!)\] `(?<id>[^`]+)`");

        var markers = markerMatches.Cast<Match>().ToArray();
        var done = markers.Count(x => x.Groups["status"].Value == "x");
        var open = markers.Count(x => x.Groups["status"].Value == " ");
        var inProgress = markers.Count(x => x.Groups["status"].Value == "~");
        var blocked = markers.Count(x => x.Groups["status"].Value == "!");
        var total = markers.Length;

        var headerLine = roadmap
            .Split('\n')
            .FirstOrDefault(x => x.Contains("staging-ready baseline", StringComparison.OrdinalIgnoreCase)
                && x.Contains("P11-ACC-002", StringComparison.OrdinalIgnoreCase));
        var headerMatch = Regex.Match(
            headerLine ?? string.Empty,
            @"`(?<done>\d+)/(?<total>\d+)`.*?`(?<percent>\d+\.\d)%`.*?`(?<remaining>\d+)`.*?`(?<open>\d+)`.*?`(?<inProgress>\d+)`.*?\[!\]");

        Assert.True(headerMatch.Success, "Roadmap header must include completed, total, percent, remaining, open, in-progress and blocked counts.");
        Assert.Equal(done, int.Parse(headerMatch.Groups["done"].Value));
        Assert.Equal(total, int.Parse(headerMatch.Groups["total"].Value));
        var actualPercent = decimal.Parse(headerMatch.Groups["percent"].Value, CultureInfo.InvariantCulture);
        var expectedPercent = Math.Round(done * 100m / total, 1, MidpointRounding.AwayFromZero);
        Assert.Equal(expectedPercent, actualPercent);
        var remaining = int.Parse(headerMatch.Groups["remaining"].Value);
        Assert.Equal(total - done, remaining);
        Assert.Equal(open + inProgress + blocked, remaining);
        Assert.Equal(open, int.Parse(headerMatch.Groups["open"].Value));
        Assert.Equal(inProgress, int.Parse(headerMatch.Groups["inProgress"].Value));
        Assert.Equal(0, blocked);
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
        Assert.Contains("715/715", readme, StringComparison.Ordinal);
        Assert.Contains("715/715", finalRunbook, StringComparison.Ordinal);
        Assert.Contains("715/715", releaseDecision, StringComparison.Ordinal);

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
