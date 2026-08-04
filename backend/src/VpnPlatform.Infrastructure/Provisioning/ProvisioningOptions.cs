namespace VpnPlatform.Infrastructure.Provisioning;

public sealed class ProvisioningOptions
{
    public string PythonBinary { get; set; } = "python3";
    public string AnsibleBinary { get; set; } = "ansible-playbook";
    public string RunnerScriptPath { get; set; } = "../infra/ansible/runner/run_playbook.py";
    public string ProvisionPlaybookPath { get; set; } = "../infra/ansible/playbooks/provision-node.yml";
    public string PrecheckPlaybookPath { get; set; } = "../infra/ansible/playbooks/precheck-node.yml";
    public string WorkingDirectory { get; set; } = "/tmp/vpnplatform-provisioning";
    public string KnownHostsPath { get; set; } = string.Empty;
    public int ExecutionTimeoutSeconds { get; set; } = 3600;

    // Safe by default: validation/staging smoke must never SSH to a real VPS unless both flags are explicitly enabled.
    public bool LiveExecutionEnabled { get; set; } = false;
    public bool AllowLiveDeploy { get; set; } = false;
}
