using System.Text.Json;
using System.Text.RegularExpressions;
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
        Assert.Contains("989/989", readme, StringComparison.Ordinal);
        Assert.Contains("2026-08-05-admin-rbac-admission", readme, StringComparison.OrdinalIgnoreCase);

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
    public void Final_Runbook_Should_Keep_Current_External_Evidence_Limits_Explicit()
    {
        var root = FindRepositoryRoot();
        var runbook = File.ReadAllText(Path.Combine(root, "docs", "final-runbook.md"));
        var roadmap = File.ReadAllText(Path.Combine(root, "docs", "PRODUCT_COMPLETION_ROADMAP.md"));

        foreach (var expected in new[]
                 {
                     "989/989",
                     "2026-08-05-admin-rbac-admission",
                     "0.493.0",
                     "staging-ready baseline",
                     "production-ready",
                     "live VPS smoke",
                     "HTTPS",
                     "secret manager",
                     "backup/restore",
                     "sandbox",
                     "3x-ui",
                     "P11-ACC-008 Production readiness gate"
                 })
        {
            Assert.Contains(expected, runbook, StringComparison.OrdinalIgnoreCase);
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
            Assert.Contains(openMarker, roadmap, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void Test_Results_Should_Keep_Current_External_Evidence_Status_Explicit()
    {
        var root = FindRepositoryRoot();
        var testResults = File.ReadAllText(Path.Combine(root, "TEST_RESULTS.md"));
        var roadmap = File.ReadAllText(Path.Combine(root, "docs", "PRODUCT_COMPLETION_ROADMAP.md"));

        foreach (var expected in new[]
                 {
                     "2026-08-05-admin-rbac-admission",
                     "0.493.0",
                     "505/525",
                     "96.2%",
                     "20",
                     "19",
                     "1",
                     "0 blockers",
                     "Backend full suite: OK, `989/989`",
                     "Local SQLite smoke: OK",
                     "Secret scan: OK, `610` files, `0` findings",
                     "Artifact cleanup: OK",
                     "external evidence remains open",
                     "real VPS/staging/live evidence remains open"
                 })
        {
            Assert.Contains(expected, testResults, StringComparison.OrdinalIgnoreCase);
        }

        foreach (var forbidden in new[]
                 {
                     "pending final local validation",
                     "Artifact cleanup: pending"
                 })
        {
            Assert.DoesNotContain(forbidden, testResults, StringComparison.OrdinalIgnoreCase);
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
            Assert.Contains(openMarker, roadmap, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void Changelog_Should_Keep_Current_External_Evidence_Status_Explicit()
    {
        var root = FindRepositoryRoot();
        var changelog = File.ReadAllText(Path.Combine(root, "CHANGELOG.md"));
        var roadmap = File.ReadAllText(Path.Combine(root, "docs", "PRODUCT_COMPLETION_ROADMAP.md"));

        foreach (var expected in new[]
                 {
                     "2026-08-05-admin-rbac-admission",
                     "0.493.0",
                     "505/525",
                     "96.2%",
                     "20",
                     "19",
                     "1",
                     "`0` blocked",
                     "staging-ready baseline",
                     "not production-ready",
                     "FinalDocsChangelogTests",
                     "targeted X3Ui/panel/SQLite suite `52/52`",
                     "backend full suite `989/989`",
                     "PostgreSQL SQL",
                     "secret scan `610` files, `0` findings"
                 })
        {
            Assert.Contains(expected, changelog, StringComparison.OrdinalIgnoreCase);
        }

        foreach (var forbidden in new[]
                 {
                     "pending final local validation",
                     "production-ready accepted"
                 })
        {
            Assert.DoesNotContain(forbidden, changelog, StringComparison.OrdinalIgnoreCase);
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
            Assert.Contains(openMarker, roadmap, StringComparison.Ordinal);
        }
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

    [Fact]
    public void Changelog_And_Test_Results_Top_Blocks_Should_Match_Latest_Release_Seed()
    {
        var root = FindRepositoryRoot();
        var changelog = File.ReadAllText(Path.Combine(root, "CHANGELOG.md"));
        var testResults = File.ReadAllText(Path.Combine(root, "TEST_RESULTS.md"));
        using var releasesJson = JsonDocument.Parse(File.ReadAllText(Path.Combine(
            root,
            "backend",
            "src",
            "VpnPlatform.Api",
            "AppReleases",
            "releases.json")));

        var latestRelease = releasesJson.RootElement
            .EnumerateArray()
            .Where(x => x.GetProperty("isActive").GetBoolean())
            .OrderByDescending(x => x.GetProperty("releasedAt").GetDateTimeOffset())
            .First();
        var releaseId = latestRelease.GetProperty("releaseId").GetString() ?? string.Empty;
        var version = latestRelease.GetProperty("version").GetString() ?? string.Empty;

        var changelogTopEntry = Regex.Match(
            changelog,
            @"(?s)\A# Changelog\s+## (?<version>[^\r\n]+?) - 2026-08-05(?<body>.*?)(?:\r?\n## |\z)");
        Assert.True(changelogTopEntry.Success, "CHANGELOG.md must start with the latest release block.");
        Assert.Contains(version, changelogTopEntry.Groups["version"].Value, StringComparison.Ordinal);
        Assert.Contains(releaseId, changelogTopEntry.Groups["body"].Value, StringComparison.OrdinalIgnoreCase);

        var testResultsTopEntry = Regex.Match(
            testResults,
            @"(?s)\A# .+?\r?\n\r?\nДата проверки: 2026-05-25\.\s+## Check 2026-08-05: (?<title>[^\r\n]+)(?<body>.*?)(?:\r?\n## Check |\z)");
        Assert.True(testResultsTopEntry.Success, "TEST_RESULTS.md must start with the latest release check block.");
        Assert.Contains(releaseId, testResultsTopEntry.Groups["body"].Value, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(version, testResultsTopEntry.Groups["body"].Value, StringComparison.Ordinal);
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
