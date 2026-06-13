using System.Text.Json;
using System.Text.RegularExpressions;
using Xunit;

namespace VpnPlatform.UnitTests;

public class GitHubSecretsAuditTests
{
    [Fact]
    public void Required_Secrets_Config_Should_Match_Deploy_Workflow_Gate()
    {
        var root = FindRepositoryRoot();
        var workflow = File.ReadAllText(Path.Combine(root, ".github", "workflows", "deploy-vps.yml"));
        using var config = JsonDocument.Parse(File.ReadAllText(Path.Combine(root, ".github", "github-secrets.audit.json")));

        var requiredSecrets = ReadSecretNames(config.RootElement, "requiredSecrets");
        Assert.Equal(new[]
        {
            "PRODUCTION_ENV_FILE",
            "VPS_APP_DIR",
            "VPS_HOST",
            "VPS_PORT",
            "VPS_SSH_KEY",
            "VPS_USER"
        }, requiredSecrets);

        var requiredGate = Regex.Match(
            workflow,
            @"for name in (?<names>[A-Z0-9_ ]+); do",
            RegexOptions.Multiline);
        Assert.True(requiredGate.Success, "deploy-vps.yml should have an explicit required secrets gate.");

        var workflowRequiredSecrets = requiredGate.Groups["names"].Value
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(requiredSecrets, workflowRequiredSecrets);
    }

    [Fact]
    public void Secrets_Audit_Config_Should_Cover_All_Deploy_Workflow_Secret_References()
    {
        var root = FindRepositoryRoot();
        var workflow = File.ReadAllText(Path.Combine(root, ".github", "workflows", "deploy-vps.yml"));
        using var config = JsonDocument.Parse(File.ReadAllText(Path.Combine(root, ".github", "github-secrets.audit.json")));

        var configuredSecrets = ReadSecretNames(config.RootElement, "requiredSecrets")
            .Concat(ReadSecretNames(config.RootElement, "optionalSecrets"))
            .Order(StringComparer.Ordinal)
            .ToArray();

        var workflowSecrets = Regex.Matches(workflow, @"secrets\.([A-Z0-9_]+)")
            .Select(x => x.Groups[1].Value)
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(configuredSecrets, workflowSecrets);
        Assert.Contains("registry", config.RootElement.GetProperty("notRequiredSecrets").GetRawText(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Secrets_Audit_Script_And_Documentation_Should_Not_Expose_Secret_Values()
    {
        var root = FindRepositoryRoot();
        var script = File.ReadAllText(Path.Combine(root, "scripts", "audit-github-secrets.ps1"));
        var documentation = File.ReadAllText(Path.Combine(root, "docs", "github-secrets-audit.md"));

        Assert.Contains("actions/secrets", script, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("DryRun", script, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Values were not requested", script, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("GITHUB_TOKEN", script, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("GH_TOKEN", script, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("missing required", script, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("scripts/audit-github-secrets.ps1", documentation, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("значения secrets", documentation, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("GitHub secrets audit passed", documentation, StringComparison.OrdinalIgnoreCase);

        Assert.DoesNotContain("ghp_", script, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("github_pat_", script, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("-----BEGIN", script, StringComparison.OrdinalIgnoreCase);
    }

    private static string[] ReadSecretNames(JsonElement root, string propertyName)
        => root.GetProperty(propertyName)
            .EnumerateArray()
            .Select(x => x.GetProperty("name").GetString() ?? string.Empty)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Order(StringComparer.Ordinal)
            .ToArray();

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

        throw new InvalidOperationException("Repository root was not found for GitHub secrets audit tests.");
    }
}
