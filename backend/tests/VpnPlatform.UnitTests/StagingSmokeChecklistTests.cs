using Xunit;

namespace VpnPlatform.UnitTests;

public class StagingSmokeChecklistTests
{
    [Fact]
    public void Staging_Smoke_Checklist_Should_Define_Required_Report_And_Fail_Closed_Gate()
    {
        var root = FindRepositoryRoot();
        var guide = File.ReadAllText(Path.Combine(root, "docs", "staging-smoke-checklist.md"));
        var template = File.ReadAllText(Path.Combine(root, "docs", "staging-smoke-report.template.json"));
        var script = File.ReadAllText(Path.Combine(root, "scripts", "validate-staging-smoke-report.ps1"));

        foreach (var expected in new[]
                 {
                     "P9-TST-007",
                     "staging-smoke-report.template.json",
                     "validate-staging-smoke-report.ps1",
                     "-RequireAllPassed",
                     "fail-closed",
                     "vps-production-smoke.ps1",
                     "secret-rotation",
                     "no-secret-leak"
                 })
        {
            Assert.Contains(expected, guide, StringComparison.OrdinalIgnoreCase);
        }

        foreach (var requiredCheck in RequiredCheckIds)
        {
            Assert.Contains($"\"id\": \"{requiredCheck}\"", template, StringComparison.OrdinalIgnoreCase);
            Assert.Contains($"\"{requiredCheck}\"", script, StringComparison.OrdinalIgnoreCase);
        }

        Assert.Contains("password=", script, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Bearer ", script, StringComparison.Ordinal);
        Assert.Contains("BEGIN OPENSSH PRIVATE KEY", script, StringComparison.Ordinal);
    }

    [Fact]
    public void Staging_Smoke_Report_Validator_Should_Block_Header_Env_And_Client_Secret_Leaks()
    {
        var root = FindRepositoryRoot();
        var script = File.ReadAllText(Path.Combine(root, "scripts", "validate-staging-smoke-report.ps1"));
        var guide = File.ReadAllText(Path.Combine(root, "docs", "staging-smoke-checklist.md"));
        var productionGate = File.ReadAllText(Path.Combine(root, "docs", "production-readiness-gate.md"));

        foreach (var forbiddenMarker in new[]
                 {
                     "Cookie:",
                     "Set-Cookie:",
                     ".env",
                     "client_secret",
                     "api_key",
                     "private header",
                     "X-Telegram-Bot-Api-Secret-Token",
                     "PRODUCTION_ENV_FILE",
                     "VPS_SSH_KEY"
                 })
        {
            Assert.Contains(forbiddenMarker, script, StringComparison.OrdinalIgnoreCase);
        }

        Assert.Contains("cookies, `.env`, auth headers", productionGate, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("private headers", guide, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Staging_Smoke_Report_Template_Should_Be_Valid_Safe_Json()
    {
        var root = FindRepositoryRoot();
        var templatePath = Path.Combine(root, "docs", "staging-smoke-report.template.json");
        using var document = System.Text.Json.JsonDocument.Parse(File.ReadAllText(templatePath));
        var rootElement = document.RootElement;

        Assert.Equal("staging-smoke-YYYY-MM-DD", rootElement.GetProperty("reportId").GetString());
        Assert.Equal("2026-06-14-staging-smoke-checklist", rootElement.GetProperty("releaseId").GetString());

        var checks = rootElement.GetProperty("checks").EnumerateArray().ToArray();
        Assert.Equal(RequiredCheckIds.Length, checks.Length);
        Assert.All(checks, check =>
        {
            Assert.Equal("blocked", check.GetProperty("status").GetString());
            Assert.False(string.IsNullOrWhiteSpace(check.GetProperty("evidence").GetString()));
        });
    }

    [Fact]
    public void Staging_Smoke_Checklist_Should_Be_Linked_From_Docs_And_Release_Seed()
    {
        var root = FindRepositoryRoot();
        var docsIndex = File.ReadAllText(Path.Combine(root, "docs", "README.md"));
        var changelog = File.ReadAllText(Path.Combine(root, "CHANGELOG.md"));
        var testResults = File.ReadAllText(Path.Combine(root, "TEST_RESULTS.md"));
        var releases = File.ReadAllText(Path.Combine(root, "backend", "src", "VpnPlatform.Api", "AppReleases", "releases.json"));

        Assert.Contains("staging-smoke-checklist.md", docsIndex, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("2026-06-14-staging-smoke-checklist", changelog, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("2026-06-14-staging-smoke-checklist", testResults, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("2026-06-14-staging-smoke-checklist", releases, StringComparison.OrdinalIgnoreCase);
    }

    private static readonly string[] RequiredCheckIds =
    [
        "deploy",
        "health-live",
        "health-ready",
        "public-web",
        "cabinet-web",
        "admin-web",
        "admin-login",
        "tariffs",
        "payment-providers",
        "checkout",
        "payment-init",
        "provider-confirmation",
        "subscription",
        "vpn-access",
        "support",
        "no-console-errors",
        "secret-rotation",
        "no-secret-leak"
    ];

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "README.md"))
                && Directory.Exists(Path.Combine(directory.FullName, "docs"))
                && Directory.Exists(Path.Combine(directory.FullName, "scripts")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Repository root was not found for staging smoke checklist tests.");
    }
}
