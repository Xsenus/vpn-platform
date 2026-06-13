using Xunit;

namespace VpnPlatform.UnitTests;

public class PostDeploySmokeTests
{
    [Fact]
    public void Post_Deploy_Smoke_Script_Should_Check_Api_Providers_And_All_Web_Apps()
    {
        var root = FindRepositoryRoot();
        var script = File.ReadAllText(Path.Combine(root, "scripts", "post-deploy-smoke.sh"));

        Assert.Contains("set -euo pipefail", script, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("API_BASE_URL", script, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("PUBLIC_WEB_URL", script, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("CABINET_WEB_URL", script, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("ADMIN_WEB_URL", script, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("/health/live", script, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("/health/ready", script, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("/metrics", script, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("/api/public/payments/providers", script, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("REQUIRE_PUBLIC_PAYMENT_PROVIDERS", script, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Public web", script, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Cabinet web", script, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Admin web", script, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("GITHUB_STEP_SUMMARY", script, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Post-deploy smoke passed", script, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Deploy_Workflow_Should_Run_Post_Deploy_Smoke_After_Docker_Or_Systemd_Deploy()
    {
        var root = FindRepositoryRoot();
        var workflow = File.ReadAllText(Path.Combine(root, ".github", "workflows", "deploy-vps.yml"));

        Assert.Contains("name: Post-deploy smoke", workflow, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("DEPLOY_MODE: ${{ steps.deploy_mode.outputs.mode }}", workflow, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("POST_DEPLOY_API_URL_SECRET", workflow, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("POST_DEPLOY_PUBLIC_WEB_URL_SECRET", workflow, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("POST_DEPLOY_CABINET_WEB_URL_SECRET", workflow, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("POST_DEPLOY_ADMIN_WEB_URL_SECRET", workflow, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("public_web_url=\"http://$VPS_HOST:5173\"", workflow, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("public_web_url=\"${VITE_PUBLIC_WEB_URL_SECRET:-http://$VPS_HOST}\"", workflow, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("cabinet_web_url=\"${POST_DEPLOY_CABINET_WEB_URL_SECRET:-http://$VPS_HOST:5174}\"", workflow, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("admin_web_url=\"${POST_DEPLOY_ADMIN_WEB_URL_SECRET:-http://$VPS_HOST:5175}\"", workflow, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("scripts/post-deploy-smoke.sh", workflow, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("REQUIRE_PUBLIC_PAYMENT_PROVIDERS=true", workflow, StringComparison.OrdinalIgnoreCase);

        var smokeIndex = workflow.IndexOf("name: Post-deploy smoke", StringComparison.OrdinalIgnoreCase);
        var dockerIndex = workflow.IndexOf("name: Start Docker production stack", StringComparison.OrdinalIgnoreCase);
        var systemdIndex = workflow.IndexOf("name: Start systemd production release", StringComparison.OrdinalIgnoreCase);
        Assert.True(smokeIndex > dockerIndex);
        Assert.True(smokeIndex > systemdIndex);
    }

    [Fact]
    public void Post_Deploy_Smoke_Documentation_Should_Explain_Actions_Evidence_And_Secrets()
    {
        var root = FindRepositoryRoot();
        var documentation = File.ReadAllText(Path.Combine(root, "docs", "post-deploy-smoke.md"));
        var secretsDocumentation = File.ReadAllText(Path.Combine(root, "docs", "github-secrets-audit.md"));

        Assert.Contains("scripts/post-deploy-smoke.sh", documentation, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("POST_DEPLOY_API_URL", documentation, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("POST_DEPLOY_PUBLIC_WEB_URL", documentation, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("POST_DEPLOY_CABINET_WEB_URL", documentation, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("POST_DEPLOY_ADMIN_WEB_URL", documentation, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Public payment providers", documentation, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("GITHUB_STEP_SUMMARY", documentation, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("P8-CI-005", documentation, StringComparison.OrdinalIgnoreCase);

        Assert.Contains("POST_DEPLOY_API_URL", secretsDocumentation, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("POST_DEPLOY_PUBLIC_WEB_URL", secretsDocumentation, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("POST_DEPLOY_CABINET_WEB_URL", secretsDocumentation, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("POST_DEPLOY_ADMIN_WEB_URL", secretsDocumentation, StringComparison.OrdinalIgnoreCase);
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

        throw new InvalidOperationException("Repository root was not found for post-deploy smoke tests.");
    }
}
