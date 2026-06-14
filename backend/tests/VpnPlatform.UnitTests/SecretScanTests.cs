using System.Text.RegularExpressions;
using Xunit;

namespace VpnPlatform.UnitTests;

public class SecretScanTests
{
    [Fact]
    public void Secret_Scan_Scripts_Should_Cover_Common_Real_Token_Families()
    {
        var root = FindRepositoryRoot();
        var bashScript = File.ReadAllText(Path.Combine(root, "scripts", "scan-secrets.sh"));
        var powershellScript = File.ReadAllText(Path.Combine(root, "scripts", "scan-secrets.ps1"));
        var docs = File.ReadAllText(Path.Combine(root, "docs", "PRODUCT_COMPLETION_ROADMAP.md"));

        foreach (var script in new[] { bashScript, powershellScript })
        {
            Assert.Contains("Telegram bot token", script, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("Stripe/OpenAI style API key", script, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("GitHub token", script, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("GitLab token", script, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("AWS access key", script, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("Google API key", script, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("Slack token", script, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("Private key PEM", script, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("node_modules", script, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("artifacts", script, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("test-results", script, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("tmp", script, StringComparison.OrdinalIgnoreCase);
            Assert.Contains(".playwright-artifacts-", script, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("local-validation", script, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("must-not-leak", script, StringComparison.OrdinalIgnoreCase);
        }

        Assert.Contains("P6-SEC-006", docs, StringComparison.Ordinal);
    }

    [Fact]
    public void Validation_Entry_Points_Should_Run_Secret_Scan()
    {
        var root = FindRepositoryRoot();
        var validateAll = File.ReadAllText(Path.Combine(root, "scripts", "validate-all.sh"));
        var validateBackend = File.ReadAllText(Path.Combine(root, "scripts", "validate-backend.sh"));
        var validateBackendPowerShell = File.ReadAllText(Path.Combine(root, "scripts", "validate-backend.ps1"));
        var validationSafety = File.ReadAllText(Path.Combine(root, "scripts", "check-validation-safety.sh"));

        Assert.Contains("scan-secrets.sh", validateAll, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("scan-secrets.sh", validateBackend, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("scan-secrets.ps1", validateBackendPowerShell, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("scan-secrets.sh", validationSafety, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Repository_Should_Not_Contain_Known_Live_Secret_Patterns()
    {
        var root = FindRepositoryRoot();
        var findings = new List<string>();
        var excludedSegments = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".git",
            ".serena",
            ".playwright-mcp",
            "node_modules",
            "bin",
            "obj",
            "dist",
            "build",
            "TestResults",
            "test-results",
            "tmp",
            "artifacts",
            "coverage",
            "playwright-report",
            "backups"
        };

        foreach (var file in Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories))
        {
            var relative = Path.GetRelativePath(root, file).Replace('\\', '/');
            var segments = relative.Split('/');
            if (segments.Any(excludedSegments.Contains)
                || segments.Any(segment => segment.StartsWith(".playwright-artifacts-", StringComparison.OrdinalIgnoreCase))
                || !IsTextCandidate(file))
            {
                continue;
            }

            var lines = File.ReadAllLines(file);
            for (var index = 0; index < lines.Length; index++)
            {
                foreach (var pattern in SecretPatterns)
                {
                    if (pattern.Regex.IsMatch(lines[index]) && !IsAllowedFixture(relative, lines[index]))
                    {
                        findings.Add($"{relative}:{index + 1}: {pattern.Name}");
                    }
                }
            }
        }

        Assert.True(findings.Count == 0, "Potential secret leaks were found: " + string.Join("; ", findings));
    }

    private static readonly (string Name, Regex Regex)[] SecretPatterns =
    {
        ("Telegram bot token", new Regex(@"\b\d{8,10}:AA[A-Za-z0-9_-]{30,}\b", RegexOptions.Compiled)),
        ("Stripe/OpenAI style API key", new Regex(@"\b(?:sk|rk|pk)_(?:live|test)_[A-Za-z0-9]{16,}\b|\bsk-(?:proj-|svcacct-)?[A-Za-z0-9_-]{32,}\b", RegexOptions.Compiled)),
        ("GitHub token", new Regex(@"\bgh[pousr]_[A-Za-z0-9_]{30,}\b", RegexOptions.Compiled)),
        ("GitLab token", new Regex(@"\bglpat-[A-Za-z0-9_-]{20,}\b", RegexOptions.Compiled)),
        ("AWS access key", new Regex(@"\bAKIA[0-9A-Z]{16}\b", RegexOptions.Compiled)),
        ("Google API key", new Regex(@"\bAIza[0-9A-Za-z_-]{35}\b", RegexOptions.Compiled)),
        ("Slack token", new Regex(@"\bxox[baprs]-[A-Za-z0-9-]{20,}\b", RegexOptions.Compiled)),
        ("Private key PEM", new Regex(@"-----BEGIN (?:RSA |OPENSSH |EC |DSA )?PRIVATE KEY-----", RegexOptions.Compiled))
    };

    private static bool IsAllowedFixture(string relative, string line)
        => relative.StartsWith("backend/tests/", StringComparison.OrdinalIgnoreCase)
           || relative.StartsWith("frontend/tests/", StringComparison.OrdinalIgnoreCase)
           || relative.Equals("scripts/scan-secrets.ps1", StringComparison.OrdinalIgnoreCase)
           || relative.Equals("scripts/scan-secrets.sh", StringComparison.OrdinalIgnoreCase)
           || Regex.IsMatch(line, "(?i)(placeholder|example|change-me|local-dev|local-validation|schema-audit|ef-drift|dummy|fixture|must-not-leak|redacted)");

    private static bool IsTextCandidate(string file)
    {
        var name = Path.GetFileName(file);
        if (name is ".env.example" or ".gitignore" or "Dockerfile" or "docker-compose.yml" or "docker-compose.validation.yml")
        {
            return true;
        }

        if (name.StartsWith("Dockerfile", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        var extension = Path.GetExtension(file);
        return new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".cs",
            ".csproj",
            ".json",
            ".md",
            ".ps1",
            ".sh",
            ".ts",
            ".tsx",
            ".js",
            ".jsx",
            ".css",
            ".html",
            ".yml",
            ".yaml",
            ".env",
            ".example",
            ".config",
            ".conf",
            ".log",
            ".txt",
            ".sql"
        }.Contains(extension);
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "README.md"))
                && Directory.Exists(Path.Combine(directory.FullName, "backend"))
                && Directory.Exists(Path.Combine(directory.FullName, "scripts")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Repository root was not found.");
    }
}
