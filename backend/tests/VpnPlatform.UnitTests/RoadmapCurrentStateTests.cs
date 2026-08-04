using System.Text.Json;
using System.Text.RegularExpressions;
using System.Globalization;
using Xunit;

namespace VpnPlatform.UnitTests;

public class RoadmapCurrentStateTests
{
    private const string CurrentReleaseId = "2026-08-04-admin-operation-audit-integrity";
    private const string CurrentVersion = "0.459.0";

    [Fact]
    public void Roadmap_Current_State_Should_Match_Latest_Local_Evidence()
    {
        var root = FindRepositoryRoot();
        var roadmap = File.ReadAllText(Path.Combine(root, "docs", "PRODUCT_COMPLETION_ROADMAP.md"));

        Assert.Contains("Дата актуализации: 2026-08-04", roadmap, StringComparison.Ordinal);
        Assert.Contains("[x] `STATE-001`", roadmap, StringComparison.Ordinal);
        Assert.Contains("798/798", roadmap, StringComparison.Ordinal);
        Assert.Contains("[x] `STATE-002`", roadmap, StringComparison.Ordinal);
        Assert.Contains("66/66", roadmap, StringComparison.Ordinal);
        Assert.Contains("11/11", roadmap, StringComparison.Ordinal);
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

        var actualNotClosedItems = markers
            .Where(x => x.Value != "x")
            .OrderBy(x => x.Key, StringComparer.Ordinal)
            .ToArray();
        var expectedNotClosedItems = expectedOpenItems
            .OrderBy(x => x.Key, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(expectedNotClosedItems.Length, actualNotClosedItems.Length);
        for (var i = 0; i < expectedNotClosedItems.Length; i++)
        {
            Assert.Equal(expectedNotClosedItems[i].Key, actualNotClosedItems[i].Key);
            Assert.Equal(expectedNotClosedItems[i].Value, actualNotClosedItems[i].Value);
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
    public void Current_Status_Docs_Should_Report_Same_Roadmap_Progress_Counters()
    {
        var root = FindRepositoryRoot();
        var roadmap = File.ReadAllText(Path.Combine(root, "docs", "PRODUCT_COMPLETION_ROADMAP.md"));
        var counters = CalculateRoadmapCounters(roadmap);
        var documents = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["README.md"] = File.ReadAllText(Path.Combine(root, "README.md")),
            ["CHANGELOG.md"] = File.ReadAllText(Path.Combine(root, "CHANGELOG.md")),
            ["TEST_RESULTS.md"] = File.ReadAllText(Path.Combine(root, "TEST_RESULTS.md")),
            ["docs/final-runbook.md"] = File.ReadAllText(Path.Combine(root, "docs", "final-runbook.md")),
            ["docs/release-decision.md"] = File.ReadAllText(Path.Combine(root, "docs", "release-decision.md")),
            ["docs/product-admin-ui-roadmap.md"] = File.ReadAllText(Path.Combine(root, "docs", "product-admin-ui-roadmap.md"))
        };

        var expectedTokens = new[]
        {
            $"`{counters.Done}/{counters.Total}`",
            $"`{counters.Percent}%`",
            $"`{counters.Remaining}`",
            $"`{counters.Open}`",
            $"`{counters.InProgress}`",
            $"`{counters.Blocked}`"
        };

        foreach (var (path, document) in documents)
        {
            Assert.Contains(CurrentReleaseId, document, StringComparison.OrdinalIgnoreCase);
            Assert.Contains(CurrentVersion, document, StringComparison.Ordinal);
            foreach (var token in expectedTokens)
            {
                Assert.Contains(token, document, StringComparison.Ordinal);
            }
        }
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
        var productRoadmap = File.ReadAllText(Path.Combine(root, "docs", "product-admin-ui-roadmap.md"));

        foreach (var document in new[] { readme, changelog, testResults, finalRunbook, releaseDecision, productRoadmap })
        {
            Assert.Contains(CurrentReleaseId, document, StringComparison.OrdinalIgnoreCase);
            Assert.Contains(CurrentVersion, document, StringComparison.Ordinal);
        }

        Assert.Contains("RoadmapCurrentStateTests", changelog, StringComparison.Ordinal);
        Assert.Contains("RoadmapCurrentStateTests", testResults, StringComparison.Ordinal);
        Assert.Contains("798/798", readme, StringComparison.Ordinal);
        Assert.Contains("798/798", finalRunbook, StringComparison.Ordinal);
        Assert.Contains("798/798", releaseDecision, StringComparison.Ordinal);

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

    [Fact]
    public void Latest_Active_Release_Should_Include_Production_Evidence_Caveat()
    {
        var root = FindRepositoryRoot();
        using var releasesJson = JsonDocument.Parse(File.ReadAllText(Path.Combine(
            root,
            "backend",
            "src",
            "VpnPlatform.Api",
            "AppReleases",
            "releases.json")));

        var latest = releasesJson.RootElement
            .EnumerateArray()
            .Where(x => x.GetProperty("isActive").GetBoolean())
            .OrderByDescending(x => x.GetProperty("releasedAt").GetDateTimeOffset())
            .First();

        Assert.Equal(CurrentReleaseId, latest.GetProperty("releaseId").GetString());
        Assert.Equal(CurrentVersion, latest.GetProperty("version").GetString());

        var importantItems = latest
            .GetProperty("items")
            .EnumerateArray()
            .Where(x => string.Equals(x.GetProperty("type").GetString(), "important", StringComparison.OrdinalIgnoreCase))
            .Select(x => x.GetProperty("text").GetString() ?? string.Empty)
            .ToArray();

        Assert.NotEmpty(importantItems);
        Assert.Contains(importantItems, text =>
            text.Contains("Real VPS/staging/payment/3x-ui evidence", StringComparison.OrdinalIgnoreCase)
            && text.Contains("still required", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Latest_Active_Release_Should_Report_Same_Roadmap_Progress_Counters()
    {
        var root = FindRepositoryRoot();
        var roadmap = File.ReadAllText(Path.Combine(root, "docs", "PRODUCT_COMPLETION_ROADMAP.md"));
        var counters = CalculateRoadmapCounters(roadmap);
        using var releasesJson = JsonDocument.Parse(File.ReadAllText(Path.Combine(
            root,
            "backend",
            "src",
            "VpnPlatform.Api",
            "AppReleases",
            "releases.json")));

        var latest = releasesJson.RootElement
            .EnumerateArray()
            .Where(x => x.GetProperty("isActive").GetBoolean())
            .OrderByDescending(x => x.GetProperty("releasedAt").GetDateTimeOffset())
            .First();

        Assert.Equal(CurrentReleaseId, latest.GetProperty("releaseId").GetString());
        Assert.Equal(CurrentVersion, latest.GetProperty("version").GetString());

        var latestText = string.Join(
            "\n",
            latest.GetProperty("summary").GetString() ?? string.Empty,
            string.Join(
                "\n",
                latest.GetProperty("items")
                    .EnumerateArray()
                    .Select(x => x.GetProperty("text").GetString() ?? string.Empty)));

        foreach (var token in new[]
                 {
                     $"{counters.Done}/{counters.Total}",
                     $"{counters.Percent}%",
                     $"{counters.Remaining} remaining",
                     $"{counters.Open} open",
                     $"{counters.InProgress} in progress",
                     $"{counters.Blocked} blocked"
                 })
        {
            Assert.Contains(token, latestText, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void Current_Status_Docs_Should_Not_Claim_Production_Ready_Without_External_Evidence()
    {
        var root = FindRepositoryRoot();
        var documents = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["README.md"] = File.ReadAllText(Path.Combine(root, "README.md")),
            ["CHANGELOG.md"] = File.ReadAllText(Path.Combine(root, "CHANGELOG.md")),
            ["TEST_RESULTS.md"] = File.ReadAllText(Path.Combine(root, "TEST_RESULTS.md")),
            ["docs/final-runbook.md"] = File.ReadAllText(Path.Combine(root, "docs", "final-runbook.md")),
            ["docs/release-decision.md"] = File.ReadAllText(Path.Combine(root, "docs", "release-decision.md")),
            ["docs/product-admin-ui-roadmap.md"] = File.ReadAllText(Path.Combine(root, "docs", "product-admin-ui-roadmap.md")),
            ["docs/PRODUCT_COMPLETION_ROADMAP.md"] = File.ReadAllText(Path.Combine(root, "docs", "PRODUCT_COMPLETION_ROADMAP.md"))
        };

        foreach (var (path, document) in documents)
        {
            Assert.Contains(CurrentReleaseId, document, StringComparison.OrdinalIgnoreCase);
            Assert.Contains(CurrentVersion, document, StringComparison.Ordinal);
            Assert.Contains("staging-ready baseline", document, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("production-ready accepted", document, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("production-ready: true", document, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("[x] `P11-ACC-002`", document, StringComparison.Ordinal);
            Assert.Contains("P11-ACC-002", document, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void Latest_Active_Release_Seed_Should_Be_Unique_And_Strictly_Newest()
    {
        var root = FindRepositoryRoot();
        using var releasesJson = JsonDocument.Parse(File.ReadAllText(Path.Combine(
            root,
            "backend",
            "src",
            "VpnPlatform.Api",
            "AppReleases",
            "releases.json")));

        var activeReleases = releasesJson.RootElement
            .EnumerateArray()
            .Where(x => x.GetProperty("isActive").GetBoolean())
            .Select(x => new
            {
                ReleaseId = x.GetProperty("releaseId").GetString() ?? string.Empty,
                Version = x.GetProperty("version").GetString() ?? string.Empty,
                ReleasedAt = x.GetProperty("releasedAt").GetDateTimeOffset()
            })
            .ToArray();

        Assert.NotEmpty(activeReleases);

        var latestReleasedAt = activeReleases.Max(x => x.ReleasedAt);
        var latestReleases = activeReleases
            .Where(x => x.ReleasedAt == latestReleasedAt)
            .ToArray();

        var latest = Assert.Single(latestReleases);
        Assert.Equal(CurrentReleaseId, latest.ReleaseId);
        Assert.Equal(CurrentVersion, latest.Version);
        Assert.All(
            activeReleases.Where(x => x.ReleaseId != CurrentReleaseId),
            release => Assert.True(
                release.ReleasedAt < latestReleasedAt,
                $"Release {release.ReleaseId} must be older than {CurrentReleaseId}."));
    }

    [Fact]
    public void Release_Seed_Should_Have_Unique_Ids_Versions_And_Timestamps()
    {
        var root = FindRepositoryRoot();
        using var releasesJson = JsonDocument.Parse(File.ReadAllText(Path.Combine(
            root,
            "backend",
            "src",
            "VpnPlatform.Api",
            "AppReleases",
            "releases.json")));

        var releases = releasesJson.RootElement
            .EnumerateArray()
            .Select(x => new
            {
                ReleaseId = x.GetProperty("releaseId").GetString() ?? string.Empty,
                Version = x.GetProperty("version").GetString() ?? string.Empty,
                ReleasedAt = x.GetProperty("releasedAt").GetString() ?? string.Empty
            })
            .ToArray();

        Assert.NotEmpty(releases);
        AssertNoDuplicates(releases.Select(x => x.ReleaseId), "releaseId");
        AssertNoDuplicates(releases.Select(x => x.Version), "version");
        AssertNoDuplicates(releases.Select(x => x.ReleasedAt), "releasedAt");
    }

    [Fact]
    public void Release_Seed_Versions_Should_Increase_With_Release_Timestamps()
    {
        var root = FindRepositoryRoot();
        using var releasesJson = JsonDocument.Parse(File.ReadAllText(Path.Combine(
            root,
            "backend",
            "src",
            "VpnPlatform.Api",
            "AppReleases",
            "releases.json")));

        var releases = releasesJson.RootElement
            .EnumerateArray()
            .Select(x => new
            {
                ReleaseId = x.GetProperty("releaseId").GetString() ?? string.Empty,
                Version = Version.Parse(x.GetProperty("version").GetString() ?? "0.0.0"),
                ReleasedAt = x.GetProperty("releasedAt").GetDateTimeOffset()
            })
            .OrderBy(x => x.ReleasedAt)
            .ToArray();

        for (var i = 1; i < releases.Length; i++)
        {
            var previous = releases[i - 1];
            var current = releases[i];
            Assert.True(
                current.Version > previous.Version,
                $"Release {current.ReleaseId} version {current.Version} must be greater than {previous.ReleaseId} version {previous.Version}.");
        }
    }

    [Fact]
    public void Release_Seed_File_Order_Should_Match_Release_Timestamps()
    {
        var root = FindRepositoryRoot();
        using var releasesJson = JsonDocument.Parse(File.ReadAllText(Path.Combine(
            root,
            "backend",
            "src",
            "VpnPlatform.Api",
            "AppReleases",
            "releases.json")));

        var releases = releasesJson.RootElement
            .EnumerateArray()
            .Select(x => new
            {
                ReleaseId = x.GetProperty("releaseId").GetString() ?? string.Empty,
                ReleasedAt = x.GetProperty("releasedAt").GetDateTimeOffset()
            })
            .ToArray();

        for (var i = 1; i < releases.Length; i++)
        {
            var previous = releases[i - 1];
            var current = releases[i];
            Assert.True(
                current.ReleasedAt > previous.ReleasedAt,
                $"Release {current.ReleaseId} must appear after older release {previous.ReleaseId} in releases.json.");
        }
    }

    [Fact]
    public void Release_Seed_Should_Not_Contain_Secret_Like_Literals()
    {
        var root = FindRepositoryRoot();
        var releasesJson = File.ReadAllText(Path.Combine(
            root,
            "backend",
            "src",
            "VpnPlatform.Api",
            "AppReleases",
            "releases.json"));

        var forbiddenPatterns = new[]
        {
            @"-----BEGIN [A-Z ]*PRIVATE KEY-----",
            @"\bBearer\s+[A-Za-z0-9._~+/=-]{20,}",
            @"\bsk-(?:live|test|proj)-[A-Za-z0-9_-]{16,}",
            @"\bgh[pousr]_[A-Za-z0-9_]{20,}",
            @"\bxox[baprs]-[A-Za-z0-9-]{20,}",
            @"\bAKIA[0-9A-Z]{16}\b",
            @"rawProviderPayload|raw_provider_payload|providerPayload"
        };

        foreach (var pattern in forbiddenPatterns)
        {
            Assert.DoesNotMatch(new Regex(pattern, RegexOptions.IgnoreCase), releasesJson);
        }
    }

    private static void AssertNoDuplicates(IEnumerable<string> values, string propertyName)
    {
        var duplicates = values
            .GroupBy(x => x, StringComparer.Ordinal)
            .Where(x => x.Count() > 1)
            .Select(x => x.Key)
            .ToArray();

        Assert.True(
            duplicates.Length == 0,
            $"Release seed must not contain duplicate {propertyName} values: {string.Join(", ", duplicates)}.");
    }

    private static RoadmapCounters CalculateRoadmapCounters(string roadmap)
    {
        var markers = Regex.Matches(
                roadmap,
                @"(?m)^- \[(?<status>x| |~|!)\] `(?<id>[^`]+)`")
            .Cast<Match>()
            .ToArray();

        var done = markers.Count(x => x.Groups["status"].Value == "x");
        var open = markers.Count(x => x.Groups["status"].Value == " ");
        var inProgress = markers.Count(x => x.Groups["status"].Value == "~");
        var blocked = markers.Count(x => x.Groups["status"].Value == "!");
        var total = markers.Length;
        var percent = Math.Round(done * 100m / total, 1, MidpointRounding.AwayFromZero)
            .ToString("0.0", CultureInfo.InvariantCulture);

        return new RoadmapCounters(total, done, total - done, open, inProgress, blocked, percent);
    }

    private sealed record RoadmapCounters(
        int Total,
        int Done,
        int Remaining,
        int Open,
        int InProgress,
        int Blocked,
        string Percent);

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
