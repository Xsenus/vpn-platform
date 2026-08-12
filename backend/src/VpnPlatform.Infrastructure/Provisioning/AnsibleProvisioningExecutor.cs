using System.Diagnostics;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using VpnPlatform.Infrastructure.Security;
using VpnPlatform.Application.Abstractions;
using VpnPlatform.Application.DTOs;
using VpnPlatform.Application.Services;
using VpnPlatform.Domain.Entities;

namespace VpnPlatform.Infrastructure.Provisioning;

public sealed class AnsibleProvisioningExecutor : IProvisioningExecutor
{
    internal const string PrecheckReportStepName = "Precheck report";

    private readonly ProvisioningOptions _options;
    private readonly ProvisioningSecretMaterializer _secretMaterializer;
    private readonly ILogger<AnsibleProvisioningExecutor> _logger;

    public AnsibleProvisioningExecutor(IOptions<ProvisioningOptions> options, ProvisioningSecretMaterializer secretMaterializer, ILogger<AnsibleProvisioningExecutor> logger)
    {
        _options = options.Value;
        _secretMaterializer = secretMaterializer;
        _logger = logger;
    }

    public async Task<ProvisioningExecutionResult> ExecuteAsync(VpnNode node, ProvisioningRun run, CancellationToken cancellationToken)
    {
        var targetError = ProvisioningService.ValidateProvisioningTarget(node);
        if (targetError is not null)
        {
            var error = $"Provisioning target validation failed: {targetError}";
            return new ProvisioningExecutionResult(
                false,
                error,
                new[] { new ProvisioningStepResult("Provisioning target validation", false, "No process was started.", targetError) },
                null,
                error);
        }

        var credentialError = ProvisioningService.ValidateProvisioningSshCredential(node);
        if (credentialError is not null)
        {
            var error = $"Provisioning SSH credential validation failed: {credentialError}";
            return new ProvisioningExecutionResult(
                false,
                error,
                new[] { new ProvisioningStepResult("SSH credential validation", false, "No process was started.", credentialError) },
                null,
                error);
        }

        if (ProvisioningService.IsValidationNode(node))
        {
            return await Task.FromResult(BuildMockResult(node, run));
        }

        if (!_options.LiveExecutionEnabled)
        {
            if (run.DryRun)
            {
                return await Task.FromResult(BuildMockResult(node, run));
            }

            return new ProvisioningExecutionResult(
                false,
                "Live provisioning execution is disabled. Set Provisioning:LiveExecutionEnabled=true only for an approved staging/live target.",
                new[]
                {
                    new ProvisioningStepResult(
                        "Live execution guard",
                        false,
                        "LiveExecutionEnabled=false. No SSH/Ansible process was started.",
                        "Live deploy requires explicit Provisioning:LiveExecutionEnabled=true.")
                },
                null,
                "Live deploy requires Provisioning:LiveExecutionEnabled=true.");
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
            runnerScript,
            "--playbook", playbookPath,
            "--host", !string.IsNullOrWhiteSpace(node.IpAddress) ? node.IpAddress.Trim() : node.Host.Trim(),
            "--ssh-user", node.SshUser.Trim(),
            "--ssh-port", node.SshPort.ToString(),
            "--workdir", workDirectory,
            "--ansible-binary", _options.AnsibleBinary
        };

        if (!string.IsNullOrWhiteSpace(node.Host))
        {
            arguments.Add("--inventory-name");
            arguments.Add($"vpn-node-{node.Id:N}");
        }

        MaterializedProvisioningSecret? materializedSshKey = null;
        string? extraVarsPath = null;
        try
        {
            try
            {
                materializedSshKey = await _secretMaterializer.MaterializeSshPrivateKeyAsync(node, workDirectory, cancellationToken);
            }
            catch (InvalidOperationException ex)
            {
                return new ProvisioningExecutionResult(
                    false,
                    "Protected SSH credential materialization failed. No raw credential was exposed to ansible-playbook.",
                    new[] { new ProvisioningStepResult("SSH credential materialization", false, "Protected credential was not materialized.", ex.Message) },
                    workDirectory,
                    ex.Message);
            }

            if (materializedSshKey is not null)
            {
                arguments.Add("--private-key-path");
                arguments.Add(materializedSshKey.Path);
            }

            if (!string.IsNullOrWhiteSpace(node.SshPrivateKeyPath))
            {
                if (node.SshPrivateKeyPath.StartsWith("v1:", StringComparison.Ordinal) || node.SshPrivateKeyPath.StartsWith("validation-placeholder:", StringComparison.Ordinal))
                {
                    return new ProvisioningExecutionResult(
                        false,
                        "Protected SSH credentials are configured in the legacy key-path field and were not exposed to ansible-playbook.",
                        new[] { new ProvisioningStepResult("SSH credential guard", false, "Legacy protected credentials were detected and not exposed to ansible-playbook.", "Move the credential into ProtectedSshCredential before live provisioning.") },
                        workDirectory,
                        "Legacy protected SSH credentials cannot be used for live Ansible.");
                }

                if (materializedSshKey is null)
                {
                    arguments.Add("--private-key-path");
                    arguments.Add(node.SshPrivateKeyPath);
                }
            }

            if (!string.IsNullOrWhiteSpace(_options.KnownHostsPath))
            {
                arguments.Add("--known-hosts-path");
                arguments.Add(_options.KnownHostsPath);
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

            extraVarsPath = Path.Combine(workDirectory, "extra-vars.json");
            await File.WriteAllTextAsync(extraVarsPath, JsonSerializer.Serialize(extraVars, new JsonSerializerOptions { WriteIndented = true }), cancellationToken);
            arguments.Add("--extra-vars-file");
            arguments.Add(extraVarsPath);

            var psi = new ProcessStartInfo
            {
                FileName = _options.PythonBinary,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = workDirectory
            };
            foreach (var argument in arguments)
            {
                psi.ArgumentList.Add(argument);
            }

            _logger.LogInformation("Starting provisioning run {RunId} for node {NodeId} with playbook {PlaybookPath}", run.Id, node.Id, playbookPath);

            using var process = Process.Start(psi) ?? throw new InvalidOperationException("Unable to start provisioning runner process.");
            var stdoutTask = process.StandardOutput.ReadToEndAsync();
            var stderrTask = process.StandardError.ReadToEndAsync();
            var timeoutSeconds = Math.Clamp(_options.ExecutionTimeoutSeconds, 1, 86_400);
            using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));
            using var executionCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

            try
            {
                await process.WaitForExitAsync(executionCts.Token);
            }
            catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
            {
                var stopped = await TryStopProcessTreeAsync(process);
                var timeoutSecrets = BuildKnownSecretsForRedaction(node, materializedSshKey);
                var timedOutStdout = stopped ? RedactRunnerOutput(await stdoutTask, timeoutSecrets) : string.Empty;
                var timedOutStderr = stopped
                    ? RedactRunnerOutput(await stderrTask, timeoutSecrets)
                    : "Runner process did not exit after termination requests.";
                var timeoutError = $"Provisioning runner timed out after {timeoutSeconds} seconds.";
                return new ProvisioningExecutionResult(
                    false,
                    timeoutError,
                    new[] { new ProvisioningStepResult("runner timeout", false, timedOutStdout, timedOutStderr) },
                    workDirectory,
                    timeoutError);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                if (await TryStopProcessTreeAsync(process))
                {
                    await Task.WhenAll(stdoutTask, stderrTask);
                }
                throw;
            }

            var stdout = await stdoutTask;
            var stderr = await stderrTask;

            var knownSecrets = BuildKnownSecretsForRedaction(node, materializedSshKey);
            var redactedStdout = RedactRunnerOutput(stdout, knownSecrets);
            var redactedStderr = RedactRunnerOutput(stderr, knownSecrets);

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
                    RedactRunnerOutput(x.Output, knownSecrets),
                    RedactRunnerOutput(x.ErrorText, knownSecrets)))
                .ToArray()
                ?? Array.Empty<ProvisioningStepResult>();

            var success = response.Success && process.ExitCode == 0;
            var summaryLog = RedactRunnerOutput(response.SummaryLog, knownSecrets);
            var errorText = RedactRunnerOutput(response.ErrorText ?? stderr, knownSecrets);

            return run.DryRun
                ? BuildPrecheckExecutionResult(node, run, success, summaryLog, steps, response.WorkDirectory ?? workDirectory, errorText)
                : new ProvisioningExecutionResult(
                    success,
                    summaryLog,
                    steps,
                    response.WorkDirectory ?? workDirectory,
                    errorText);
        }
        finally
        {
            if (extraVarsPath is not null)
            {
                TryDeleteSensitiveFile(extraVarsPath);
            }

            materializedSshKey?.Dispose();
        }
    }

    internal static IReadOnlyCollection<string?> BuildKnownSecretsForRedaction(VpnNode node, MaterializedProvisioningSecret? materializedSshKey)
    {
        var secrets = new List<string?>
        {
            node.PanelPassword,
            node.ProtectedPanelPassword,
            node.SshPrivateKeyPath,
            node.ProtectedSshCredential,
            materializedSshKey?.Plaintext,
            materializedSshKey?.Path
        };

        var materializedDirectory = materializedSshKey?.Path is null ? null : Path.GetDirectoryName(materializedSshKey.Path);
        if (!string.IsNullOrWhiteSpace(materializedDirectory))
        {
            secrets.Add(materializedDirectory);
        }

        return secrets;
    }

    private static string RedactRunnerOutput(string? value, IReadOnlyCollection<string?> knownSecrets)
    {
        var redacted = SecretRedactor.Redact(value, knownSecrets);
        return redacted.Length <= 4000 ? redacted : redacted[..4000];
    }


    private static ProvisioningExecutionResult BuildMockResult(VpnNode node, ProvisioningRun run)
    {
        var host = !string.IsNullOrWhiteSpace(node.IpAddress) ? node.IpAddress : node.Host;
        var safeHost = string.IsNullOrWhiteSpace(host) ? "unknown-host" : host;
        if (run.DryRun)
        {
            var steps = new[]
            {
                new ProvisioningStepResult("Validate input", true, $"Host={safeHost}; Port={node.SshPort}; User={node.SshUser}; credentials=configured"),
                new ProvisioningStepResult("Check SSH config", true, "Mock SSH config accepted. No socket was opened."),
                new ProvisioningStepResult("Check OS", true, "Mock OS: Ubuntu/Debian compatible."),
                new ProvisioningStepResult("Check ports", true, "Mock ports: 22/443/2053 available for SSH, HTTPS and panel traffic."),
                new ProvisioningStepResult("Check disk", true, "Mock disk: root filesystem has more than 1 GiB free."),
                new ProvisioningStepResult("Check RAM", true, "Mock RAM: node has at least 512 MiB memory."),
                new ProvisioningStepResult("Check firewall", true, "Mock firewall: UFW/firewall state can be inspected and required rules can be applied."),
                new ProvisioningStepResult("Check Docker", true, "Mock Docker: optional Docker runtime check completed; provisioning can continue without container mode."),
                new ProvisioningStepResult("Check systemd", true, "Mock systemd: service manager is available."),
                new ProvisioningStepResult("Check 3x-ui availability", true, "Mock 3x-ui: panel binary can be installed or reused during deploy.")
            };

            return BuildPrecheckExecutionResult(
                node,
                run,
                true,
                $"Validation precheck succeeded for {safeHost}. validation/mock mode active: no SSH/Ansible network call was made.",
                steps,
                $"mock://provisioning/{run.Id:N}",
                null);
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

    private static async Task<bool> TryStopProcessTreeAsync(Process process)
    {
        try
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
            }
        }
        catch
        {
            try
            {
                if (!process.HasExited)
                {
                    process.Kill();
                }
            }
            catch
            {
                return false;
            }
        }

        try
        {
            using var waitCts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            await process.WaitForExitAsync(waitCts.Token);
            return true;
        }
        catch (OperationCanceledException)
        {
            return process.HasExited;
        }
    }

    private static ProvisioningExecutionResult BuildPrecheckExecutionResult(
        VpnNode node,
        ProvisioningRun run,
        bool success,
        string? summaryLog,
        IReadOnlyCollection<ProvisioningStepResult> steps,
        string? workDirectory,
        string? errorText)
    {
        var report = BuildPrecheckReport(node, run, success, steps, errorText);
        var enrichedSteps = steps
            .Where(x => !string.Equals(x.StepName, PrecheckReportStepName, StringComparison.OrdinalIgnoreCase))
            .Append(new ProvisioningStepResult(PrecheckReportStepName, success, report, success ? null : errorText))
            .ToArray();

        var safeSummary = string.IsNullOrWhiteSpace(summaryLog)
            ? (success ? "Precheck succeeded." : "Precheck failed.")
            : summaryLog.Trim();

        return new ProvisioningExecutionResult(
            success,
            $"{safeSummary}{Environment.NewLine}{Environment.NewLine}{PrecheckReportStepName}:{Environment.NewLine}{report}",
            enrichedSteps,
            workDirectory,
            errorText);
    }

    private static string BuildPrecheckReport(VpnNode node, ProvisioningRun run, bool success, IReadOnlyCollection<ProvisioningStepResult> steps, string? errorText)
    {
        var host = !string.IsNullOrWhiteSpace(node.IpAddress) ? node.IpAddress : node.Host;
        var report = new PrecheckReport(
            run.Id,
            node.Id,
            string.IsNullOrWhiteSpace(host) ? "unknown-host" : host,
            node.SshPort,
            string.IsNullOrWhiteSpace(node.SshUser) ? "root" : node.SshUser,
            success ? "passed" : "failed",
            success ? "Server precheck passed." : "Server precheck failed. Review failed/not_reported checks and runner log.",
            new[]
            {
                ResolvePrecheckItem("ssh", "SSH connectivity", steps, new[] { "ssh", "ping", "inventory", "ansible-playbook" }, success),
                ResolvePrecheckItem("os", "Operating system", steps, new[] { "os", "debian", "ubuntu", "distribution" }, success),
                ResolvePrecheckItem("ports", "Required ports", steps, new[] { "ports", "443", "2053", "listening" }, success),
                ResolvePrecheckItem("disk", "Disk space", steps, new[] { "disk", "root filesystem", "root free", "free bytes" }, success),
                ResolvePrecheckItem("ram", "RAM", steps, new[] { "ram", "memory", "memory mb" }, success),
                ResolvePrecheckItem("firewall", "Firewall", steps, new[] { "firewall", "ufw" }, success),
                ResolvePrecheckItem("docker", "Docker", steps, new[] { "docker", "container" }, true),
                ResolvePrecheckItem("systemd", "systemd", steps, new[] { "systemd", "systemctl" }, success),
                ResolvePrecheckItem("x3ui", "3x-ui availability", steps, new[] { "3x-ui", "x-ui", "x3ui" }, true)
            },
            string.IsNullOrWhiteSpace(errorText) ? null : TrimForReport(errorText, 1000));

        return JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
    }

    private static PrecheckItem ResolvePrecheckItem(string key, string label, IReadOnlyCollection<ProvisioningStepResult> steps, IReadOnlyCollection<string> markers, bool fallbackSuccess)
    {
        var matched = steps.FirstOrDefault(step =>
            markers.Any(marker => ContainsMarker(step.StepName, marker)
                || ContainsMarker(step.Output, marker)
                || ContainsMarker(step.ErrorText, marker)));

        if (matched is null)
        {
            return new PrecheckItem(
                key,
                label,
                fallbackSuccess ? "passed" : "not_reported",
                fallbackSuccess
                    ? "Runner completed successfully but did not return a dedicated per-check output."
                    : "Runner did not return a dedicated per-check output; inspect ansible-playbook log.",
                fallbackSuccess ? null : "Open the provisioning run details and review the runner output.");
        }

        var evidence = !string.IsNullOrWhiteSpace(matched.Output) ? matched.Output : matched.ErrorText;
        return new PrecheckItem(
            key,
            label,
            matched.Success ? "passed" : "failed",
            TrimForReport(evidence, 1000),
            matched.Success ? null : "Fix the server environment and run precheck again.");
    }

    private static bool ContainsMarker(string? value, string marker)
        => !string.IsNullOrWhiteSpace(value) && value.Contains(marker, StringComparison.OrdinalIgnoreCase);

    private static string TrimForReport(string? value, int maxLength)
    {
        var text = string.IsNullOrWhiteSpace(value) ? "No output." : value.Trim();
        return text.Length <= maxLength ? text : text[..maxLength];
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

    private sealed record PrecheckReport(
        Guid RunId,
        Guid NodeId,
        string Host,
        int SshPort,
        string SshUser,
        string Status,
        string Summary,
        IReadOnlyCollection<PrecheckItem> Checks,
        string? ErrorText);

    private sealed record PrecheckItem(
        string Key,
        string Label,
        string Status,
        string Evidence,
        string? RequiredAction);
}
