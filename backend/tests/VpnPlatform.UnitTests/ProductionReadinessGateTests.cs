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
    public void Production_Readiness_Summary_Validator_Should_Check_Markdown_Json_And_Production_Mode()
    {
        var root = FindRepositoryRoot();
        var script = File.ReadAllText(Path.Combine(root, "scripts", "validate-production-readiness-summary.ps1"));
        var docs = File.ReadAllText(Path.Combine(root, "docs", "production-readiness-gate.md"));
        var roadmap = File.ReadAllText(Path.Combine(root, "docs", "PRODUCT_COMPLETION_ROADMAP.md"));

        foreach (var expected in new[]
                 {
                     "SummaryPath",
                     "JsonSummaryPath",
                     "RequireProductionReady",
                     "RequireReportFiles",
                     "Production readiness summary valid",
                     "staging-vps",
                     "payment-providers",
                     "admin-vps",
                     "vpn-live",
                     "roadmapBlockers",
                     "reportPaths",
                     "forbidden secret marker"
                 })
        {
            Assert.Contains(expected, script, StringComparison.OrdinalIgnoreCase);
        }

        Assert.Contains("validate-production-readiness-summary.ps1", docs, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("[x] `P11-ACC-013`", roadmap, StringComparison.Ordinal);
    }

    [Fact]
    public void Production_Evidence_Bundle_Validator_Should_Check_All_Reports_And_Summary()
    {
        var root = FindRepositoryRoot();
        var script = File.ReadAllText(Path.Combine(root, "scripts", "validate-production-evidence-bundle.ps1"));
        var docs = File.ReadAllText(Path.Combine(root, "docs", "production-readiness-gate.md"));
        var roadmap = File.ReadAllText(Path.Combine(root, "docs", "PRODUCT_COMPLETION_ROADMAP.md"));

        foreach (var expected in new[]
                 {
                     "BundleDirectory",
                     "RequireSummary",
                     "RequireProductionReady",
                     "staging-smoke-report.json",
                     "payment-provider-smoke-report.json",
                     "admin-vps-smoke-report.json",
                     "vpn-live-smoke-report.json",
                     "production-readiness-summary.md",
                     "validate-staging-smoke-report.ps1",
                     "validate-payment-provider-smoke-report.ps1",
                     "validate-admin-vps-smoke-report.ps1",
                     "validate-vpn-live-smoke-report.ps1",
                     "validate-production-readiness-summary.ps1",
                     "production evidence bundle valid"
                 })
        {
            Assert.Contains(expected, script, StringComparison.OrdinalIgnoreCase);
        }

        Assert.Contains("validate-production-evidence-bundle.ps1", docs, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("[x] `P11-ACC-014`", roadmap, StringComparison.Ordinal);
    }

    [Fact]
    public void Production_Evidence_Manifest_Should_Record_Safe_File_Hashes_For_Handoff()
    {
        var root = FindRepositoryRoot();
        var script = File.ReadAllText(Path.Combine(root, "scripts", "new-production-evidence-manifest.ps1"));
        var docs = File.ReadAllText(Path.Combine(root, "docs", "production-readiness-gate.md"));
        var roadmap = File.ReadAllText(Path.Combine(root, "docs", "PRODUCT_COMPLETION_ROADMAP.md"));

        foreach (var expected in new[]
                 {
                     "BundleDirectory",
                     "production-evidence-manifest.json",
                     "validate-production-evidence-bundle.ps1",
                     "Get-FileSha256",
                     "sha256",
                     "relativePath",
                     "lengthBytes",
                     "totalBytes",
                     "releaseId",
                     "production evidence manifest generated"
                 })
        {
            Assert.Contains(expected, script, StringComparison.OrdinalIgnoreCase);
        }

        Assert.Contains("new-production-evidence-manifest.ps1", docs, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("[x] `P11-ACC-015`", roadmap, StringComparison.Ordinal);
    }

    [Fact]
    public void Production_Evidence_Manifest_Validator_Should_Recalculate_Hashes()
    {
        var root = FindRepositoryRoot();
        var script = File.ReadAllText(Path.Combine(root, "scripts", "validate-production-evidence-manifest.ps1"));
        var docs = File.ReadAllText(Path.Combine(root, "docs", "production-readiness-gate.md"));
        var roadmap = File.ReadAllText(Path.Combine(root, "docs", "PRODUCT_COMPLETION_ROADMAP.md"));

        foreach (var expected in new[]
                 {
                     "ManifestPath",
                     "RequireAllFiles",
                     "Get-FileSha256",
                     "sha256 mismatch",
                     "length mismatch",
                     "totalBytes",
                     "verifiedFiles",
                     "production evidence manifest valid",
                     "staging-smoke-report.json",
                     "production-readiness-summary.json"
                 })
        {
            Assert.Contains(expected, script, StringComparison.OrdinalIgnoreCase);
        }

        Assert.Contains("validate-production-evidence-manifest.ps1", docs, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("[x] `P11-ACC-016`", roadmap, StringComparison.Ordinal);
    }

    [Fact]
    public void Production_Evidence_Archive_Should_Be_Built_From_Validated_Manifest()
    {
        var root = FindRepositoryRoot();
        var script = File.ReadAllText(Path.Combine(root, "scripts", "new-production-evidence-archive.ps1"));
        var docs = File.ReadAllText(Path.Combine(root, "docs", "production-readiness-gate.md"));
        var roadmap = File.ReadAllText(Path.Combine(root, "docs", "PRODUCT_COMPLETION_ROADMAP.md"));

        foreach (var expected in new[]
                 {
                     "ManifestPath",
                     "RequireAllFiles",
                     "validate-production-evidence-manifest.ps1",
                     "ZipArchive",
                     "Get-FileSha256",
                     "archiveSha256",
                     "manifestSha256",
                     "must stay inside bundle directory",
                     "production evidence archive created"
                 })
        {
            Assert.Contains(expected, script, StringComparison.OrdinalIgnoreCase);
        }

        Assert.Contains("new-production-evidence-archive.ps1", docs, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("[x] `P11-ACC-017`", roadmap, StringComparison.Ordinal);
    }

    [Fact]
    public void Production_Evidence_Archive_Validator_Should_Verify_Zip_Entries()
    {
        var root = FindRepositoryRoot();
        var script = File.ReadAllText(Path.Combine(root, "scripts", "validate-production-evidence-archive.ps1"));
        var docs = File.ReadAllText(Path.Combine(root, "docs", "production-readiness-gate.md"));
        var roadmap = File.ReadAllText(Path.Combine(root, "docs", "PRODUCT_COMPLETION_ROADMAP.md"));

        foreach (var expected in new[]
                 {
                     "ArchivePath",
                     "ExpectedArchiveSha256",
                     "RequireAllFiles",
                     "production-evidence-manifest.json",
                     "ZipArchive",
                     "Get-StreamSha256",
                     "unexpected entry",
                     "sha256 mismatch",
                     "length mismatch",
                     "production evidence archive valid"
                 })
        {
            Assert.Contains(expected, script, StringComparison.OrdinalIgnoreCase);
        }

        Assert.Contains("validate-production-evidence-archive.ps1", docs, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("[x] `P11-ACC-018`", roadmap, StringComparison.Ordinal);
    }

    [Fact]
    public void Production_Evidence_Handoff_Receipt_Should_Record_Archive_Hashes()
    {
        var root = FindRepositoryRoot();
        var script = File.ReadAllText(Path.Combine(root, "scripts", "new-production-evidence-handoff-receipt.ps1"));
        var docs = File.ReadAllText(Path.Combine(root, "docs", "production-readiness-gate.md"));
        var roadmap = File.ReadAllText(Path.Combine(root, "docs", "PRODUCT_COMPLETION_ROADMAP.md"));

        foreach (var expected in new[]
                 {
                     "ArchivePath",
                     "ExpectedArchiveSha256",
                     "validate-production-evidence-archive.ps1",
                     "production-evidence-handoff-receipt.json",
                     "receiptMarkdownPath",
                     "ready-for-handoff",
                     "archiveSha256",
                     "manifestSha256",
                     "verifiedFiles",
                     "production evidence handoff receipt created"
                 })
        {
            Assert.Contains(expected, script, StringComparison.OrdinalIgnoreCase);
        }

        Assert.Contains("new-production-evidence-handoff-receipt.ps1", docs, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("[x] `P11-ACC-019`", roadmap, StringComparison.Ordinal);
    }

    [Fact]
    public void Production_Evidence_Handoff_Receipt_Validator_Should_Verify_Receipt_Against_Archive()
    {
        var root = FindRepositoryRoot();
        var script = File.ReadAllText(Path.Combine(root, "scripts", "validate-production-evidence-handoff-receipt.ps1"));
        var docs = File.ReadAllText(Path.Combine(root, "docs", "production-readiness-gate.md"));
        var roadmap = File.ReadAllText(Path.Combine(root, "docs", "PRODUCT_COMPLETION_ROADMAP.md"));

        foreach (var expected in new[]
                 {
                     "ReceiptPath",
                     "ArchivePath",
                     "ExpectedArchiveSha256",
                     "validate-production-evidence-archive.ps1",
                     "ready-for-handoff",
                     "archiveSha256 does not match archive",
                     "manifestSha256 does not match archive",
                     "receipt markdown",
                     "production evidence handoff receipt valid"
                 })
        {
            Assert.Contains(expected, script, StringComparison.OrdinalIgnoreCase);
        }

        Assert.Contains("validate-production-evidence-handoff-receipt.ps1", docs, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("[x] `P11-ACC-020`", roadmap, StringComparison.Ordinal);
    }

    [Fact]
    public void Production_Evidence_Handoff_Checklist_Should_Gate_Operator_Handoff()
    {
        var root = FindRepositoryRoot();
        var script = File.ReadAllText(Path.Combine(root, "scripts", "new-production-evidence-handoff-checklist.ps1"));
        var docs = File.ReadAllText(Path.Combine(root, "docs", "production-readiness-gate.md"));
        var roadmap = File.ReadAllText(Path.Combine(root, "docs", "PRODUCT_COMPLETION_ROADMAP.md"));

        foreach (var expected in new[]
                 {
                     "ReceiptPath",
                     "SummaryJsonPath",
                     "RequireProductionReady",
                     "validate-production-evidence-handoff-receipt.ps1",
                     "production-evidence-handoff-checklist.json",
                     "Production evidence handoff checklist",
                     "production-ready-handoff",
                     "blocked",
                     "operatorActions",
                     "Do not attach .env files, cookies, private headers, provider secrets or API keys",
                     "production evidence handoff checklist created"
                 })
        {
            Assert.Contains(expected, script, StringComparison.OrdinalIgnoreCase);
        }

        Assert.Contains("new-production-evidence-handoff-checklist.ps1", docs, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("[x] `P11-ACC-021`", roadmap, StringComparison.Ordinal);
    }

    [Fact]
    public void Production_Evidence_Handoff_Checklist_Validator_Should_Verify_Checklist_Against_Receipt()
    {
        var root = FindRepositoryRoot();
        var script = File.ReadAllText(Path.Combine(root, "scripts", "validate-production-evidence-handoff-checklist.ps1"));
        var docs = File.ReadAllText(Path.Combine(root, "docs", "production-readiness-gate.md"));
        var roadmap = File.ReadAllText(Path.Combine(root, "docs", "PRODUCT_COMPLETION_ROADMAP.md"));

        foreach (var expected in new[]
                 {
                     "ChecklistPath",
                     "ReceiptPath",
                     "ExpectedArchiveSha256",
                     "RequireProductionReady",
                     "validate-production-evidence-handoff-receipt.ps1",
                     "production-ready-handoff",
                     "Production evidence handoff checklist markdown",
                     "operatorActions",
                     "forbidden secret marker",
                     "archiveSha256 does not match receipt",
                     "production evidence handoff checklist valid"
                 })
        {
            Assert.Contains(expected, script, StringComparison.OrdinalIgnoreCase);
        }

        Assert.Contains("validate-production-evidence-handoff-checklist.ps1", docs, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("[x] `P11-ACC-022`", roadmap, StringComparison.Ordinal);
    }

    [Fact]
    public void Production_Evidence_Handoff_Package_Should_Copy_Only_Verified_Artifacts()
    {
        var root = FindRepositoryRoot();
        var script = File.ReadAllText(Path.Combine(root, "scripts", "new-production-evidence-handoff-package.ps1"));
        var docs = File.ReadAllText(Path.Combine(root, "docs", "production-readiness-gate.md"));
        var roadmap = File.ReadAllText(Path.Combine(root, "docs", "PRODUCT_COMPLETION_ROADMAP.md"));

        foreach (var expected in new[]
                 {
                     "ChecklistPath",
                     "OutputDirectory",
                     "ExpectedArchiveSha256",
                     "validate-production-evidence-handoff-checklist.ps1",
                     "production-evidence-handoff-package",
                     "production-evidence-handoff-package-index.json",
                     "production-evidence-handoff-package-index.md",
                     "SHA256SUMS.txt",
                     "Package contains only archive, receipt, checklist and hash indexes",
                     "Do not add .env files, cookies, private headers, provider secrets or API keys",
                     "production evidence handoff package created"
                 })
        {
            Assert.Contains(expected, script, StringComparison.OrdinalIgnoreCase);
        }

        Assert.Contains("new-production-evidence-handoff-package.ps1", docs, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("[x] `P11-ACC-023`", roadmap, StringComparison.Ordinal);
    }

    [Fact]
    public void Production_Evidence_Handoff_Package_Validator_Should_Verify_Index_And_Checksums()
    {
        var root = FindRepositoryRoot();
        var script = File.ReadAllText(Path.Combine(root, "scripts", "validate-production-evidence-handoff-package.ps1"));
        var docs = File.ReadAllText(Path.Combine(root, "docs", "production-readiness-gate.md"));
        var roadmap = File.ReadAllText(Path.Combine(root, "docs", "PRODUCT_COMPLETION_ROADMAP.md"));

        foreach (var expected in new[]
                 {
                     "PackageDirectory",
                     "ExpectedArchiveSha256",
                     "RequireProductionReady",
                     "validate-production-evidence-handoff-checklist.ps1",
                     "production-evidence-handoff-package-index.json",
                     "SHA256SUMS.txt",
                     "unexpected file",
                     "sha256 mismatch",
                     "archiveSha256 does not match checklist",
                     "Production evidence handoff package markdown",
                     "production evidence handoff package valid"
                 })
        {
            Assert.Contains(expected, script, StringComparison.OrdinalIgnoreCase);
        }

        Assert.Contains("validate-production-evidence-handoff-package.ps1", docs, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("[x] `P11-ACC-024`", roadmap, StringComparison.Ordinal);
    }

    [Fact]
    public void Production_Evidence_Handoff_Package_Archive_Should_Be_Built_From_Validated_Package()
    {
        var root = FindRepositoryRoot();
        var script = File.ReadAllText(Path.Combine(root, "scripts", "new-production-evidence-handoff-package-archive.ps1"));
        var docs = File.ReadAllText(Path.Combine(root, "docs", "production-readiness-gate.md"));
        var roadmap = File.ReadAllText(Path.Combine(root, "docs", "PRODUCT_COMPLETION_ROADMAP.md"));

        foreach (var expected in new[]
                 {
                     "PackageDirectory",
                     "OutputPath",
                     "ExpectedArchiveSha256",
                     "RequireProductionReady",
                     "validate-production-evidence-handoff-package.ps1",
                     "ZipArchive",
                     "Get-SafeEntryName",
                     "duplicated entry",
                     "production evidence handoff package archive created",
                     "packageArchiveSourceSha256",
                     "entries"
                 })
        {
            Assert.Contains(expected, script, StringComparison.OrdinalIgnoreCase);
        }

        Assert.Contains("new-production-evidence-handoff-package-archive.ps1", docs, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("[x] `P11-ACC-025`", roadmap, StringComparison.Ordinal);
    }

    [Fact]
    public void Production_Evidence_Handoff_Package_Archive_Validator_Should_Verify_Zip_And_Package()
    {
        var root = FindRepositoryRoot();
        var script = File.ReadAllText(Path.Combine(root, "scripts", "validate-production-evidence-handoff-package-archive.ps1"));
        var docs = File.ReadAllText(Path.Combine(root, "docs", "production-readiness-gate.md"));
        var roadmap = File.ReadAllText(Path.Combine(root, "docs", "PRODUCT_COMPLETION_ROADMAP.md"));

        foreach (var expected in new[]
                 {
                     "ArchivePath",
                     "ExpectedArchiveSha256",
                     "RequireProductionReady",
                     "validate-production-evidence-handoff-package.ps1",
                     "ZipArchive",
                     "production-evidence-handoff-package-index.json",
                     "SHA256SUMS.txt",
                     "unexpected entry",
                     "duplicated entry",
                     "production evidence handoff package archive valid"
                 })
        {
            Assert.Contains(expected, script, StringComparison.OrdinalIgnoreCase);
        }

        Assert.Contains("validate-production-evidence-handoff-package-archive.ps1", docs, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("[x] `P11-ACC-026`", roadmap, StringComparison.Ordinal);
    }

    [Fact]
    public void Production_Evidence_Handoff_Package_Archive_Validator_Should_Have_Tamper_Regression_Harness()
    {
        var root = FindRepositoryRoot();
        var script = File.ReadAllText(Path.Combine(root, "scripts", "test-production-evidence-handoff-package-archive-validator.ps1"));
        var docs = File.ReadAllText(Path.Combine(root, "docs", "production-readiness-gate.md"));
        var roadmap = File.ReadAllText(Path.Combine(root, "docs", "PRODUCT_COMPLETION_ROADMAP.md"));

        foreach (var expected in new[]
                 {
                     "ArchivePath",
                     "ExpectedArchiveSha256",
                     "Invoke-ArchiveValidator",
                     "validate-production-evidence-handoff-package-archive.ps1",
                     "Assert-FailsWith",
                     "unexpected-entry.txt",
                     "SHA256SUMS.txt",
                     "does not match expected archive hash",
                     "missing required entry",
                     "production evidence handoff package archive validator regression passed"
                 })
        {
            Assert.Contains(expected, script, StringComparison.OrdinalIgnoreCase);
        }

        Assert.Contains("test-production-evidence-handoff-package-archive-validator.ps1", docs, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("[x] `P11-ACC-027`", roadmap, StringComparison.Ordinal);
    }

    [Fact]
    public void Production_Evidence_Handoff_Package_Archive_Flow_Should_Have_Executable_End_To_End_Harness()
    {
        var root = FindRepositoryRoot();
        var script = File.ReadAllText(Path.Combine(root, "scripts", "test-production-evidence-handoff-package-archive-flow.ps1"));
        var docs = File.ReadAllText(Path.Combine(root, "docs", "production-readiness-gate.md"));
        var roadmap = File.ReadAllText(Path.Combine(root, "docs", "PRODUCT_COMPLETION_ROADMAP.md"));

        foreach (var expected in new[]
                 {
                     "OutputDirectory",
                     "new-production-evidence-bundle.ps1",
                     "new-production-readiness-summary.ps1",
                     "new-production-evidence-manifest.ps1",
                     "new-production-evidence-archive.ps1",
                     "new-production-evidence-handoff-receipt.ps1",
                     "new-production-evidence-handoff-checklist.ps1",
                     "new-production-evidence-handoff-package.ps1",
                     "new-production-evidence-handoff-package-archive.ps1",
                     "test-production-evidence-handoff-package-archive-validator.ps1",
                     "production evidence handoff package archive flow passed"
                 })
        {
            Assert.Contains(expected, script, StringComparison.OrdinalIgnoreCase);
        }

        Assert.Contains("test-production-evidence-handoff-package-archive-flow.ps1", docs, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("[x] `P11-ACC-028`", roadmap, StringComparison.Ordinal);
    }

    [Fact]
    public void Production_Evidence_Handoff_Package_Archive_Flow_Should_Guard_Output_Directory_Cleanup()
    {
        var root = FindRepositoryRoot();
        var script = File.ReadAllText(Path.Combine(root, "scripts", "test-production-evidence-handoff-package-archive-flow.ps1"));
        var docs = File.ReadAllText(Path.Combine(root, "docs", "production-readiness-gate.md"));
        var roadmap = File.ReadAllText(Path.Combine(root, "docs", "PRODUCT_COMPLETION_ROADMAP.md"));

        foreach (var expected in new[]
                 {
                     "Assert-SafeOutputDirectory",
                     "filesystem root",
                     "repository root",
                     "clearly named for production-evidence artifacts",
                     "Remove-Item -LiteralPath $bundleDirectory -Recurse -Force",
                     "production evidence handoff package archive flow output directory"
                 })
        {
            Assert.Contains(expected, script, StringComparison.OrdinalIgnoreCase);
        }

        Assert.Contains("production-evidence", docs, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("[x] `P11-ACC-029`", roadmap, StringComparison.Ordinal);
    }

    [Fact]
    public void Production_Evidence_Handoff_Package_Archive_Flow_Should_Write_Result_Artifacts()
    {
        var root = FindRepositoryRoot();
        var script = File.ReadAllText(Path.Combine(root, "scripts", "test-production-evidence-handoff-package-archive-flow.ps1"));
        var docs = File.ReadAllText(Path.Combine(root, "docs", "production-readiness-gate.md"));
        var roadmap = File.ReadAllText(Path.Combine(root, "docs", "PRODUCT_COMPLETION_ROADMAP.md"));

        foreach (var expected in new[]
                 {
                     "production-evidence-handoff-package-archive-flow-result.json",
                     "production-evidence-handoff-package-archive-flow-result.md",
                     "Write-Utf8NoBomFile",
                     "ConvertTo-FlowMarkdown",
                     "resultJsonPath",
                     "resultMarkdownPath",
                     "Tested failures",
                     "Handoff package archive SHA256"
                 })
        {
            Assert.Contains(expected, script, StringComparison.OrdinalIgnoreCase);
        }

        Assert.Contains("production-evidence-handoff-package-archive-flow-result.json", docs, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("[x] `P11-ACC-030`", roadmap, StringComparison.Ordinal);
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
