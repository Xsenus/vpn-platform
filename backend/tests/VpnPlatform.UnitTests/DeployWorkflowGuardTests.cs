using Xunit;

namespace VpnPlatform.UnitTests;

public class DeployWorkflowGuardTests
{
    [Fact]
    public void Deploy_Vps_Workflow_Should_Auto_Detect_Docker_Or_Systemd_And_Log_Selection()
    {
        var root = FindRepositoryRoot();
        var workflow = File.ReadAllText(Path.Combine(root, ".github", "workflows", "deploy-vps.yml"));

        Assert.Contains("deploy_mode:", workflow, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("- auto", workflow, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("- docker", workflow, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("- systemd", workflow, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("REQUESTED_DEPLOY_MODE", workflow, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("command -v docker", workflow, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("docker compose version", workflow, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("mode=\"docker\"", workflow, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("mode=\"systemd\"", workflow, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("docker_detected", workflow, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("reason=", workflow, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("::notice title=Deploy mode::requested=$requested selected=$mode", workflow, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("GITHUB_STEP_SUMMARY", workflow, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("steps.deploy_mode.outputs.mode == 'docker'", workflow, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("steps.deploy_mode.outputs.mode == 'systemd'", workflow, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Deploy_Auto_Detect_Documentation_Should_Explain_Actions_Log_Evidence()
    {
        var root = FindRepositoryRoot();
        var documentation = File.ReadAllText(Path.Combine(root, "docs", "deploy-vps-auto-detect.md"));

        Assert.Contains("Deploy mode", documentation, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Requested", documentation, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Selected", documentation, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Docker detected", documentation, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("GITHUB_STEP_SUMMARY", documentation, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("docker compose version", documentation, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("VPS_DEPLOY_MODE", documentation, StringComparison.OrdinalIgnoreCase);
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

        throw new InvalidOperationException("Repository root was not found for deploy workflow guard tests.");
    }
}
