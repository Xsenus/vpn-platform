using System.Diagnostics;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using VpnPlatform.Infrastructure.Security;
using VpnPlatform.Application.Abstractions;
using VpnPlatform.Application.DTOs;
using VpnPlatform.Domain.Entities;

namespace VpnPlatform.Infrastructure.Provisioning;

public sealed class AnsibleProvisioningExecutor : IProvisioningExecutor
{
    private readonly ProvisioningOptions _options;
    private readonly ILogger<AnsibleProvisioningExecutor> _logger;

    public AnsibleProvisioningExecutor(IOptions<ProvisioningOptions> options, ILogger<AnsibleProvisioningExecutor> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task<ProvisioningExecutionResult> ExecuteAsync(VpnNode node, ProvisioningRun run, CancellationToken cancellationToken)
    {
        if (!_options.LiveExecutionEnabled)
        {
            return await Task.FromResult(BuildMockResult(node, run));
        }

        if (!run.DryRun && !_options.AllowLiveDeploy)
        {
            return new ProvisioningExecutionResult(
                false,
                "Live provisioning is disabled. Set Provisioning:AllowLiveDeploy=true only for an approved staging/live target.",
                new[]
                {
                    new ProvisioningStepResult(
                        "Live deploy guard",
                        false,
                        "LiveExecutionEnabled=true but AllowLiveDeploy=false. No SSH/Ansible deploy was started.",
                        "Live deploy requires explicit Provisioning:AllowLiveDeploy=true.")
                },
                null,
                "Live deploy is disabled by policy.");
        }

        var workDirectory = Path.Combine(_options.WorkingDirectory, run.Id.ToString("N"));
        Directory.CreateDirectory(workDirectory);

        var runnerScript = ResolveExistingPath(_options.RunnerScriptPath);
        var playbookPath = ResolveExistingPath(run.DryRun ? _options.PrecheckPlaybookPath : _options.ProvisionPlaybookPath);

        var arguments = new List<string>
        {
            Quote(runnerScript),
            "--playbook", Quote(playbookPath),
            "--host", Quote(!string.IsNullOrWhiteSpace(node.IpAddress) ? node.IpAddress : node.Host),
            "--ssh-user", Quote(string.IsNullOrWhiteSpace(node.SshUser) ? "root" : node.SshUser),
            "--ssh-port", node.SshPort.ToString(),
            "--workdir", Quote(workDirectory),
            "--ansible-binary", Quote(_options.AnsibleBinary)
        };

        if (!string.IsNullOrWhiteSpace(node.Host))
        {
            arguments.Add("--inventory-name");
            arguments.Add(Quote(node.Host));
        }

        if (!string.IsNullOrWhiteSpace(node.ProtectedSshCredential) || !string.IsNullOrWhiteSpace(node.SshCredentialRef))
        {
            return new ProvisioningExecutionResult(
                false,
                "Protected SSH credentials are configured, but live Ansible credential materialization is not enabled in this MVP. Use validation mode or configure an approved key path on a staging host.",
                new[] { new ProvisioningStepResult("SSH credential guard", false, "Protected credentials were detected and not exposed to ansible-playbook.", "Live protected credential materialization is not implemented in this MVP.") },
                workDirectory,
                "Protected SSH credentials cannot be used for live Ansible in this MVP.");
        }

        if (!string.IsNullOrWhiteSpace(node.SshPrivateKeyPath))
        {
            if (node.SshPrivateKeyPath.StartsWith("v1:", StringComparison.Ordinal) || node.SshPrivateKeyPath.StartsWith("validation-placeholder:", StringComparison.Ordinal))
            {
                return new ProvisioningExecutionResult(
                    false,
                    "Protected SSH credentials are configured in the legacy key-path field, but live Ansible credential materialization is not enabled in this MVP. Use validation mode or configure an approved key path on a staging host.",
                    new[] { new ProvisioningStepResult("SSH credential guard", false, "Protected credentials were detected and not exposed to ansible-playbook.", "Live protected credential materialization is not implemented in this MVP.") },
                    workDirectory,
                    "Protected SSH credentials cannot be used for live Ansible in this MVP.");
            }

            arguments.Add("--private-key-path");
            arguments.Add(Quote(node.SshPrivateKeyPath));
        }

        if (!string.IsNullOrWhiteSpace(_options.KnownHostsPath))
        {
            arguments.Add("--known-hosts-path");
            arguments.Add(Quote(_options.KnownHostsPath));
        }

        if (node.SkipHostKeyChecking)
        {
            arguments.Add("--skip-host-key-checking");
        }

        if (run.DryRun)
        {
            arguments.Add("--check");
        }

        var extraVars = new Dictionary<string, object?>
        {
            ["node_name"] = node.Name,
            ["node_region"] = node.Region,
            ["node_country"] = node.Country,
            ["node_provider"] = node.Provider,
            ["x3ui_port"] = 2053,
            ["vpn_platform_user"] = "vpnplatform",
            ["panel_base_url"] = node.PanelBaseUrl,
            ["panel_username"] = node.PanelUsername,
            ["panel_password"] = string.Empty,
            ["panel_inbound_id"] = node.PanelInboundId,
            ["public_hostname"] = node.PublicHostname,
            ["public_port"] = node.PublicPort,
            ["install_xui"] = !run.DryRun
        };

        var extraVarsPath = Path.Combine(workDirectory, "extra-vars.json");
        await File.WriteAllTextAsync(extraVarsPath, JsonSerializer.Serialize(extraVars, new JsonSerializerOptions { WriteIndented = true }), cancellationToken);
        arguments.Add("--extra-vars-file");
        arguments.Add(Quote(extraVarsPath));

        var psi = new ProcessStartInfo
        {
            FileName = _options.PythonBinary,
            Arguments = string.Join(' ', arguments),
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = workDirectory
        };

        _logger.LogInformation("Starting provisioning run {RunId} for node {NodeId} with playbook {PlaybookPath}", run.Id, node.Id, playbookPath);

        using var process = Process.Start(psi) ?? throw new InvalidOperationException("Unable to start provisioning runner process.");
        var stdout = await process.StandardOutput.ReadToEndAsync(cancellationToken);
        var stderr = await process.StandardError.ReadToEndAsync(cancellationToken);
        await process.WaitForExitAsync(cancellationToken);

        var knownSecrets = new[] { node.PanelPassword, node.ProtectedPanelPassword, node.SshPrivateKeyPath, node.ProtectedSshCredential };
        var redactedStdout = SecretRedactor.Redact(stdout, knownSecrets);
        var redactedStderr = SecretRedactor.Redact(stderr, knownSecrets);

        TryDeleteSensitiveFile(extraVarsPath);

        if (!string.IsNullOrWhiteSpace(redactedStderr))
        {
            _logger.LogWarning("Provisioning runner stderr for {RunId}: {Stderr}", run.Id, redactedStderr);
        }

        RunnerResponse? response = null;
        if (!string.IsNullOrWhiteSpace(stdout))
        {
            response = JsonSerializer.Deserialize<RunnerResponse>(stdout, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }

        if (response is null)
        {
            return new ProvisioningExecutionResult(
                false,
                $"Provisioning runner returned unreadable response. ExitCode={process.ExitCode}",
                new[] { new ProvisioningStepResult("runner", false, redactedStdout, redactedStderr) },
                workDirectory,
                redactedStderr);
        }

        var steps = response.Steps?
            .Select(x => new ProvisioningStepResult(
                x.StepName ?? "ansible",
                x.Success,
                SecretRedactor.Redact(x.Output, knownSecrets),
                SecretRedactor.Redact(x.ErrorText, knownSecrets)))
            .ToArray()
            ?? Array.Empty<ProvisioningStepResult>();

        return new ProvisioningExecutionResult(
            response.Success && process.ExitCode == 0,
            SecretRedactor.Redact(response.SummaryLog, knownSecrets),
            steps,
            response.WorkDirectory ?? workDirectory,
            SecretRedactor.Redact(response.ErrorText ?? stderr, knownSecrets));
    }


    private static ProvisioningExecutionResult BuildMockResult(VpnNode node, ProvisioningRun run)
    {
        var host = !string.IsNullOrWhiteSpace(node.IpAddress) ? node.IpAddress : node.Host;
        var safeHost = string.IsNullOrWhiteSpace(host) ? "unknown-host" : host;
        if (run.DryRun)
        {
            return new ProvisioningExecutionResult(
                true,
                $"Validation precheck succeeded for {safeHost}. validation/mock mode active: no SSH/Ansible network call was made.",
                new[]
                {
                    new ProvisioningStepResult("Validate input", true, $"Host={safeHost}; Port={node.SshPort}; User={node.SshUser}; credentials=configured"),
                    new ProvisioningStepResult("Check SSH config", true, "Mock SSH config accepted. No socket was opened."),
                    new ProvisioningStepResult("Check OS", true, "Mock OS: Ubuntu/Debian compatible."),
                    new ProvisioningStepResult("Check ports", true, "Mock ports: 22/443/2053 allowed."),
                    new ProvisioningStepResult("Check resources", true, "Mock resources: disk and memory are sufficient.")
                },
                $"mock://provisioning/{run.Id:N}");
        }

        return new ProvisioningExecutionResult(
            true,
            $"Validation deploy succeeded for {safeHost}. Mock mode active: no SSH/Ansible network call was made.",
            new[]
            {
                new ProvisioningStepResult("Prepare mock deployment", true, "Created deterministic validation artifacts."),
                new ProvisioningStepResult("Install 3x-ui", true, "Mock 3x-ui installation completed."),
                new ProvisioningStepResult("Configure panel", true, "Mock 3x-ui panel configured."),
                new ProvisioningStepResult("Create inbound", true, "Mock VLESS inbound created."),
                new ProvisioningStepResult("Finalize node", true, "Mock node ready for VPN access creation.")
            },
            $"mock://provisioning/{run.Id:N}");
    }

    private static string Quote(string value)
        => value.Contains(' ') ? $"\"{value}\"" : value;

    private static void TryDeleteSensitiveFile(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch
        {
            // Best-effort cleanup. The redacted runner logs are still safe for persistence.
        }
    }

    private static string ResolveExistingPath(string value)
    {
        if (Path.IsPathRooted(value) && File.Exists(value))
        {
            return value;
        }

        var candidates = new[]
        {
            Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), value)),
            Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, value)),
            Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", value)),
            Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "..", value))
        };

        var match = candidates.FirstOrDefault(File.Exists);
        if (match is not null)
        {
            return match;
        }

        throw new FileNotFoundException($"Required provisioning asset not found: {value}");
    }

    private sealed class RunnerResponse
    {
        public bool Success { get; set; }
        public string? SummaryLog { get; set; }
        public string? WorkDirectory { get; set; }
        public string? ErrorText { get; set; }
        public List<RunnerStep>? Steps { get; set; }
    }

    private sealed class RunnerStep
    {
        public string? StepName { get; set; }
        public bool Success { get; set; }
        public string? Output { get; set; }
        public string? ErrorText { get; set; }
    }
}
