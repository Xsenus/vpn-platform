using System.Diagnostics;
using System.Text;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using VpnPlatform.Application.Abstractions;
using VpnPlatform.Domain.Entities;
using VpnPlatform.Domain.Enums;
using VpnPlatform.Infrastructure.Provisioning;
using Xunit;

namespace VpnPlatform.UnitTests;

public class AnsibleProvisioningExecutorTimeoutTests
{
    [Fact]
    public async Task ExecuteAsync_Should_Keep_Validation_Node_In_Mock_Mode_When_Live_Flags_Are_Enabled()
    {
        var root = Path.Combine(Path.GetTempPath(), $"vpn-platform-validation-runner-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);
        try
        {
            var canaryPath = Path.Combine(root, "runner-started.txt");
            var runnerPath = Path.Combine(root, "runner.py");
            var playbookPath = Path.Combine(root, "provision.yml");
            var escapedCanaryPath = canaryPath.Replace("\\", "\\\\", StringComparison.Ordinal).Replace("'", "\\'", StringComparison.Ordinal);
            await File.WriteAllTextAsync(
                runnerPath,
                $"from pathlib import Path\nPath('{escapedCanaryPath}').write_text('started')\nprint('{{\"success\":true,\"summaryLog\":\"runner started\",\"steps\":[]}}')\n",
                new UTF8Encoding(false));
            await File.WriteAllTextAsync(playbookPath, "---\n", new UTF8Encoding(false));
            var options = new ProvisioningOptions
            {
                LiveExecutionEnabled = true,
                AllowLiveDeploy = true,
                ExecutionTimeoutSeconds = 10,
                WorkingDirectory = Path.Combine(root, "work"),
                PythonBinary = ResolvePythonBinary(),
                RunnerScriptPath = runnerPath,
                ProvisionPlaybookPath = playbookPath,
                PrecheckPlaybookPath = playbookPath
            };
            var executor = new AnsibleProvisioningExecutor(
                Options.Create(options),
                new ProvisioningSecretMaterializer(new TestSecretProtector()),
                NullLogger<AnsibleProvisioningExecutor>.Instance);
            var node = new VpnNode
            {
                Name = "validation-live-flags",
                Host = "validation.example.test",
                SshUser = "root",
                SshPort = 22,
                TagsCsv = "validation-mode:true,ssh-auth:ssh_key"
            };
            var run = new ProvisioningRun
            {
                NodeId = node.Id,
                Status = ProvisioningRunStatus.DeployQueued,
                DryRun = false
            };

            var result = await executor.ExecuteAsync(node, run, CancellationToken.None);

            Assert.True(result.Success, result.ErrorText);
            Assert.Contains("Validation deploy", result.SummaryLog, StringComparison.OrdinalIgnoreCase);
            Assert.False(File.Exists(canaryPath));
            Assert.False(Directory.Exists(options.WorkingDirectory));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task ExecuteAsync_Should_Fail_Closed_For_Live_Deploy_When_Live_Execution_Is_Disabled()
    {
        var executor = new AnsibleProvisioningExecutor(
            Options.Create(new ProvisioningOptions
            {
                LiveExecutionEnabled = false,
                AllowLiveDeploy = true
            }),
            new ProvisioningSecretMaterializer(new TestSecretProtector()),
            NullLogger<AnsibleProvisioningExecutor>.Instance);
        var node = new VpnNode
        {
            Name = "explicit-live-disabled",
            Host = "live.example.test",
            SshUser = "root",
            SshPort = 22,
            TagsCsv = "validation-mode:false,explicit-live-provisioning:true"
        };
        var run = new ProvisioningRun
        {
            NodeId = node.Id,
            Status = ProvisioningRunStatus.DeployQueued,
            DryRun = false
        };

        var result = await executor.ExecuteAsync(node, run, CancellationToken.None);

        Assert.False(result.Success);
        Assert.Contains("LiveExecutionEnabled=true", result.ErrorText, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(result.Steps, step => step.StepName == "Live execution guard" && !step.Success);
    }

    [Fact]
    public async Task ExecuteAsync_Should_Kill_Timed_Out_Runner_And_Delete_ExtraVars()
    {
        var root = Path.Combine(Path.GetTempPath(), $"vpn-platform-runner-timeout-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);
        try
        {
            var runnerPath = Path.Combine(root, "slow_runner.py");
            var playbookPath = Path.Combine(root, "precheck.yml");
            await File.WriteAllTextAsync(runnerPath, "import sys, time\nprint('runner started', flush=True)\nsys.stderr.write('e' * 1048576)\nsys.stderr.flush()\ntime.sleep(30)\n", new UTF8Encoding(false));
            await File.WriteAllTextAsync(playbookPath, "---\n", new UTF8Encoding(false));
            var options = new ProvisioningOptions
            {
                LiveExecutionEnabled = true,
                ExecutionTimeoutSeconds = 1,
                WorkingDirectory = Path.Combine(root, "work"),
                PythonBinary = ResolvePythonBinary(),
                RunnerScriptPath = runnerPath,
                PrecheckPlaybookPath = playbookPath,
                ProvisionPlaybookPath = playbookPath
            };
            var executor = new AnsibleProvisioningExecutor(
                Options.Create(options),
                new ProvisioningSecretMaterializer(new TestSecretProtector()),
                NullLogger<AnsibleProvisioningExecutor>.Instance);
            var node = new VpnNode
            {
                Name = "timeout-test",
                Host = "timeout.example.test",
                SshUser = "root",
                SshPort = 22
            };
            var run = new ProvisioningRun
            {
                NodeId = node.Id,
                Status = ProvisioningRunStatus.PrecheckQueued,
                DryRun = true
            };
            var stopwatch = Stopwatch.StartNew();

            var result = await executor.ExecuteAsync(node, run, CancellationToken.None);

            stopwatch.Stop();
            Assert.False(result.Success);
            Assert.Contains("timed out after 1 seconds", result.ErrorText, StringComparison.OrdinalIgnoreCase);
            var timeoutStep = Assert.Single(result.Steps, step => step.StepName == "runner timeout");
            Assert.True(timeoutStep.ErrorText?.Length <= 4000);
            Assert.True(stopwatch.Elapsed < TimeSpan.FromSeconds(10), $"Runner timeout took {stopwatch.Elapsed}.");
            Assert.False(File.Exists(Path.Combine(options.WorkingDirectory, run.Id.ToString("N"), "extra-vars.json")));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private static string ResolvePythonBinary()
    {
        foreach (var candidate in new[] { "python", "python3" })
        {
            try
            {
                using var process = Process.Start(new ProcessStartInfo
                {
                    FileName = candidate,
                    Arguments = "--version",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                });
                if (process is null)
                {
                    continue;
                }

                process.WaitForExit(5_000);
                if (process.ExitCode == 0)
                {
                    return candidate;
                }
            }
            catch
            {
                // Try the next common Python executable name.
            }
        }

        throw new InvalidOperationException("Python is required for the provisioning runner timeout contract test.");
    }

    private sealed class TestSecretProtector : ISecretProtector
    {
        public string Protect(string plaintext) => plaintext;
        public string Unprotect(string protectedValue) => protectedValue;
        public string Mask(string? value, int visibleTail = 4) => "***";
    }
}
