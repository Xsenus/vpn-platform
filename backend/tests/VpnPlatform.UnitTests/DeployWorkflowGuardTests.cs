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

    [Fact]
    public void Deploy_Workflow_Should_Normalize_Production_Env_Before_Upload()
    {
        var root = FindRepositoryRoot();
        var workflow = File.ReadAllText(Path.Combine(root, ".github", "workflows", "deploy-vps.yml"));
        var script = File.ReadAllText(Path.Combine(root, "scripts", "normalize-production-env.ps1"));
        var regression = File.ReadAllText(Path.Combine(root, "scripts", "test-normalize-production-env.ps1"));

        Assert.Contains("name: Normalize production environment file", workflow, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("scripts/normalize-production-env.ps1 -Path production.env -OutputPath production.env", workflow, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("scp -P \"$VPS_PORT\" production.env", workflow, StringComparison.OrdinalIgnoreCase);

        var normalizeIndex = workflow.IndexOf("name: Normalize production environment file", StringComparison.OrdinalIgnoreCase);
        var dockerUploadIndex = workflow.IndexOf("name: Upload Docker release and environment file", StringComparison.OrdinalIgnoreCase);
        var systemdUploadIndex = workflow.IndexOf("name: Upload systemd release and environment file", StringComparison.OrdinalIgnoreCase);
        Assert.True(normalizeIndex > 0);
        Assert.True(normalizeIndex < dockerUploadIndex);
        Assert.True(normalizeIndex < systemdUploadIndex);

        foreach (var expected in new[]
                 {
                     "ASPNETCORE_ENVIRONMENT",
                     "AdminBootstrap__Enabled",
                     "AdminBootstrap__Password",
                     "AdminBootstrap__ResetExistingPassword",
                     "Database__ApplyMigrationsOnStartup",
                     "Database__SeedDemoData",
                     "Swagger__Enabled"
                 })
        {
            Assert.Contains(expected, script, StringComparison.Ordinal);
            Assert.Contains(expected, regression, StringComparison.Ordinal);
        }

        Assert.Contains("\"Production\"", script, StringComparison.Ordinal);
        Assert.Contains("\"false\"", script, StringComparison.Ordinal);
        Assert.Contains("temporary-secret-that-must-not-stay", regression, StringComparison.Ordinal);
    }

    [Fact]
    public void Production_Env_Normalizer_Regression_Should_Cleanup_Default_Tmp()
    {
        var root = FindRepositoryRoot();
        var regression = File.ReadAllText(Path.Combine(root, "scripts", "test-normalize-production-env.ps1"));
        var documentation = File.ReadAllText(Path.Combine(root, "docs", "github-deployment.md"));
        var roadmap = File.ReadAllText(Path.Combine(root, "docs", "PRODUCT_COMPLETION_ROADMAP.md"));

        foreach (var expected in new[]
                 {
                     "$defaultOutputDirectory",
                     "$usingDefaultOutputDirectory",
                     "Assert-InWorkspace",
                     "finally",
                     "Get-ChildItem -LiteralPath $tmpDirectory -Force",
                     "Remove-Item -LiteralPath $tmpDirectory -Force"
                 })
        {
            Assert.Contains(expected, regression, StringComparison.OrdinalIgnoreCase);
        }

        Assert.Contains("test-normalize-production-env.ps1", documentation, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("removes its autogenerated production.env fixtures and empty `tmp` directory", documentation, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("[x] `P8-CI-007`", roadmap, StringComparison.Ordinal);
    }

    [Fact]
    public void Docker_Deploy_Workflow_Should_Cleanup_Remote_Tmp_Artifacts()
    {
        var root = FindRepositoryRoot();
        var workflow = File.ReadAllText(Path.Combine(root, ".github", "workflows", "deploy-vps.yml"));
        var documentation = File.ReadAllText(Path.Combine(root, "docs", "github-deployment.md"));
        var roadmap = File.ReadAllText(Path.Combine(root, "docs", "PRODUCT_COMPLETION_ROADMAP.md"));

        foreach (var expected in new[]
                 {
                     "tmp_dir=\"$(mktemp -d)\"",
                     "cleanup()",
                     "rm -rf \"$tmp_dir\"",
                     "trap cleanup EXIT",
                     ">\"$tmp_dir/vpnplatform-compose.yml\""
                 })
        {
            Assert.Contains(expected, workflow, StringComparison.OrdinalIgnoreCase);
        }

        Assert.DoesNotContain(">/tmp/vpnplatform-compose.yml", workflow, StringComparison.OrdinalIgnoreCase);
        Assert.True(
            workflow.IndexOf("trap cleanup EXIT", StringComparison.OrdinalIgnoreCase)
            < workflow.IndexOf("docker compose --project-name vpnplatform --env-file .env -f docker-compose.yml config", StringComparison.OrdinalIgnoreCase));

        Assert.Contains("Start Docker production stack", documentation, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("temporary compose config artifact", documentation, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("[x] `P8-CI-009`", roadmap, StringComparison.Ordinal);
    }

    [Fact]
    public void Ci_Ansible_Syntax_Check_Should_Cleanup_Tmp_Inventory()
    {
        var root = FindRepositoryRoot();
        var workflow = File.ReadAllText(Path.Combine(root, ".github", "workflows", "ci.yml"));
        var documentation = File.ReadAllText(Path.Combine(root, "docs", "github-deployment.md"));
        var roadmap = File.ReadAllText(Path.Combine(root, "docs", "PRODUCT_COMPLETION_ROADMAP.md"));

        foreach (var expected in new[]
                 {
                     "tmp_dir=\"$(mktemp -d)\"",
                     "cleanup()",
                     "rm -rf \"$tmp_dir\"",
                     "trap cleanup EXIT",
                     "\"$tmp_dir/inventory.ini\"",
                     "ansible-playbook --syntax-check -i \"$tmp_dir/inventory.ini\""
                 })
        {
            Assert.Contains(expected, workflow, StringComparison.OrdinalIgnoreCase);
        }

        Assert.DoesNotContain("/tmp/vpnplatform-ci", workflow, StringComparison.OrdinalIgnoreCase);
        Assert.True(
            workflow.IndexOf("trap cleanup EXIT", StringComparison.OrdinalIgnoreCase)
            < workflow.IndexOf("ansible-playbook --syntax-check", StringComparison.OrdinalIgnoreCase));

        Assert.Contains("Ansible syntax check", documentation, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("per-run temp directory", documentation, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("[x] `P8-CI-010`", roadmap, StringComparison.Ordinal);
    }

    [Fact]
    public void Validate_Repo_Should_Cleanup_Ansible_Tmp_Inventory_On_Failure()
    {
        var root = FindRepositoryRoot();
        var script = File.ReadAllText(Path.Combine(root, "scripts", "validate_repo.sh"));
        var documentation = File.ReadAllText(Path.Combine(root, "docs", "phase-1-stabilization.md"));
        var roadmap = File.ReadAllText(Path.Combine(root, "docs", "PRODUCT_COMPLETION_ROADMAP.md"));

        foreach (var expected in new[]
                 {
                     "TMP_DIR=\"$(mktemp -d)\"",
                     "cleanup_ansible_tmp()",
                     "rm -rf \"$TMP_DIR\"",
                     "trap cleanup_ansible_tmp EXIT",
                     "\"$TMP_DIR/inventory.ini\""
                 })
        {
            Assert.Contains(expected, script, StringComparison.OrdinalIgnoreCase);
        }

        Assert.True(
            script.IndexOf("trap cleanup_ansible_tmp EXIT", StringComparison.OrdinalIgnoreCase)
            < script.IndexOf("ansible-playbook --syntax-check", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(
            "ansible-playbook --syntax-check -i \"$TMP_DIR/inventory.ini\" infra/ansible/playbooks/provision-node.yml\n  rm -rf \"$TMP_DIR\"",
            script.Replace("\r\n", "\n"),
            StringComparison.OrdinalIgnoreCase);

        Assert.Contains("validate_repo.sh", documentation, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("temporary Ansible inventory", documentation, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("[x] `P8-CI-011`", roadmap, StringComparison.Ordinal);
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
