using System.Text.Json;
using Xunit;

namespace VpnPlatform.UnitTests;

public class ReleaseDocumentationGuardTests
{
    private static readonly ReleaseNoteExpectation[] Expectations =
    [
        new("P2-ADM-CNT-001", "2026-06-11-admin-home-content-readiness"),
        new("P2-ADM-FAQ-001", "2026-06-11-admin-faq-management"),
        new("P2-ADM-REL-001", "2026-06-11-app-version-management"),
        new("P2-ADM-REL-002", "2026-06-11-release-note-guard"),
        new("P7-PROV-005", "2026-06-12-live-provisioning-runbook"),
        new("P8-CI-001", "2026-06-12-deploy-mode-auto-detect"),
        new("P8-CI-002", "2026-06-12-required-checks-main"),
        new("P8-CI-003", "2026-06-13-github-secrets-audit"),
        new("P8-CI-004", "2026-06-13-vps-maintenance-safe-cleanup"),
        new("P8-CI-005", "2026-06-13-post-deploy-smoke")
    ];

    [Fact]
    public void Completed_Roadmap_Items_Should_Have_Whats_New_Release_And_Test_Result()
    {
        var root = FindRepositoryRoot();
        var roadmap = File.ReadAllText(Path.Combine(root, "docs", "PRODUCT_COMPLETION_ROADMAP.md"));
        var testResults = File.ReadAllText(Path.Combine(root, "TEST_RESULTS.md"));
        using var releasesJson = JsonDocument.Parse(File.ReadAllText(Path.Combine(root, "backend", "src", "VpnPlatform.Api", "AppReleases", "releases.json")));
        var releaseIds = releasesJson.RootElement
            .EnumerateArray()
            .Select(x => x.GetProperty("releaseId").GetString() ?? string.Empty)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var expectation in Expectations)
        {
            Assert.Contains($"[x] `{expectation.RoadmapId}`", roadmap, StringComparison.OrdinalIgnoreCase);
            Assert.Contains(expectation.ReleaseId, releaseIds);
            Assert.Contains(expectation.ReleaseId, testResults, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void Whats_New_Seed_Should_Keep_User_Facing_Release_Items()
    {
        var root = FindRepositoryRoot();
        using var releasesJson = JsonDocument.Parse(File.ReadAllText(Path.Combine(root, "backend", "src", "VpnPlatform.Api", "AppReleases", "releases.json")));

        foreach (var release in releasesJson.RootElement.EnumerateArray())
        {
            var releaseId = release.GetProperty("releaseId").GetString() ?? string.Empty;
            Assert.False(string.IsNullOrWhiteSpace(releaseId));
            Assert.False(string.IsNullOrWhiteSpace(release.GetProperty("version").GetString()));
            Assert.False(string.IsNullOrWhiteSpace(release.GetProperty("title").GetString()));
            Assert.False(string.IsNullOrWhiteSpace(release.GetProperty("summary").GetString()));

            var items = release.GetProperty("items").EnumerateArray().ToArray();
            Assert.NotEmpty(items);
            Assert.All(items, item =>
            {
                Assert.Contains(item.GetProperty("type").GetString(), new[] { "new", "improved", "fixed", "important" });
                Assert.False(string.IsNullOrWhiteSpace(item.GetProperty("text").GetString()));
            });
        }
    }

    [Fact]
    public void Live_Provisioning_Runbook_Should_Cover_Operator_Gates()
    {
        var root = FindRepositoryRoot();
        var runbook = File.ReadAllText(Path.Combine(root, "docs", "live-provisioning-runbook.md"));

        Assert.Contains("Provisioning__LiveExecutionEnabled=true", runbook, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Provisioning__AllowLiveDeploy=true", runbook, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("explicit-live-provisioning:true", runbook, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("validation-mode:false", runbook, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Provisioning__KnownHostsPath", runbook, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("ansible-playbook --syntax-check", runbook, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("POST /api/admin/servers/{id}/precheck", runbook, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("POST /api/admin/provisioning-runs/{id}/deploy", runbook, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Precheck report", runbook, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Rollback node state", runbook, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Fail-closed", runbook, StringComparison.OrdinalIgnoreCase);
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, ".env.example")) && Directory.Exists(Path.Combine(directory.FullName, "backend")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Repository root was not found for release documentation guard tests.");
    }

    private sealed record ReleaseNoteExpectation(string RoadmapId, string ReleaseId);
}
