using Xunit;

namespace VpnPlatform.UnitTests;

public class ProductionReadinessGateTests
{
    [Fact]
    public void Production_Readiness_Gate_Should_Fail_Closed_On_Smoke_Report_And_Open_Roadmap_Blockers()
    {
        var root = FindRepositoryRoot();
        var script = File.ReadAllText(Path.Combine(root, "scripts", "assert-production-readiness.ps1"));
        var docs = File.ReadAllText(Path.Combine(root, "docs", "production-readiness-gate.md"));
        var roadmap = File.ReadAllText(Path.Combine(root, "docs", "PRODUCT_COMPLETION_ROADMAP.md"));
        var releaseDecision = File.ReadAllText(Path.Combine(root, "docs", "release-decision.md"));

        foreach (var expected in new[]
                 {
                     "validate-staging-smoke-report.ps1",
                     "validate-payment-provider-smoke-report.ps1",
                     "validate-admin-vps-smoke-report.ps1",
                     "validate-vpn-live-smoke-report.ps1",
                     "-RequireAllPassed",
                     "Invoke-EvidenceValidator",
                     "evidenceReports",
                     "PaymentProviderReportPath",
                     "AdminVpsReportPath",
                     "VpnLiveReportPath",
                     "Production readiness blocked",
                     "[ ] `STATE-011`",
                     "[ ] `STATE-012`",
                     "[ ] `STATE-013`",
                     "[ ] `P11-ACC-002`",
                     "| BUG-001 | P0 | VPS/Admin |",
                     "| BUG-002 | P0 | VPN |",
                     "| BUG-003 | P0 | Payments |",
                     "staging-ready baseline"
                 })
        {
            Assert.Contains(expected, script, StringComparison.OrdinalIgnoreCase);
        }

        Assert.Contains("P11-ACC-008", docs, StringComparison.Ordinal);
        Assert.Contains("fail-closed", docs, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("scripts\\assert-production-readiness.ps1", docs, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("PaymentProviderReportPath", docs, StringComparison.Ordinal);
        Assert.Contains("AdminVpsReportPath", docs, StringComparison.Ordinal);
        Assert.Contains("VpnLiveReportPath", docs, StringComparison.Ordinal);
        Assert.Contains("[x] `P11-ACC-008`", roadmap, StringComparison.Ordinal);
        Assert.Contains("assert-production-readiness.ps1", releaseDecision, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Production_Readiness_Gate_Should_Require_All_Evidence_Reports()
    {
        var root = FindRepositoryRoot();
        var script = File.ReadAllText(Path.Combine(root, "scripts", "assert-production-readiness.ps1"));
        var docs = File.ReadAllText(Path.Combine(root, "docs", "production-readiness-gate.md"));
        var roadmap = File.ReadAllText(Path.Combine(root, "docs", "PRODUCT_COMPLETION_ROADMAP.md"));

        foreach (var expected in new[]
                 {
                     "docs/payment-provider-smoke-report.template.json",
                     "docs/admin-vps-smoke-report.template.json",
                     "docs/vpn-live-smoke-report.template.json",
                     "Invoke-EvidenceValidator -Name \"payment-providers\"",
                     "Invoke-EvidenceValidator -Name \"admin-vps\"",
                     "Invoke-EvidenceValidator -Name \"vpn-live\"",
                     "paymentProviderReportPath",
                     "adminVpsReportPath",
                     "vpnLiveReportPath"
                 })
        {
            Assert.Contains(expected, script, StringComparison.OrdinalIgnoreCase);
        }

        Assert.Contains("payment provider smoke report", docs, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("admin VPS smoke report", docs, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("VPN live smoke report", docs, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("[x] `P11-ACC-009`", roadmap, StringComparison.Ordinal);
    }

    [Fact]
    public void Production_Readiness_Gate_Should_Aggregate_All_Evidence_Failures()
    {
        var root = FindRepositoryRoot();
        var script = File.ReadAllText(Path.Combine(root, "scripts", "assert-production-readiness.ps1"));
        var docs = File.ReadAllText(Path.Combine(root, "docs", "production-readiness-gate.md"));
        var roadmap = File.ReadAllText(Path.Combine(root, "docs", "PRODUCT_COMPLETION_ROADMAP.md"));

        foreach (var expected in new[]
                 {
                     "failedEvidenceReports",
                     "$failedEvidenceReports.Count -gt 0 -or $foundBlockers.Count -gt 0",
                     "validatorPath",
                     "message = $_.Exception.Message",
                     "ConvertTo-Json -Depth 8 -Compress",
                     "staging-vps",
                     "payment-providers",
                     "admin-vps",
                     "vpn-live"
                 })
        {
            Assert.Contains(expected, script, StringComparison.OrdinalIgnoreCase);
        }

        Assert.Contains("evidenceReports", docs, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("агрег", docs, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("[x] `P11-ACC-010`", roadmap, StringComparison.Ordinal);
    }

    [Fact]
    public void Production_Evidence_Bundle_Generator_Should_Create_All_Report_Drafts()
    {
        var root = FindRepositoryRoot();
        var script = File.ReadAllText(Path.Combine(root, "scripts", "new-production-evidence-bundle.ps1"));
        var docs = File.ReadAllText(Path.Combine(root, "docs", "production-readiness-gate.md"));
        var roadmap = File.ReadAllText(Path.Combine(root, "docs", "PRODUCT_COMPLETION_ROADMAP.md"));

        foreach (var expected in new[]
                 {
                     "new-staging-smoke-report.ps1",
                     "new-payment-provider-smoke-report.ps1",
                     "new-admin-vps-smoke-report.ps1",
                     "new-vpn-live-smoke-report.ps1",
                     "staging-smoke-report.json",
                     "payment-provider-smoke-report.json",
                     "admin-vps-smoke-report.json",
                     "vpn-live-smoke-report.json",
                     "RunProductionGate",
                     "productionGateStatus",
                     "assert-production-readiness.ps1"
                 })
        {
            Assert.Contains(expected, script, StringComparison.OrdinalIgnoreCase);
        }

        Assert.Contains("new-production-evidence-bundle.ps1", docs, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("[x] `P11-ACC-011`", roadmap, StringComparison.Ordinal);
    }

    [Fact]
    public void Production_Readiness_Summary_Should_Expose_Human_Readable_Blockers()
    {
        var root = FindRepositoryRoot();
        var script = File.ReadAllText(Path.Combine(root, "scripts", "new-production-readiness-summary.ps1"));
        var docs = File.ReadAllText(Path.Combine(root, "docs", "production-readiness-gate.md"));
        var roadmap = File.ReadAllText(Path.Combine(root, "docs", "PRODUCT_COMPLETION_ROADMAP.md"));

        foreach (var expected in new[]
                 {
                     "Production readiness summary",
                     "Payment providers",
                     "Roadmap blockers",
                     "staging-vps",
                     "payment-providers",
                     "admin-vps",
                     "vpn-live",
                     "reportPaths",
                     "roadmapBlockers",
                     "production readiness summary generated"
                 })
        {
            Assert.Contains(expected, script, StringComparison.OrdinalIgnoreCase);
        }

        Assert.Contains("new-production-readiness-summary.ps1", docs, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("[x] `P11-ACC-012`", roadmap, StringComparison.Ordinal);
    }

    [Fact]
    public void Production_Readiness_Gate_Should_Be_Documented_In_Index_Changelog_Test_Results_And_Release_Seed()
    {
        var root = FindRepositoryRoot();
        var docsIndex = File.ReadAllText(Path.Combine(root, "docs", "README.md"));
        var changelog = File.ReadAllText(Path.Combine(root, "CHANGELOG.md"));
        var testResults = File.ReadAllText(Path.Combine(root, "TEST_RESULTS.md"));
        var releases = File.ReadAllText(Path.Combine(root, "backend", "src", "VpnPlatform.Api", "AppReleases", "releases.json"));

        Assert.Contains("production-readiness-gate.md", docsIndex, StringComparison.OrdinalIgnoreCase);

        foreach (var document in new[] { changelog, testResults, releases })
        {
            Assert.Contains("2026-06-14-production-readiness-gate", document, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("production-readiness-gate", document, StringComparison.OrdinalIgnoreCase);
        }

        Assert.Contains("ProductionReadinessGateTests", changelog, StringComparison.Ordinal);
        Assert.Contains("ProductionReadinessGateTests", testResults, StringComparison.Ordinal);
        Assert.Contains("0.112.0", releases, StringComparison.Ordinal);
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "README.md"))
                && File.Exists(Path.Combine(directory.FullName, "CHANGELOG.md"))
                && Directory.Exists(Path.Combine(directory.FullName, "scripts")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Repository root was not found for production readiness gate tests.");
    }
}
