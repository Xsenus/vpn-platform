using System.Text.Json;
using Xunit;

namespace VpnPlatform.UnitTests;

public class BranchProtectionGuardTests
{
    [Fact]
    public void Required_Checks_Config_Should_Match_Ci_Workflow_Job_Names()
    {
        var root = FindRepositoryRoot();
        var workflow = File.ReadAllText(Path.Combine(root, ".github", "workflows", "ci.yml"));
        using var config = JsonDocument.Parse(File.ReadAllText(Path.Combine(root, ".github", "branch-protection.required-checks.json")));

        var contexts = config.RootElement
            .GetProperty("requiredStatusChecks")
            .GetProperty("contexts")
            .EnumerateArray()
            .Select(x => x.GetString() ?? string.Empty)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToArray();

        Assert.Equal(new[]
        {
            "backend restore, build, test, EF",
            "frontend install, typecheck, build, test",
            "provisioning runner and Ansible syntax",
            "docker compose config and image build"
        }, contexts);

        foreach (var context in contexts)
        {
            Assert.Contains($"name: {context}", workflow, StringComparison.OrdinalIgnoreCase);
        }

        Assert.DoesNotContain("deploy to VPS", contexts);
        Assert.DoesNotContain("validate before deploy", contexts);
    }

    [Fact]
    public void Branch_Protection_Config_Should_Block_Risky_Main_Changes()
    {
        var root = FindRepositoryRoot();
        using var config = JsonDocument.Parse(File.ReadAllText(Path.Combine(root, ".github", "branch-protection.required-checks.json")));
        var rootElement = config.RootElement;

        Assert.Equal("Xsenus/vpn-platform", rootElement.GetProperty("repository").GetString());
        Assert.Contains("main", rootElement.GetProperty("branches").EnumerateArray().Select(x => x.GetString()));
        Assert.True(rootElement.GetProperty("requiredStatusChecks").GetProperty("strict").GetBoolean());
        Assert.True(rootElement.GetProperty("enforceAdmins").GetBoolean());
        Assert.True(rootElement.GetProperty("requiredConversationResolution").GetBoolean());
        Assert.False(rootElement.GetProperty("allowForcePushes").GetBoolean());
        Assert.False(rootElement.GetProperty("allowDeletions").GetBoolean());
        Assert.True(rootElement.GetProperty("pullRequestReviews").GetProperty("dismissStaleReviews").GetBoolean());
        Assert.Equal(1, rootElement.GetProperty("pullRequestReviews").GetProperty("requiredApprovingReviewCount").GetInt32());
    }

    [Fact]
    public void Branch_Protection_Script_Should_Use_GitHub_Rest_Api_And_Keep_Token_Out_Of_Files()
    {
        var root = FindRepositoryRoot();
        var script = File.ReadAllText(Path.Combine(root, "scripts", "configure-branch-protection.ps1"));
        var documentation = File.ReadAllText(Path.Combine(root, "docs", "github-required-checks.md"));

        Assert.Contains("api.github.com/repos", script, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("required_status_checks", script, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("required_pull_request_reviews", script, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("required_conversation_resolution", script, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("GITHUB_TOKEN", script, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("GH_TOKEN", script, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("DryRun", script, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Branch '", script, StringComparison.OrdinalIgnoreCase);

        Assert.Contains("scripts/configure-branch-protection.ps1", documentation, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Settings -> Branches", documentation, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("ghp_", script, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("github_pat_", script, StringComparison.OrdinalIgnoreCase);
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

        throw new InvalidOperationException("Repository root was not found for branch protection guard tests.");
    }
}
