using System.Text.Json;
using Xunit;

namespace VpnPlatform.UnitTests;

public class ReleaseDecisionTests
{
    [Fact]
    public void Release_Decision_Should_Be_Staging_Ready_Baseline_Not_Production_Ready()
    {
        var root = FindRepositoryRoot();
        var decision = File.ReadAllText(Path.Combine(root, "docs", "release-decision.md"));
        var roadmap = File.ReadAllText(Path.Combine(root, "docs", "PRODUCT_COMPLETION_ROADMAP.md"));
        var readme = File.ReadAllText(Path.Combine(root, "README.md"));
        var changelog = File.ReadAllText(Path.Combine(root, "CHANGELOG.md"));

        Assert.Contains("P11-ACC-007", decision, StringComparison.Ordinal);
        Assert.Contains("staging-ready baseline", decision, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("не production-ready", decision, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("P11-ACC-002 VPS production smoke", decision, StringComparison.Ordinal);
        Assert.Contains("ротировать любые секреты", decision, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("реальная 3x-ui", decision, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("[x] `P11-ACC-007`", roadmap, StringComparison.Ordinal);

        Assert.Contains("staging-ready baseline", readme, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("2026-08-10-cabinet-help-navigation", readme, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("0.104.0 - 2026-06-14", changelog, StringComparison.Ordinal);
        Assert.Contains("staging-ready baseline", changelog, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Release_Decision_Should_Match_Latest_Release_Seed_And_Test_Results()
    {
        var root = FindRepositoryRoot();
        var testResults = File.ReadAllText(Path.Combine(root, "TEST_RESULTS.md"));
        var docsIndex = File.ReadAllText(Path.Combine(root, "docs", "README.md"));
        using var releasesJson = JsonDocument.Parse(File.ReadAllText(Path.Combine(root, "backend", "src", "VpnPlatform.Api", "AppReleases", "releases.json")));

        var releaseDecision = releasesJson.RootElement
            .EnumerateArray()
            .Single(x => string.Equals(x.GetProperty("releaseId").GetString(), "2026-06-14-release-decision", StringComparison.Ordinal));

        Assert.Equal("0.104.0", releaseDecision.GetProperty("version").GetString());
        Assert.Contains("staging-ready baseline", releaseDecision.GetProperty("summary").GetString(), StringComparison.OrdinalIgnoreCase);
        Assert.Contains("2026-06-14-release-decision", testResults, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("ReleaseDecisionTests", testResults, StringComparison.Ordinal);
        Assert.Contains("release-decision.md", docsIndex, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Release_Decision_Should_Keep_Production_Gates_Explicit()
    {
        var root = FindRepositoryRoot();
        var decision = File.ReadAllText(Path.Combine(root, "docs", "release-decision.md"));

        foreach (var expected in new[]
                 {
                     "dotnet test backend/VpnPlatform.sln --configuration Release",
                     "powershell -ExecutionPolicy Bypass -File scripts\\fresh-local-smoke.ps1 -ApiPort 18101",
                     "powershell -ExecutionPolicy Bypass -File scripts\\scan-secrets.ps1",
                     "powershell -ExecutionPolicy Bypass -File scripts\\assert-production-readiness.ps1 -ReportPath docs\\staging-smoke-report.template.json",
                     "npm run e2e:console --prefix frontend",
                     "npm audit --audit-level=high --prefix frontend",
                     "successful GitHub Actions deploy",
                     "/health/live",
                     "/health/ready",
                     "post-deploy smoke"
                 })
        {
            Assert.Contains(expected, decision, StringComparison.OrdinalIgnoreCase);
        }

        Assert.Contains("payment sandbox webhook", decision, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("VPN access URI", decision, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("cookies, tokens, `.env`", decision, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Release_Decision_Should_Keep_External_Evidence_Blockers_Tied_To_Open_Roadmap_Items()
    {
        var root = FindRepositoryRoot();
        var decision = File.ReadAllText(Path.Combine(root, "docs", "release-decision.md"));
        var roadmap = File.ReadAllText(Path.Combine(root, "docs", "PRODUCT_COMPLETION_ROADMAP.md"));

        foreach (var openMarker in new[]
                 {
                     "[ ] `STATE-011`",
                     "[ ] `STATE-012`",
                     "[ ] `STATE-013`",
                     "[ ] `P0-ADMIN-001`",
                     "[ ] `P0-ADMIN-002`",
                     "[ ] `P0-VPN-001`",
                     "[ ] `P0-VPN-004`",
                     "[ ] `P0-PAY-002`",
                     "[ ] `P0-PAY-009`",
                     "[~] `P9-TST-007`",
                     "[ ] `P11-ACC-002`"
                 })
        {
            Assert.Contains(openMarker, roadmap, StringComparison.Ordinal);
        }

        foreach (var blocker in new[]
                 {
                     "P11-ACC-002 VPS production smoke",
                     "live deploy -> health -> admin login -> public order -> payment -> subscription -> VPN access",
                     "домен",
                     "HTTPS",
                     "PostgreSQL backup/restore",
                     "sandbox",
                     "3x-ui",
                     "Telegram bot webhook/invoice flow"
                 })
        {
            Assert.Contains(blocker, decision, StringComparison.OrdinalIgnoreCase);
        }

        Assert.Contains("staging-ready baseline", decision, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("не production-ready", decision, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("production-ready accepted", decision, StringComparison.OrdinalIgnoreCase);
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

        throw new InvalidOperationException("Repository root was not found for release decision tests.");
    }
}
