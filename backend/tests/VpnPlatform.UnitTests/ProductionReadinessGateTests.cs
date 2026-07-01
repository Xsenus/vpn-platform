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
    public void Production_Readiness_Gate_Should_Write_Result_Artifacts()
    {
        var root = FindRepositoryRoot();
        var script = File.ReadAllText(Path.Combine(root, "scripts", "assert-production-readiness.ps1"));
        var docs = File.ReadAllText(Path.Combine(root, "docs", "production-readiness-gate.md"));
        var roadmap = File.ReadAllText(Path.Combine(root, "docs", "PRODUCT_COMPLETION_ROADMAP.md"));

        foreach (var expected in new[]
                 {
                     "OutputPath",
                     "JsonOutputPath",
                     "Write-ReadinessResult",
                     "ConvertTo-ReadinessMarkdown",
                     "Production readiness assertion",
                     "Failed evidence reports",
                     "Blockers",
                     "failedEvidenceReportsCount",
                     "blockersCount",
                     "resultJsonPath",
                     "resultMarkdownPath"
                 })
        {
            Assert.Contains(expected, script, StringComparison.OrdinalIgnoreCase);
        }

        Assert.Contains("assert-production-readiness.ps1", docs, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("-OutputPath", docs, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("[x] `P11-ACC-041`", roadmap, StringComparison.Ordinal);
    }

    [Fact]
    public void Production_Readiness_Assertion_Result_Should_Have_Standalone_Validator()
    {
        var root = FindRepositoryRoot();
        var validator = File.ReadAllText(Path.Combine(root, "scripts", "validate-production-readiness-assertion-result.ps1"));
        var gate = File.ReadAllText(Path.Combine(root, "scripts", "assert-production-readiness.ps1"));
        var docs = File.ReadAllText(Path.Combine(root, "docs", "production-readiness-gate.md"));
        var roadmap = File.ReadAllText(Path.Combine(root, "docs", "PRODUCT_COMPLETION_ROADMAP.md"));

        foreach (var expected in new[]
                 {
                     "ResultJsonPath",
                     "ResultMarkdownPath",
                     "RequireProductionReady",
                     "failedEvidenceReportsCount",
                     "blockersCount",
                     "staging-vps",
                     "payment-providers",
                     "admin-vps",
                     "vpn-live",
                     "production readiness assertion result valid"
                 })
        {
            Assert.Contains(expected, validator, StringComparison.OrdinalIgnoreCase);
        }

        Assert.Contains("validate-production-readiness-assertion-result.ps1", gate, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("validate-production-readiness-assertion-result.ps1", docs, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("[x] `P11-ACC-042`", roadmap, StringComparison.Ordinal);
    }

    [Fact]
    public void Production_Readiness_Assertion_Result_Validator_Should_Have_Regression_Harness()
    {
        var root = FindRepositoryRoot();
        var harness = File.ReadAllText(Path.Combine(root, "scripts", "test-production-readiness-assertion-result-validator.ps1"));
        var docs = File.ReadAllText(Path.Combine(root, "docs", "production-readiness-gate.md"));
        var roadmap = File.ReadAllText(Path.Combine(root, "docs", "PRODUCT_COMPLETION_ROADMAP.md"));

        foreach (var expected in new[]
                 {
                     "validate-production-readiness-assertion-result.ps1",
                     "bad-status",
                     "bad-failed-evidence-count",
                     "missing-evidence-report",
                     "bad-markdown",
                     "require-production-ready",
                     "production readiness assertion result validator regression passed"
                 })
        {
            Assert.Contains(expected, harness, StringComparison.OrdinalIgnoreCase);
        }

        Assert.Contains("test-production-readiness-assertion-result-validator.ps1", docs, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("[x] `P11-ACC-043`", roadmap, StringComparison.Ordinal);
    }

    [Fact]
    public void Production_Readiness_Assertion_Should_Have_Ci_Regression_Wrapper()
    {
        var root = FindRepositoryRoot();
        var wrapper = File.ReadAllText(Path.Combine(root, "scripts", "test-production-readiness-assertion-ci-regression.ps1"));
        var workflow = File.ReadAllText(Path.Combine(root, ".github", "workflows", "ci.yml"));
        var docs = File.ReadAllText(Path.Combine(root, "docs", "production-readiness-gate.md"));
        var roadmap = File.ReadAllText(Path.Combine(root, "docs", "PRODUCT_COMPLETION_ROADMAP.md"));

        foreach (var expected in new[]
                 {
                     "assert-production-readiness.ps1",
                     "validate-production-readiness-assertion-result.ps1",
                     "test-production-readiness-assertion-result-validator.ps1",
                     "production-readiness-assertion-ci-regression-result.json",
                     "production-readiness-assertion-ci-regression-result.md",
                     "GITHUB_STEP_SUMMARY",
                     "production readiness assertion CI regression passed"
                 })
        {
            Assert.Contains(expected, wrapper, StringComparison.OrdinalIgnoreCase);
        }

        Assert.Contains("production-readiness-assertion", workflow, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("test-production-readiness-assertion-ci-regression.ps1", workflow, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("production-readiness-assertion-ci-regression", workflow, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("test-production-readiness-assertion-ci-regression.ps1", docs, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("[x] `P11-ACC-044`", roadmap, StringComparison.Ordinal);
    }

    [Fact]
    public void Production_Readiness_Assertion_Ci_Regression_Result_Should_Have_Standalone_Validator()
    {
        var root = FindRepositoryRoot();
        var validator = File.ReadAllText(Path.Combine(root, "scripts", "validate-production-readiness-assertion-ci-regression-result.ps1"));
        var wrapper = File.ReadAllText(Path.Combine(root, "scripts", "test-production-readiness-assertion-ci-regression.ps1"));
        var docs = File.ReadAllText(Path.Combine(root, "docs", "production-readiness-gate.md"));
        var roadmap = File.ReadAllText(Path.Combine(root, "docs", "PRODUCT_COMPLETION_ROADMAP.md"));

        foreach (var expected in new[]
                 {
                     "ResultJsonPath",
                     "ResultMarkdownPath",
                     "RequireBlockedAssertion",
                     "production-readiness-assertion-ci-regression-result.json",
                     "production-readiness-assertion-ci-regression-result.md",
                     "bad-failed-evidence-count",
                     "require-production-ready",
                     "production readiness assertion CI regression result valid"
                 })
        {
            Assert.Contains(expected, validator, StringComparison.OrdinalIgnoreCase);
        }

        Assert.Contains("validate-production-readiness-assertion-ci-regression-result.ps1", wrapper, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("validate-production-readiness-assertion-ci-regression-result.ps1", docs, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("[x] `P11-ACC-045`", roadmap, StringComparison.Ordinal);
    }

    [Fact]
    public void Production_Readiness_Assertion_Ci_Result_Validator_Should_Have_Regression_Harness()
    {
        var root = FindRepositoryRoot();
        var harness = File.ReadAllText(Path.Combine(root, "scripts", "test-production-readiness-assertion-ci-regression-result-validator.ps1"));
        var wrapper = File.ReadAllText(Path.Combine(root, "scripts", "test-production-readiness-assertion-ci-regression.ps1"));
        var docs = File.ReadAllText(Path.Combine(root, "docs", "production-readiness-gate.md"));
        var roadmap = File.ReadAllText(Path.Combine(root, "docs", "PRODUCT_COMPLETION_ROADMAP.md"));

        foreach (var expected in new[]
                 {
                     "validate-production-readiness-assertion-ci-regression-result.ps1",
                     "bad-status",
                     "bad-assertion-exit-code",
                     "missing-regression-failure",
                     "bad-markdown",
                     "wrong-validator-count",
                     "production readiness assertion CI regression result validator regression passed"
                 })
        {
            Assert.Contains(expected, harness, StringComparison.OrdinalIgnoreCase);
        }

        Assert.Contains("test-production-readiness-assertion-ci-regression-result-validator.ps1", wrapper, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("ciResultValidatorRegression", wrapper, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("test-production-readiness-assertion-ci-regression-result-validator.ps1", docs, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("[x] `P11-ACC-046`", roadmap, StringComparison.Ordinal);
    }

    [Fact]
    public void Production_Readiness_Assertion_Ci_Summary_Should_Have_Fail_Closed_Validator()
    {
        var root = FindRepositoryRoot();
        var validator = File.ReadAllText(Path.Combine(root, "scripts", "validate-production-readiness-assertion-ci-summary.ps1"));
        var harness = File.ReadAllText(Path.Combine(root, "scripts", "test-production-readiness-assertion-ci-summary-validator.ps1"));
        var wrapper = File.ReadAllText(Path.Combine(root, "scripts", "test-production-readiness-assertion-ci-regression.ps1"));
        var docs = File.ReadAllText(Path.Combine(root, "docs", "production-readiness-gate.md"));
        var roadmap = File.ReadAllText(Path.Combine(root, "docs", "PRODUCT_COMPLETION_ROADMAP.md"));

        foreach (var expected in new[]
                 {
                     "ResultJsonPath",
                     "SummaryPath",
                     "Production readiness assertion CI regression",
                     "Assertion status",
                     "Result validator regression",
                     "CI result validator regression",
                     "CI regression JSON",
                     "production readiness assertion CI summary valid"
                 })
        {
            Assert.Contains(expected, validator, StringComparison.OrdinalIgnoreCase);
        }

        foreach (var expected in new[]
                 {
                     "validate-production-readiness-assertion-ci-summary.ps1",
                     "bad-status",
                     "bad-assertion-status",
                     "missing-artifact-path",
                     "bad-result-validator-regression",
                     "bad-ci-result-validator-regression",
                     "production readiness assertion CI summary validator regression passed"
                 })
        {
            Assert.Contains(expected, harness, StringComparison.OrdinalIgnoreCase);
        }

        Assert.Contains("validate-production-readiness-assertion-ci-summary.ps1", wrapper, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("test-production-readiness-assertion-ci-summary-validator.ps1", wrapper, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("ciSummaryValidatorRegression", wrapper, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("validate-production-readiness-assertion-ci-summary.ps1", docs, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("test-production-readiness-assertion-ci-summary-validator.ps1", docs, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("[x] `P11-ACC-047`", roadmap, StringComparison.Ordinal);
    }

    [Fact]
    public void Production_Readiness_Assertion_Ci_Should_Verify_GitHub_Step_Summary_File()
    {
        var root = FindRepositoryRoot();
        var script = File.ReadAllText(Path.Combine(root, "scripts", "test-production-readiness-assertion-ci-step-summary.ps1"));
        var wrapper = File.ReadAllText(Path.Combine(root, "scripts", "test-production-readiness-assertion-ci-regression.ps1"));
        var docs = File.ReadAllText(Path.Combine(root, "docs", "production-readiness-gate.md"));
        var roadmap = File.ReadAllText(Path.Combine(root, "docs", "PRODUCT_COMPLETION_ROADMAP.md"));

        foreach (var expected in new[]
                 {
                     "GITHUB_STEP_SUMMARY",
                     "test-production-readiness-assertion-ci-regression.ps1",
                     "validate-production-readiness-assertion-ci-summary.ps1",
                     "production-readiness-assertion-ci-step-summary.md",
                     "CI summary validator regression",
                     "CI result validator regression",
                     "CI artifacts validator regression",
                     "Markdown does not match result Markdown",
                     "production readiness assertion CI step summary passed"
                 })
        {
            Assert.Contains(expected, script, StringComparison.OrdinalIgnoreCase);
        }

        Assert.Contains("Add-GitHubStepSummary", wrapper, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("validate-production-readiness-assertion-ci-summary.ps1", wrapper, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("test-production-readiness-assertion-ci-step-summary.ps1", docs, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("[x] `P11-ACC-048`", roadmap, StringComparison.Ordinal);
    }

    [Fact]
    public void Production_Readiness_Assertion_Ci_Artifacts_Should_Have_Directory_Validator()
    {
        var root = FindRepositoryRoot();
        var validator = File.ReadAllText(Path.Combine(root, "scripts", "validate-production-readiness-assertion-ci-artifacts.ps1"));
        var wrapper = File.ReadAllText(Path.Combine(root, "scripts", "test-production-readiness-assertion-ci-regression.ps1"));
        var docs = File.ReadAllText(Path.Combine(root, "docs", "production-readiness-gate.md"));
        var roadmap = File.ReadAllText(Path.Combine(root, "docs", "PRODUCT_COMPLETION_ROADMAP.md"));

        foreach (var expected in new[]
                 {
                     "ArtifactDirectory",
                     "StepSummaryPath",
                     "RequireBlockedAssertion",
                     "production-readiness-assertion-ci-regression-result.json",
                     "production-readiness-assertion-ci-regression-result.md",
                     "production-readiness-assertion.json",
                     "production-readiness-assertion.md",
                     "production-readiness-assertion.log",
                     "validate-production-readiness-assertion-ci-regression-result.ps1",
                     "validate-production-readiness-assertion-ci-summary.ps1",
                     "outputDirectory does not match artifact directory",
                     "production readiness assertion CI artifacts valid"
                 })
        {
            Assert.Contains(expected, validator, StringComparison.OrdinalIgnoreCase);
        }

        Assert.Contains("validate-production-readiness-assertion-ci-artifacts.ps1", wrapper, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("validate-production-readiness-assertion-ci-artifacts.ps1", docs, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("[x] `P11-ACC-049`", roadmap, StringComparison.Ordinal);
    }

    [Fact]
    public void Production_Readiness_Assertion_Ci_Artifacts_Validator_Should_Have_Regression_Harness()
    {
        var root = FindRepositoryRoot();
        var harness = File.ReadAllText(Path.Combine(root, "scripts", "test-production-readiness-assertion-ci-artifacts-validator.ps1"));
        var wrapper = File.ReadAllText(Path.Combine(root, "scripts", "test-production-readiness-assertion-ci-regression.ps1"));
        var resultValidator = File.ReadAllText(Path.Combine(root, "scripts", "validate-production-readiness-assertion-ci-regression-result.ps1"));
        var summaryValidator = File.ReadAllText(Path.Combine(root, "scripts", "validate-production-readiness-assertion-ci-summary.ps1"));
        var docs = File.ReadAllText(Path.Combine(root, "docs", "production-readiness-gate.md"));
        var roadmap = File.ReadAllText(Path.Combine(root, "docs", "PRODUCT_COMPLETION_ROADMAP.md"));

        foreach (var expected in new[]
                 {
                     "missing-required-artifact",
                     "bad-output-directory",
                     "bad-assertion-log-path",
                     "bad-result-markdown",
                     "bad-step-summary",
                     "Copy-ArtifactDirectory",
                     "validate-production-readiness-assertion-ci-artifacts.ps1",
                     "production readiness assertion CI artifacts validator regression passed"
                 })
        {
            Assert.Contains(expected, harness, StringComparison.OrdinalIgnoreCase);
        }

        Assert.Contains("test-production-readiness-assertion-ci-artifacts-validator.ps1", wrapper, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("ciArtifactsValidatorRegression", wrapper, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("CI artifacts validator regression", wrapper, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("ciArtifactsValidatorRegression", resultValidator, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("ciArtifactsValidatorRegression", summaryValidator, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("test-production-readiness-assertion-ci-artifacts-validator.ps1", docs, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("[x] `P11-ACC-050`", roadmap, StringComparison.Ordinal);
    }

    [Fact]
    public void Production_Readiness_Assertion_Ci_Summary_Regression_Should_Cover_Artifacts_Regression_Line()
    {
        var root = FindRepositoryRoot();
        var summaryHarness = File.ReadAllText(Path.Combine(root, "scripts", "test-production-readiness-assertion-ci-summary-validator.ps1"));
        var wrapper = File.ReadAllText(Path.Combine(root, "scripts", "test-production-readiness-assertion-ci-regression.ps1"));
        var resultValidator = File.ReadAllText(Path.Combine(root, "scripts", "validate-production-readiness-assertion-ci-regression-result.ps1"));
        var docs = File.ReadAllText(Path.Combine(root, "docs", "production-readiness-gate.md"));
        var roadmap = File.ReadAllText(Path.Combine(root, "docs", "PRODUCT_COMPLETION_ROADMAP.md"));

        foreach (var expected in new[]
                 {
                     "bad-ci-artifacts-validator-regression",
                     "CI artifacts validator regression",
                     "ciArtifactsValidatorRegression",
                     "markdown is missing",
                     "Production readiness assertion CI summary validator regression passed"
                 })
        {
            Assert.Contains(expected, summaryHarness, StringComparison.OrdinalIgnoreCase);
        }

        Assert.Contains("ciArtifactsValidatorRegression", wrapper, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("ciSummaryValidatorRegression", wrapper, StringComparison.OrdinalIgnoreCase);
        Assert.True(
            wrapper.IndexOf("scripts/test-production-readiness-assertion-ci-artifacts-validator.ps1", StringComparison.OrdinalIgnoreCase)
                < wrapper.IndexOf("scripts/test-production-readiness-assertion-ci-summary-validator.ps1", StringComparison.OrdinalIgnoreCase),
            "Artifact regression harness must run before summary regression tests the summary line.");
        Assert.Contains("bad-ci-artifacts-validator-regression", resultValidator, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("bad-ci-artifacts-validator-regression", docs, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("[x] `P11-ACC-051`", roadmap, StringComparison.Ordinal);
    }

    [Fact]
    public void Production_Readiness_Assertion_Ci_Workflow_Should_Publish_Complete_Artifacts_Directory()
    {
        var root = FindRepositoryRoot();
        var workflowGuard = File.ReadAllText(Path.Combine(root, "scripts", "test-production-readiness-assertion-ci-workflow-artifacts.ps1"));
        var workflow = File.ReadAllText(Path.Combine(root, ".github", "workflows", "ci.yml"));
        var docs = File.ReadAllText(Path.Combine(root, "docs", "production-readiness-gate.md"));
        var roadmap = File.ReadAllText(Path.Combine(root, "docs", "PRODUCT_COMPLETION_ROADMAP.md"));

        foreach (var expected in new[]
                 {
                     "production-readiness-assertion:",
                     "needs: backend",
                     "Guard production readiness assertion workflow artifacts",
                     "test-production-readiness-assertion-ci-workflow-artifacts.ps1 -WriteJson",
                     "test-production-readiness-assertion-ci-regression.ps1",
                     "actions/upload-artifact@v4",
                     "if-no-files-found: error",
                     "production-readiness-assertion-ci-regression-result.json",
                     "production-readiness-assertion-ci-regression-result.md",
                     "production-readiness-assertion.json",
                     "production-readiness-assertion.md",
                     "production-readiness-assertion.log",
                     "production readiness assertion CI workflow artifacts passed"
                 })
        {
            Assert.Contains(expected, workflowGuard, StringComparison.OrdinalIgnoreCase);
        }

        foreach (var expected in new[]
                 {
                     "production-readiness-assertion-ci-regression-result.json",
                     "production-readiness-assertion-ci-regression-result.md",
                     "production-readiness-assertion.json",
                     "production-readiness-assertion.md",
                     "production-readiness-assertion.log"
                 })
        {
            Assert.Contains(expected, workflow, StringComparison.OrdinalIgnoreCase);
        }

        Assert.Contains("test-production-readiness-assertion-ci-workflow-artifacts.ps1", docs, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("[x] `P11-ACC-052`", roadmap, StringComparison.Ordinal);
    }

    [Fact]
    public void Production_Readiness_Assertion_Ci_Workflow_Should_Run_Workflow_Artifacts_Guard()
    {
        var root = FindRepositoryRoot();
        var workflow = File.ReadAllText(Path.Combine(root, ".github", "workflows", "ci.yml"));
        var docs = File.ReadAllText(Path.Combine(root, "docs", "production-readiness-gate.md"));
        var roadmap = File.ReadAllText(Path.Combine(root, "docs", "PRODUCT_COMPLETION_ROADMAP.md"));

        var guardStepIndex = workflow.IndexOf(
            "Guard production readiness assertion workflow artifacts",
            StringComparison.OrdinalIgnoreCase);
        var guardCommandIndex = workflow.IndexOf(
            "test-production-readiness-assertion-ci-workflow-artifacts.ps1 -WriteJson",
            StringComparison.OrdinalIgnoreCase);
        var wrapperIndex = workflow.IndexOf(
            "Run production readiness assertion CI regression",
            StringComparison.OrdinalIgnoreCase);
        var uploadIndex = workflow.IndexOf(
            "Upload production readiness assertion artifacts",
            StringComparison.OrdinalIgnoreCase);

        Assert.True(guardStepIndex >= 0, "Production readiness assertion workflow guard step must be present in CI.");
        Assert.True(guardCommandIndex > guardStepIndex, "Production readiness assertion workflow guard command must be in the guard step.");
        Assert.True(guardCommandIndex < wrapperIndex, "Workflow artifacts guard must run before the readiness assertion wrapper.");
        Assert.True(wrapperIndex < uploadIndex, "Readiness assertion wrapper must still run before artifacts upload.");
        Assert.Contains("test-production-readiness-assertion-ci-workflow-artifacts.ps1 -WriteJson", docs, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("[x] `P11-ACC-053`", roadmap, StringComparison.Ordinal);
    }

    [Fact]
    public void Production_Readiness_Assertion_Ci_Workflow_Guard_Should_Have_Fail_Closed_Regression()
    {
        var root = FindRepositoryRoot();
        var harness = File.ReadAllText(Path.Combine(root, "scripts", "test-production-readiness-assertion-ci-workflow-artifacts-validator.ps1"));
        var docs = File.ReadAllText(Path.Combine(root, "docs", "production-readiness-gate.md"));
        var roadmap = File.ReadAllText(Path.Combine(root, "docs", "PRODUCT_COMPLETION_ROADMAP.md"));

        foreach (var expected in new[]
                 {
                     "test-production-readiness-assertion-ci-workflow-artifacts.ps1",
                     "missing-guard-step",
                     "missing-assertion-log-artifact",
                     "bad-artifact-name",
                     "missing-if-no-files-found-error",
                     "testedFailures",
                     "production readiness assertion CI workflow artifacts guard validator passed"
                 })
        {
            Assert.Contains(expected, harness, StringComparison.OrdinalIgnoreCase);
        }

        Assert.Contains("test-production-readiness-assertion-ci-workflow-artifacts-validator.ps1", docs, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("[x] `P11-ACC-056`", roadmap, StringComparison.Ordinal);
    }

    [Fact]
    public void Production_Ci_Workflow_Artifacts_Guards_Should_Have_Aggregate_Command()
    {
        var root = FindRepositoryRoot();
        var harness = File.ReadAllText(Path.Combine(root, "scripts", "test-production-ci-workflow-artifacts-guards.ps1"));
        var workflow = File.ReadAllText(Path.Combine(root, ".github", "workflows", "ci.yml"));
        var docs = File.ReadAllText(Path.Combine(root, "docs", "production-readiness-gate.md"));
        var roadmap = File.ReadAllText(Path.Combine(root, "docs", "PRODUCT_COMPLETION_ROADMAP.md"));

        foreach (var expected in new[]
                 {
                     "test-production-readiness-assertion-ci-workflow-artifacts.ps1",
                     "test-production-readiness-assertion-ci-workflow-artifacts-validator.ps1",
                     "test-production-evidence-handoff-package-archive-ci-workflow-artifacts.ps1",
                     "test-production-evidence-handoff-package-archive-ci-workflow-artifacts-validator.ps1",
                     "guardsCount",
                     "production CI workflow artifacts guards passed"
                 })
        {
            Assert.Contains(expected, harness, StringComparison.OrdinalIgnoreCase);
        }

        var aggregateStepIndex = workflow.IndexOf("Guard production CI workflow artifacts contracts", StringComparison.OrdinalIgnoreCase);
        var aggregateCommandIndex = workflow.IndexOf("test-production-ci-workflow-artifacts-guards.ps1 -WriteJson", StringComparison.OrdinalIgnoreCase);
        var setupDotnetIndex = workflow.IndexOf("Setup .NET SDK from global.json", StringComparison.OrdinalIgnoreCase);

        Assert.True(aggregateStepIndex >= 0, "Aggregate production CI workflow artifacts guard step must be present in CI.");
        Assert.True(aggregateCommandIndex > aggregateStepIndex, "Aggregate production CI workflow artifacts guard command must be in the guard step.");
        Assert.True(aggregateCommandIndex < setupDotnetIndex, "Aggregate production CI workflow artifacts guard must run before backend setup and tests.");
        Assert.Contains("test-production-ci-workflow-artifacts-guards.ps1", docs, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("[x] `P11-ACC-057`", roadmap, StringComparison.Ordinal);
    }

    [Fact]
    public void Production_Ci_Workflow_Artifacts_Guards_Aggregate_Should_Include_Ci_Step_Guards()
    {
        var root = FindRepositoryRoot();
        var harness = File.ReadAllText(Path.Combine(root, "scripts", "test-production-ci-workflow-artifacts-guards.ps1"));
        var docs = File.ReadAllText(Path.Combine(root, "docs", "production-readiness-gate.md"));
        var roadmap = File.ReadAllText(Path.Combine(root, "docs", "PRODUCT_COMPLETION_ROADMAP.md"));

        foreach (var expected in new[]
                 {
                     "production-ci-workflow-artifacts-ci-step",
                     "test-production-ci-workflow-artifacts-guards-ci-step.ps1",
                     "production-ci-workflow-artifacts-ci-step-validator",
                     "test-production-ci-workflow-artifacts-guards-ci-step-validator.ps1",
                     "production-readiness-assertion-workflow-artifacts",
                     "production-evidence-workflow-artifacts",
                     "guardsCount"
                 })
        {
            Assert.Contains(expected, harness, StringComparison.OrdinalIgnoreCase);
        }

        Assert.Contains("test-production-ci-workflow-artifacts-guards.ps1", docs, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("[x] `P11-ACC-062`", roadmap, StringComparison.Ordinal);
    }

    [Fact]
    public void Production_Ci_Workflow_Artifacts_Guards_Aggregate_Should_Have_Fail_Closed_Regression()
    {
        var root = FindRepositoryRoot();
        var harness = File.ReadAllText(Path.Combine(root, "scripts", "test-production-ci-workflow-artifacts-guards-validator.ps1"));
        var docs = File.ReadAllText(Path.Combine(root, "docs", "production-readiness-gate.md"));
        var roadmap = File.ReadAllText(Path.Combine(root, "docs", "PRODUCT_COMPLETION_ROADMAP.md"));

        foreach (var expected in new[]
                 {
                     "test-production-ci-workflow-artifacts-guards.ps1",
                     "missing-readiness-guard-step",
                     "missing-readiness-assertion-log-artifact",
                     "missing-production-evidence-result-artifact",
                     "missing-if-no-files-found-error",
                     "testedFailures",
                     "production CI workflow artifacts aggregate guard validator passed"
                 })
        {
            Assert.Contains(expected, harness, StringComparison.OrdinalIgnoreCase);
        }

        Assert.Contains("test-production-ci-workflow-artifacts-guards-validator.ps1", docs, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("[x] `P11-ACC-058`", roadmap, StringComparison.Ordinal);
    }

    [Fact]
    public void Production_Ci_Workflow_Artifacts_Guards_Aggregate_Validator_Should_Cover_Ci_Step_Guards()
    {
        var root = FindRepositoryRoot();
        var harness = File.ReadAllText(Path.Combine(root, "scripts", "test-production-ci-workflow-artifacts-guards-validator.ps1"));
        var docs = File.ReadAllText(Path.Combine(root, "docs", "production-readiness-gate.md"));
        var roadmap = File.ReadAllText(Path.Combine(root, "docs", "PRODUCT_COMPLETION_ROADMAP.md"));

        foreach (var expected in new[]
                 {
                     "missing-aggregate-ci-step-guard-command",
                     "missing-aggregate-ci-step-validator",
                     "aggregate CI step guard command",
                     "aggregate CI step guard validator"
                 })
        {
            Assert.Contains(expected, harness, StringComparison.OrdinalIgnoreCase);
        }

        Assert.Contains("test-production-ci-workflow-artifacts-guards-validator.ps1", docs, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("[x] `P11-ACC-063`", roadmap, StringComparison.Ordinal);
    }

    [Fact]
    public void Production_Ci_Workflow_Artifacts_Aggregate_Validator_Should_Run_In_Ci()
    {
        var root = FindRepositoryRoot();
        var workflow = File.ReadAllText(Path.Combine(root, ".github", "workflows", "ci.yml"));
        var docs = File.ReadAllText(Path.Combine(root, "docs", "production-readiness-gate.md"));
        var roadmap = File.ReadAllText(Path.Combine(root, "docs", "PRODUCT_COMPLETION_ROADMAP.md"));

        var aggregateGuardIndex = workflow.IndexOf(
            "Guard production CI workflow artifacts contracts",
            StringComparison.OrdinalIgnoreCase);
        var validatorStepIndex = workflow.IndexOf(
            "Guard production CI workflow artifacts contracts regression",
            StringComparison.OrdinalIgnoreCase);
        var validatorCommandIndex = workflow.IndexOf(
            "test-production-ci-workflow-artifacts-guards-validator.ps1 -WriteJson",
            StringComparison.OrdinalIgnoreCase);
        var setupDotnetIndex = workflow.IndexOf(
            "Setup .NET SDK from global.json",
            StringComparison.OrdinalIgnoreCase);

        Assert.True(aggregateGuardIndex >= 0, "Aggregate production CI workflow artifacts guard step must be present in CI.");
        Assert.True(validatorStepIndex > aggregateGuardIndex, "Aggregate validator step must run after the aggregate guard step.");
        Assert.True(validatorCommandIndex > validatorStepIndex, "Aggregate validator command must be inside the validator step.");
        Assert.True(validatorCommandIndex < setupDotnetIndex, "Aggregate validator must run before backend setup and tests.");
        Assert.Contains("test-production-ci-workflow-artifacts-guards-validator.ps1 -WriteJson", docs, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("[x] `P11-ACC-059`", roadmap, StringComparison.Ordinal);
    }

    [Fact]
    public void Production_Ci_Workflow_Artifacts_Aggregate_Ci_Steps_Should_Have_Guard()
    {
        var root = FindRepositoryRoot();
        var script = File.ReadAllText(Path.Combine(root, "scripts", "test-production-ci-workflow-artifacts-guards-ci-step.ps1"));
        var workflow = File.ReadAllText(Path.Combine(root, ".github", "workflows", "ci.yml"));
        var docs = File.ReadAllText(Path.Combine(root, "docs", "production-readiness-gate.md"));
        var roadmap = File.ReadAllText(Path.Combine(root, "docs", "PRODUCT_COMPLETION_ROADMAP.md"));

        foreach (var expected in new[]
                 {
                     "Guard production CI workflow artifacts guard steps",
                     "test-production-ci-workflow-artifacts-guards-ci-step.ps1 -WriteJson",
                     "Guard production CI workflow artifacts guard steps regression",
                     "test-production-ci-workflow-artifacts-guards-ci-step-validator.ps1 -WriteJson",
                     "Guard production CI workflow artifacts contracts",
                     "test-production-ci-workflow-artifacts-guards.ps1 -WriteJson",
                     "Guard production CI workflow artifacts contracts regression",
                     "test-production-ci-workflow-artifacts-guards-validator.ps1 -WriteJson",
                     "Setup .NET SDK from global.json",
                     "production CI workflow artifacts aggregate CI step guard passed"
                 })
        {
            Assert.Contains(expected, script, StringComparison.OrdinalIgnoreCase);
        }

        var ciStepGuardIndex = workflow.IndexOf(
            "Guard production CI workflow artifacts guard steps",
            StringComparison.OrdinalIgnoreCase);
        var ciStepGuardCommandIndex = workflow.IndexOf(
            "test-production-ci-workflow-artifacts-guards-ci-step.ps1 -WriteJson",
            StringComparison.OrdinalIgnoreCase);
        var aggregateGuardIndex = workflow.IndexOf(
            "Guard production CI workflow artifacts contracts",
            StringComparison.OrdinalIgnoreCase);
        var ciStepGuardValidatorIndex = workflow.IndexOf(
            "Guard production CI workflow artifacts guard steps regression",
            StringComparison.OrdinalIgnoreCase);
        var ciStepGuardValidatorCommandIndex = workflow.IndexOf(
            "test-production-ci-workflow-artifacts-guards-ci-step-validator.ps1 -WriteJson",
            StringComparison.OrdinalIgnoreCase);

        Assert.True(ciStepGuardIndex >= 0, "Aggregate CI step guard must run in CI.");
        Assert.True(ciStepGuardCommandIndex > ciStepGuardIndex, "Aggregate CI step guard command must be inside the step.");
        Assert.True(ciStepGuardCommandIndex < ciStepGuardValidatorIndex, "Aggregate CI step guard must run before its validator.");
        Assert.True(ciStepGuardValidatorCommandIndex > ciStepGuardValidatorIndex, "Aggregate CI step guard validator command must be inside the step.");
        Assert.True(ciStepGuardValidatorCommandIndex < aggregateGuardIndex, "Aggregate CI step guard validator must run before the aggregate guard.");
        Assert.Contains("test-production-ci-workflow-artifacts-guards-ci-step.ps1", docs, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("[x] `P11-ACC-060`", roadmap, StringComparison.Ordinal);
    }

    [Fact]
    public void Production_Ci_Workflow_Artifacts_Aggregate_Ci_Step_Guard_Should_Have_Fail_Closed_Validator()
    {
        var root = FindRepositoryRoot();
        var script = File.ReadAllText(Path.Combine(root, "scripts", "test-production-ci-workflow-artifacts-guards-ci-step-validator.ps1"));
        var workflow = File.ReadAllText(Path.Combine(root, ".github", "workflows", "ci.yml"));
        var docs = File.ReadAllText(Path.Combine(root, "docs", "production-readiness-gate.md"));
        var roadmap = File.ReadAllText(Path.Combine(root, "docs", "PRODUCT_COMPLETION_ROADMAP.md"));

        foreach (var expected in new[]
                 {
                     "test-production-ci-workflow-artifacts-guards-ci-step.ps1",
                     "missing-ci-step-guard",
                     "missing-ci-step-guard-command",
                     "missing-ci-step-validator",
                     "ci-step-guard-after-aggregate-guard",
                     "production CI workflow artifacts aggregate CI step guard validator passed"
                 })
        {
            Assert.Contains(expected, script, StringComparison.OrdinalIgnoreCase);
        }

        var ciStepGuardIndex = workflow.IndexOf(
            "Guard production CI workflow artifacts guard steps",
            StringComparison.OrdinalIgnoreCase);
        var ciStepGuardValidatorIndex = workflow.IndexOf(
            "Guard production CI workflow artifacts guard steps regression",
            StringComparison.OrdinalIgnoreCase);
        var ciStepGuardValidatorCommandIndex = workflow.IndexOf(
            "test-production-ci-workflow-artifacts-guards-ci-step-validator.ps1 -WriteJson",
            StringComparison.OrdinalIgnoreCase);
        var aggregateGuardIndex = workflow.IndexOf(
            "Guard production CI workflow artifacts contracts",
            StringComparison.OrdinalIgnoreCase);

        Assert.True(ciStepGuardValidatorIndex > ciStepGuardIndex, "Aggregate CI step guard validator must run after the guard.");
        Assert.True(ciStepGuardValidatorCommandIndex > ciStepGuardValidatorIndex, "Aggregate CI step guard validator command must be inside the step.");
        Assert.True(ciStepGuardValidatorCommandIndex < aggregateGuardIndex, "Aggregate CI step guard validator must run before the aggregate guard.");
        Assert.Contains("test-production-ci-workflow-artifacts-guards-ci-step-validator.ps1", docs, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("[x] `P11-ACC-061`", roadmap, StringComparison.Ordinal);
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
    public void Production_Evidence_Bundle_Should_Reject_Stale_Release_In_Production_Ready_Mode()
    {
        var root = FindRepositoryRoot();
        var validator = File.ReadAllText(Path.Combine(root, "scripts", "validate-production-evidence-bundle.ps1"));
        var regression = File.ReadAllText(Path.Combine(root, "scripts", "test-production-evidence-bundle-latest-release-guard.ps1"));
        var docs = File.ReadAllText(Path.Combine(root, "docs", "production-readiness-gate.md"));
        var roadmap = File.ReadAllText(Path.Combine(root, "docs", "PRODUCT_COMPLETION_ROADMAP.md"));

        foreach (var expected in new[]
                 {
                     "Get-LatestActiveReleaseId",
                     "backend/src/VpnPlatform.Api/AppReleases/releases.json",
                     "must match latest active release",
                     "RequireProductionReady"
                 })
        {
            Assert.Contains(expected, validator, StringComparison.OrdinalIgnoreCase);
        }

        foreach (var expected in new[]
                 {
                     "stale-release-id",
                     "must match latest active release",
                     "production evidence bundle latest release guard valid"
                 })
        {
            Assert.Contains(expected, regression, StringComparison.OrdinalIgnoreCase);
        }

        Assert.Contains("test-production-evidence-bundle-latest-release-guard.ps1", docs, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("[x] `P11-ACC-073`", roadmap, StringComparison.Ordinal);
    }

    [Fact]
    public void Production_Evidence_Bundle_Generator_Should_Reject_Unknown_Manual_Release()
    {
        var root = FindRepositoryRoot();
        var generator = File.ReadAllText(Path.Combine(root, "scripts", "new-production-evidence-bundle.ps1"));
        var regression = File.ReadAllText(Path.Combine(root, "scripts", "test-production-evidence-bundle-generator-release-guard.ps1"));
        var docs = File.ReadAllText(Path.Combine(root, "docs", "production-readiness-gate.md"));
        var roadmap = File.ReadAllText(Path.Combine(root, "docs", "PRODUCT_COMPLETION_ROADMAP.md"));

        foreach (var expected in new[]
                 {
                     "Assert-KnownReleaseId",
                     "ReleaseId must exist in backend/src/VpnPlatform.Api/AppReleases/releases.json",
                     "missing-release-id-for-regression"
                 })
        {
            Assert.Contains(expected, generator + regression, StringComparison.OrdinalIgnoreCase);
        }

        foreach (var expected in new[]
                 {
                     "Production evidence bundle generator accepted unknown ReleaseId",
                     "Production evidence bundle generator created output directory after unknown ReleaseId failure",
                     "production evidence bundle generator release guard valid"
                 })
        {
            Assert.Contains(expected, regression, StringComparison.OrdinalIgnoreCase);
        }

        Assert.Contains("test-production-evidence-bundle-generator-release-guard.ps1", docs, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("[x] `P11-ACC-076`", roadmap, StringComparison.Ordinal);
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
    public void Production_Evidence_Manifest_Should_Reject_Unknown_Release()
    {
        var root = FindRepositoryRoot();
        var generator = File.ReadAllText(Path.Combine(root, "scripts", "new-production-evidence-manifest.ps1"));
        var regression = File.ReadAllText(Path.Combine(root, "scripts", "test-production-evidence-manifest-release-guard.ps1"));
        var docs = File.ReadAllText(Path.Combine(root, "docs", "production-readiness-gate.md"));
        var roadmap = File.ReadAllText(Path.Combine(root, "docs", "PRODUCT_COMPLETION_ROADMAP.md"));

        foreach (var expected in new[]
                 {
                     "Assert-KnownReleaseId",
                     "ReleaseId must exist in backend/src/VpnPlatform.Api/AppReleases/releases.json",
                     "missing-release-id-for-regression"
                 })
        {
            Assert.Contains(expected, generator + regression, StringComparison.OrdinalIgnoreCase);
        }

        foreach (var expected in new[]
                 {
                     "Production evidence manifest generator accepted unknown releaseId",
                     "Production evidence manifest generator created manifest after unknown releaseId failure",
                     "production evidence manifest release guard valid"
                 })
        {
            Assert.Contains(expected, regression, StringComparison.OrdinalIgnoreCase);
        }

        Assert.Contains("test-production-evidence-manifest-release-guard.ps1", docs, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("[x] `P11-ACC-077`", roadmap, StringComparison.Ordinal);
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
    public void Production_Evidence_Archive_Should_Reject_Unknown_Release()
    {
        var root = FindRepositoryRoot();
        var generator = File.ReadAllText(Path.Combine(root, "scripts", "new-production-evidence-archive.ps1"));
        var regression = File.ReadAllText(Path.Combine(root, "scripts", "test-production-evidence-archive-release-guard.ps1"));
        var docs = File.ReadAllText(Path.Combine(root, "docs", "production-readiness-gate.md"));
        var roadmap = File.ReadAllText(Path.Combine(root, "docs", "PRODUCT_COMPLETION_ROADMAP.md"));

        foreach (var expected in new[]
                 {
                     "Assert-KnownReleaseId",
                     "ReleaseId must exist in backend/src/VpnPlatform.Api/AppReleases/releases.json",
                     "missing-release-id-for-archive-regression"
                 })
        {
            Assert.Contains(expected, generator + regression, StringComparison.OrdinalIgnoreCase);
        }

        foreach (var expected in new[]
                 {
                     "Production evidence archive generator accepted unknown releaseId",
                     "Production evidence archive generator created archive after unknown releaseId failure",
                     "production evidence archive release guard valid"
                 })
        {
            Assert.Contains(expected, regression, StringComparison.OrdinalIgnoreCase);
        }

        Assert.Contains("test-production-evidence-archive-release-guard.ps1", docs, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("[x] `P11-ACC-078`", roadmap, StringComparison.Ordinal);
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
    public void Production_Evidence_Handoff_Receipt_Should_Reject_Unknown_Release()
    {
        var root = FindRepositoryRoot();
        var generator = File.ReadAllText(Path.Combine(root, "scripts", "new-production-evidence-handoff-receipt.ps1"));
        var regression = File.ReadAllText(Path.Combine(root, "scripts", "test-production-evidence-handoff-receipt-release-guard.ps1"));
        var docs = File.ReadAllText(Path.Combine(root, "docs", "production-readiness-gate.md"));
        var roadmap = File.ReadAllText(Path.Combine(root, "docs", "PRODUCT_COMPLETION_ROADMAP.md"));

        foreach (var expected in new[]
                 {
                     "Assert-KnownReleaseId",
                     "ReleaseId must exist in backend/src/VpnPlatform.Api/AppReleases/releases.json",
                     "missing-release-id-for-receipt-regression"
                 })
        {
            Assert.Contains(expected, generator + regression, StringComparison.OrdinalIgnoreCase);
        }

        foreach (var expected in new[]
                 {
                     "Production evidence handoff receipt generator accepted unknown releaseId",
                     "Production evidence handoff receipt generator created receipt after unknown releaseId failure",
                     "Production evidence handoff receipt generator created markdown after unknown releaseId failure",
                     "production evidence handoff receipt release guard valid"
                 })
        {
            Assert.Contains(expected, regression, StringComparison.OrdinalIgnoreCase);
        }

        Assert.Contains("test-production-evidence-handoff-receipt-release-guard.ps1", docs, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("[x] `P11-ACC-079`", roadmap, StringComparison.Ordinal);
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
    public void Production_Evidence_Handoff_Receipt_Validator_Should_Reject_Tampered_Verified_Files()
    {
        var root = FindRepositoryRoot();
        var validator = File.ReadAllText(Path.Combine(root, "scripts", "validate-production-evidence-handoff-receipt.ps1"));
        var regression = File.ReadAllText(Path.Combine(root, "scripts", "test-production-evidence-handoff-receipt-verified-files-guard.ps1"));
        var docs = File.ReadAllText(Path.Combine(root, "docs", "production-readiness-gate.md"));
        var roadmap = File.ReadAllText(Path.Combine(root, "docs", "PRODUCT_COMPLETION_ROADMAP.md"));

        foreach (var expected in new[]
                 {
                     "verifiedFiles count does not match archive",
                     "verifiedFiles is missing archive entry",
                     "verifiedFiles contains duplicated entry",
                     "lengthBytes does not match archive",
                     "sha256 does not match archive"
                 })
        {
            Assert.Contains(expected, validator, StringComparison.OrdinalIgnoreCase);
        }

        foreach (var expected in new[]
                 {
                     "Production evidence handoff receipt validator accepted tampered verifiedFiles sha256",
                     "Production evidence handoff receipt verified file staging-smoke-report.json sha256 does not match archive",
                     "production evidence handoff receipt verified files guard valid"
                 })
        {
            Assert.Contains(expected, regression, StringComparison.OrdinalIgnoreCase);
        }

        Assert.Contains("test-production-evidence-handoff-receipt-verified-files-guard.ps1", docs, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("[x] `P11-ACC-081`", roadmap, StringComparison.Ordinal);
    }

    [Fact]
    public void Production_Evidence_Handoff_Receipt_Validator_Should_Reject_Tampered_Verified_Files_Markdown()
    {
        var root = FindRepositoryRoot();
        var validator = File.ReadAllText(Path.Combine(root, "scripts", "validate-production-evidence-handoff-receipt.ps1"));
        var regression = File.ReadAllText(Path.Combine(root, "scripts", "test-production-evidence-handoff-receipt-markdown-verified-files-guard.ps1"));
        var docs = File.ReadAllText(Path.Combine(root, "docs", "production-readiness-gate.md"));
        var roadmap = File.ReadAllText(Path.Combine(root, "docs", "PRODUCT_COMPLETION_ROADMAP.md"));

        foreach (var expected in new[]
                 {
                     "Production evidence handoff receipt markdown is missing verified file detail",
                     "file.name",
                     "file.entryName",
                     "file.sha256"
                 })
        {
            Assert.Contains(expected, validator, StringComparison.OrdinalIgnoreCase);
        }

        foreach (var expected in new[]
                 {
                     "Production evidence handoff receipt validator accepted tampered verifiedFiles markdown",
                     "Production evidence handoff receipt markdown is missing verified file detail",
                     "production evidence handoff receipt markdown verified files guard valid"
                 })
        {
            Assert.Contains(expected, regression, StringComparison.OrdinalIgnoreCase);
        }

        Assert.Contains("test-production-evidence-handoff-receipt-markdown-verified-files-guard.ps1", docs, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("[x] `P11-ACC-082`", roadmap, StringComparison.Ordinal);
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
    public void Production_Evidence_Handoff_Checklist_Should_Reject_Unknown_Release()
    {
        var root = FindRepositoryRoot();
        var generator = File.ReadAllText(Path.Combine(root, "scripts", "new-production-evidence-handoff-checklist.ps1"));
        var regression = File.ReadAllText(Path.Combine(root, "scripts", "test-production-evidence-handoff-checklist-release-guard.ps1"));
        var docs = File.ReadAllText(Path.Combine(root, "docs", "production-readiness-gate.md"));
        var roadmap = File.ReadAllText(Path.Combine(root, "docs", "PRODUCT_COMPLETION_ROADMAP.md"));

        foreach (var expected in new[]
                 {
                     "Assert-KnownReleaseId",
                     "ReleaseId must exist in backend/src/VpnPlatform.Api/AppReleases/releases.json",
                     "missing-release-id-for-checklist-regression"
                 })
        {
            Assert.Contains(expected, generator + regression, StringComparison.OrdinalIgnoreCase);
        }

        foreach (var expected in new[]
                 {
                     "Production evidence handoff checklist generator accepted unknown releaseId",
                     "Production evidence handoff checklist generator created checklist after unknown releaseId failure",
                     "Production evidence handoff checklist generator created markdown after unknown releaseId failure",
                     "production evidence handoff checklist release guard valid"
                 })
        {
            Assert.Contains(expected, regression, StringComparison.OrdinalIgnoreCase);
        }

        Assert.Contains("test-production-evidence-handoff-checklist-release-guard.ps1", docs, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("[x] `P11-ACC-080`", roadmap, StringComparison.Ordinal);
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
    public void Production_Evidence_Handoff_Checklist_Validator_Should_Reject_Tampered_Gate_Markdown()
    {
        var root = FindRepositoryRoot();
        var validator = File.ReadAllText(Path.Combine(root, "scripts", "validate-production-evidence-handoff-checklist.ps1"));
        var regression = File.ReadAllText(Path.Combine(root, "scripts", "test-production-evidence-handoff-checklist-markdown-gates-guard.ps1"));
        var docs = File.ReadAllText(Path.Combine(root, "docs", "production-readiness-gate.md"));
        var roadmap = File.ReadAllText(Path.Combine(root, "docs", "PRODUCT_COMPLETION_ROADMAP.md"));

        foreach (var expected in new[]
                 {
                     "Production evidence handoff checklist markdown is missing gate detail",
                     "Production evidence handoff checklist markdown is missing operator action",
                     "gate.name",
                     "gate.status",
                     "gate.message"
                 })
        {
            Assert.Contains(expected, validator, StringComparison.OrdinalIgnoreCase);
        }

        foreach (var expected in new[]
                 {
                     "Production evidence handoff checklist validator accepted tampered gate markdown",
                     "Production evidence handoff checklist markdown is missing gate detail",
                     "production evidence handoff checklist markdown gates guard valid"
                 })
        {
            Assert.Contains(expected, regression, StringComparison.OrdinalIgnoreCase);
        }

        Assert.Contains("test-production-evidence-handoff-checklist-markdown-gates-guard.ps1", docs, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("[x] `P11-ACC-083`", roadmap, StringComparison.Ordinal);
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
    public void Production_Evidence_Handoff_Package_Validator_Should_Reject_Tampered_File_Markdown()
    {
        var root = FindRepositoryRoot();
        var validator = File.ReadAllText(Path.Combine(root, "scripts", "validate-production-evidence-handoff-package.ps1"));
        var regression = File.ReadAllText(Path.Combine(root, "scripts", "test-production-evidence-handoff-package-markdown-files-guard.ps1"));
        var docs = File.ReadAllText(Path.Combine(root, "docs", "production-readiness-gate.md"));
        var roadmap = File.ReadAllText(Path.Combine(root, "docs", "PRODUCT_COMPLETION_ROADMAP.md"));

        foreach (var expected in new[]
                 {
                     "Production evidence handoff package markdown is missing file detail",
                     "file.fileName",
                     "file.lengthBytes",
                     "file.sha256"
                 })
        {
            Assert.Contains(expected, validator, StringComparison.OrdinalIgnoreCase);
        }

        foreach (var expected in new[]
                 {
                     "Production evidence handoff package validator accepted tampered package index markdown",
                     "Production evidence handoff package markdown is missing file detail",
                     "production evidence handoff package markdown files guard valid"
                 })
        {
            Assert.Contains(expected, regression, StringComparison.OrdinalIgnoreCase);
        }

        Assert.Contains("test-production-evidence-handoff-package-markdown-files-guard.ps1", docs, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("[x] `P11-ACC-084`", roadmap, StringComparison.Ordinal);
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
                     "Get-DefaultArchiveName",
                     "Get-TextSha256",
                     "Substring(0, 12)",
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
    public void Production_Evidence_Handoff_Package_Archive_Validator_Should_Reject_Duplicated_Entries()
    {
        var root = FindRepositoryRoot();
        var validator = File.ReadAllText(Path.Combine(root, "scripts", "validate-production-evidence-handoff-package-archive.ps1"));
        var regression = File.ReadAllText(Path.Combine(root, "scripts", "test-production-evidence-handoff-package-archive-duplicate-entry-guard.ps1"));
        var docs = File.ReadAllText(Path.Combine(root, "docs", "production-readiness-gate.md"));
        var roadmap = File.ReadAllText(Path.Combine(root, "docs", "PRODUCT_COMPLETION_ROADMAP.md"));

        foreach (var expected in new[]
                 {
                     "duplicated entry",
                     "seen.Add"
                 })
        {
            Assert.Contains(expected, validator, StringComparison.OrdinalIgnoreCase);
        }

        foreach (var expected in new[]
                 {
                     "New-DuplicateEntryArchive",
                     "ZipArchiveMode]::Create",
                     "SHA256SUMS.txt",
                     "Assert-FailsWith",
                     "validate-production-evidence-handoff-package-archive.ps1",
                     "duplicated entry",
                     "production evidence handoff package archive duplicate entry guard valid"
                 })
        {
            Assert.Contains(expected, regression, StringComparison.OrdinalIgnoreCase);
        }

        Assert.Contains("test-production-evidence-handoff-package-archive-duplicate-entry-guard.ps1", docs, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("[x] `P11-ACC-085`", roadmap, StringComparison.Ordinal);
    }

    [Fact]
    public void Production_Evidence_Handoff_Package_Archive_Validator_Should_Reject_Nested_Entries()
    {
        var root = FindRepositoryRoot();
        var validator = File.ReadAllText(Path.Combine(root, "scripts", "validate-production-evidence-handoff-package-archive.ps1"));
        var regression = File.ReadAllText(Path.Combine(root, "scripts", "test-production-evidence-handoff-package-archive-nested-entry-guard.ps1"));
        var docs = File.ReadAllText(Path.Combine(root, "docs", "production-readiness-gate.md"));
        var roadmap = File.ReadAllText(Path.Combine(root, "docs", "PRODUCT_COMPLETION_ROADMAP.md"));

        foreach (var expected in new[]
                 {
                     "entry.FullName -ne $entry.Name",
                     "unexpected entry"
                 })
        {
            Assert.Contains(expected, validator, StringComparison.OrdinalIgnoreCase);
        }

        foreach (var expected in new[]
                 {
                     "New-NestedEntryArchive",
                     "nested/SHA256SUMS.txt",
                     "Assert-FailsWith",
                     "validate-production-evidence-handoff-package-archive.ps1",
                     "unexpected entry",
                     "production evidence handoff package archive nested entry guard valid"
                 })
        {
            Assert.Contains(expected, regression, StringComparison.OrdinalIgnoreCase);
        }

        Assert.Contains("test-production-evidence-handoff-package-archive-nested-entry-guard.ps1", docs, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("[x] `P11-ACC-086`", roadmap, StringComparison.Ordinal);
    }

    [Fact]
    public void Production_Evidence_Handoff_Package_Archive_Validator_Should_Reject_Directory_Entries()
    {
        var root = FindRepositoryRoot();
        var validator = File.ReadAllText(Path.Combine(root, "scripts", "validate-production-evidence-handoff-package-archive.ps1"));
        var regression = File.ReadAllText(Path.Combine(root, "scripts", "test-production-evidence-handoff-package-archive-directory-entry-guard.ps1"));
        var docs = File.ReadAllText(Path.Combine(root, "docs", "production-readiness-gate.md"));
        var roadmap = File.ReadAllText(Path.Combine(root, "docs", "PRODUCT_COMPLETION_ROADMAP.md"));

        foreach (var expected in new[]
                 {
                     "IsNullOrWhiteSpace($entry.Name)",
                     "unexpected entry"
                 })
        {
            Assert.Contains(expected, validator, StringComparison.OrdinalIgnoreCase);
        }

        foreach (var expected in new[]
                 {
                     "New-DirectoryEntryArchive",
                     "empty-folder/",
                     "Assert-FailsWith",
                     "validate-production-evidence-handoff-package-archive.ps1",
                     "unexpected entry",
                     "production evidence handoff package archive directory entry guard valid"
                 })
        {
            Assert.Contains(expected, regression, StringComparison.OrdinalIgnoreCase);
        }

        Assert.Contains("test-production-evidence-handoff-package-archive-directory-entry-guard.ps1", docs, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("[x] `P11-ACC-087`", roadmap, StringComparison.Ordinal);
    }

    [Fact]
    public void Production_Evidence_Handoff_Package_Archive_Validator_Should_Reject_Backslash_Entries()
    {
        var root = FindRepositoryRoot();
        var validator = File.ReadAllText(Path.Combine(root, "scripts", "validate-production-evidence-handoff-package-archive.ps1"));
        var regression = File.ReadAllText(Path.Combine(root, "scripts", "test-production-evidence-handoff-package-archive-backslash-entry-guard.ps1"));
        var docs = File.ReadAllText(Path.Combine(root, "docs", "production-readiness-gate.md"));
        var roadmap = File.ReadAllText(Path.Combine(root, "docs", "PRODUCT_COMPLETION_ROADMAP.md"));

        foreach (var expected in new[]
                 {
                     "entry.FullName -ne $entry.Name",
                     "unexpected entry"
                 })
        {
            Assert.Contains(expected, validator, StringComparison.OrdinalIgnoreCase);
        }

        foreach (var expected in new[]
                 {
                     "New-BackslashEntryArchive",
                     "nested\\SHA256SUMS.txt",
                     "Assert-FailsWith",
                     "validate-production-evidence-handoff-package-archive.ps1",
                     "unexpected entry",
                     "production evidence handoff package archive backslash entry guard valid"
                 })
        {
            Assert.Contains(expected, regression, StringComparison.OrdinalIgnoreCase);
        }

        Assert.Contains("test-production-evidence-handoff-package-archive-backslash-entry-guard.ps1", docs, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("[x] `P11-ACC-088`", roadmap, StringComparison.Ordinal);
    }

    [Fact]
    public void Production_Evidence_Handoff_Package_Archive_Validator_Should_Reject_DotDot_Entries()
    {
        var root = FindRepositoryRoot();
        var validator = File.ReadAllText(Path.Combine(root, "scripts", "validate-production-evidence-handoff-package-archive.ps1"));
        var regression = File.ReadAllText(Path.Combine(root, "scripts", "test-production-evidence-handoff-package-archive-dotdot-entry-guard.ps1"));
        var docs = File.ReadAllText(Path.Combine(root, "docs", "production-readiness-gate.md"));
        var roadmap = File.ReadAllText(Path.Combine(root, "docs", "PRODUCT_COMPLETION_ROADMAP.md"));

        foreach (var expected in new[]
                 {
                     "EntryName -eq \"..\"",
                     "entry must be a file name only"
                 })
        {
            Assert.Contains(expected, validator, StringComparison.OrdinalIgnoreCase);
        }

        foreach (var expected in new[]
                 {
                     "New-DotDotEntryArchive",
                     "EntryName \"..\"",
                     "Assert-FailsWith",
                     "validate-production-evidence-handoff-package-archive.ps1",
                     "file name only",
                     "production evidence handoff package archive dotdot entry guard valid"
                 })
        {
            Assert.Contains(expected, regression, StringComparison.OrdinalIgnoreCase);
        }

        Assert.Contains("test-production-evidence-handoff-package-archive-dotdot-entry-guard.ps1", docs, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("[x] `P11-ACC-089`", roadmap, StringComparison.Ordinal);
    }

    [Fact]
    public void Production_Evidence_Handoff_Package_Archive_Validator_Should_Reject_Dot_Entries()
    {
        var root = FindRepositoryRoot();
        var validator = File.ReadAllText(Path.Combine(root, "scripts", "validate-production-evidence-handoff-package-archive.ps1"));
        var regression = File.ReadAllText(Path.Combine(root, "scripts", "test-production-evidence-handoff-package-archive-dot-entry-guard.ps1"));
        var docs = File.ReadAllText(Path.Combine(root, "docs", "production-readiness-gate.md"));
        var roadmap = File.ReadAllText(Path.Combine(root, "docs", "PRODUCT_COMPLETION_ROADMAP.md"));

        foreach (var expected in new[]
                 {
                     "EntryName -eq \".\"",
                     "entry must be a file name only"
                 })
        {
            Assert.Contains(expected, validator, StringComparison.OrdinalIgnoreCase);
        }

        foreach (var expected in new[]
                 {
                     "New-DotEntryArchive",
                     "EntryName \".\"",
                     "Assert-FailsWith",
                     "validate-production-evidence-handoff-package-archive.ps1",
                     "file name only",
                     "production evidence handoff package archive dot entry guard valid"
                 })
        {
            Assert.Contains(expected, regression, StringComparison.OrdinalIgnoreCase);
        }

        Assert.Contains("test-production-evidence-handoff-package-archive-dot-entry-guard.ps1", docs, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("[x] `P11-ACC-090`", roadmap, StringComparison.Ordinal);
    }

    [Fact]
    public void Production_Evidence_Handoff_Package_Archive_Validator_Should_Reject_Rooted_Entries()
    {
        var root = FindRepositoryRoot();
        var validator = File.ReadAllText(Path.Combine(root, "scripts", "validate-production-evidence-handoff-package-archive.ps1"));
        var regression = File.ReadAllText(Path.Combine(root, "scripts", "test-production-evidence-handoff-package-archive-rooted-entry-guard.ps1"));
        var docs = File.ReadAllText(Path.Combine(root, "docs", "production-readiness-gate.md"));
        var roadmap = File.ReadAllText(Path.Combine(root, "docs", "PRODUCT_COMPLETION_ROADMAP.md"));

        foreach (var expected in new[]
                 {
                     "entry.FullName -ne $entry.Name",
                     "unexpected entry"
                 })
        {
            Assert.Contains(expected, validator, StringComparison.OrdinalIgnoreCase);
        }

        foreach (var expected in new[]
                 {
                     "New-RootedEntryArchive",
                     "C:\\SHA256SUMS.txt",
                     "Assert-FailsWith",
                     "validate-production-evidence-handoff-package-archive.ps1",
                     "unexpected entry",
                     "production evidence handoff package archive rooted entry guard valid"
                 })
        {
            Assert.Contains(expected, regression, StringComparison.OrdinalIgnoreCase);
        }

        Assert.Contains("test-production-evidence-handoff-package-archive-rooted-entry-guard.ps1", docs, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("[x] `P11-ACC-091`", roadmap, StringComparison.Ordinal);
    }

    [Fact]
    public void Production_Evidence_Handoff_Package_Archive_Validator_Should_Reject_Whitespace_Entries()
    {
        var root = FindRepositoryRoot();
        var validator = File.ReadAllText(Path.Combine(root, "scripts", "validate-production-evidence-handoff-package-archive.ps1"));
        var regression = File.ReadAllText(Path.Combine(root, "scripts", "test-production-evidence-handoff-package-archive-whitespace-entry-guard.ps1"));
        var docs = File.ReadAllText(Path.Combine(root, "docs", "production-readiness-gate.md"));
        var roadmap = File.ReadAllText(Path.Combine(root, "docs", "PRODUCT_COMPLETION_ROADMAP.md"));

        foreach (var expected in new[]
                 {
                     "IsNullOrWhiteSpace($entry.Name)",
                     "unexpected entry"
                 })
        {
            Assert.Contains(expected, validator, StringComparison.OrdinalIgnoreCase);
        }

        foreach (var expected in new[]
                 {
                     "New-WhitespaceEntryArchive",
                     "Add-ZipTextEntry -Archive $archive -EntryName \" \"",
                     "Assert-FailsWith",
                     "validate-production-evidence-handoff-package-archive.ps1",
                     "unexpected entry",
                     "production evidence handoff package archive whitespace entry guard valid"
                 })
        {
            Assert.Contains(expected, regression, StringComparison.OrdinalIgnoreCase);
        }

        Assert.Contains("test-production-evidence-handoff-package-archive-whitespace-entry-guard.ps1", docs, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("[x] `P11-ACC-092`", roadmap, StringComparison.Ordinal);
    }

    [Fact]
    public void Production_Evidence_Handoff_Package_Archive_Validator_Should_Reject_Entry_Case_Mismatches()
    {
        var root = FindRepositoryRoot();
        var validator = File.ReadAllText(Path.Combine(root, "scripts", "validate-production-evidence-handoff-package-archive.ps1"));
        var regression = File.ReadAllText(Path.Combine(root, "scripts", "test-production-evidence-handoff-package-archive-entry-case-guard.ps1"));
        var docs = File.ReadAllText(Path.Combine(root, "docs", "production-readiness-gate.md"));
        var roadmap = File.ReadAllText(Path.Combine(root, "docs", "PRODUCT_COMPLETION_ROADMAP.md"));

        foreach (var expected in new[]
                 {
                     "[StringComparer]::Ordinal",
                     "unexpected entry"
                 })
        {
            Assert.Contains(expected, validator, StringComparison.OrdinalIgnoreCase);
        }

        Assert.DoesNotContain("[StringComparer]::OrdinalIgnoreCase", validator, StringComparison.OrdinalIgnoreCase);

        foreach (var expected in new[]
                 {
                     "New-CaseMismatchArchive",
                     "sha256sums.txt",
                     "Assert-FailsWith",
                     "validate-production-evidence-handoff-package-archive.ps1",
                     "unexpected entry",
                     "production evidence handoff package archive entry case guard valid"
                 })
        {
            Assert.Contains(expected, regression, StringComparison.OrdinalIgnoreCase);
        }

        Assert.Contains("test-production-evidence-handoff-package-archive-entry-case-guard.ps1", docs, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("[x] `P11-ACC-093`", roadmap, StringComparison.Ordinal);
    }

    [Fact]
    public void Production_Evidence_Handoff_Package_Archive_Entry_Case_Guard_Should_Cleanup_Artifacts()
    {
        var root = FindRepositoryRoot();
        var regression = File.ReadAllText(Path.Combine(root, "scripts", "test-production-evidence-handoff-package-archive-entry-case-guard.ps1"));
        var roadmap = File.ReadAllText(Path.Combine(root, "docs", "PRODUCT_COMPLETION_ROADMAP.md"));

        foreach (var expected in new[]
                 {
                     "Remove-EmptyDirectory",
                     "Get-ChildItem -LiteralPath $DirectoryPath -Force",
                     "Remove-Item -LiteralPath $archivePath -Force",
                     "Remove-EmptyDirectory -DirectoryPath $tmpDirectory"
                 })
        {
            Assert.Contains(expected, regression, StringComparison.OrdinalIgnoreCase);
        }

        Assert.Contains("[x] `P11-ACC-094`", roadmap, StringComparison.Ordinal);
    }

    [Fact]
    public void Production_Evidence_Handoff_Package_Archive_Duplicate_Entry_Guard_Should_Cleanup_Artifacts()
    {
        var root = FindRepositoryRoot();
        var regression = File.ReadAllText(Path.Combine(root, "scripts", "test-production-evidence-handoff-package-archive-duplicate-entry-guard.ps1"));
        var roadmap = File.ReadAllText(Path.Combine(root, "docs", "PRODUCT_COMPLETION_ROADMAP.md"));

        foreach (var expected in new[]
                 {
                     "Remove-EmptyDirectory",
                     "Get-ChildItem -LiteralPath $DirectoryPath -Force",
                     "Remove-Item -LiteralPath $archivePath -Force",
                     "Remove-EmptyDirectory -DirectoryPath $tmpDirectory"
                 })
        {
            Assert.Contains(expected, regression, StringComparison.OrdinalIgnoreCase);
        }

        Assert.Contains("[x] `P11-ACC-095`", roadmap, StringComparison.Ordinal);
    }

    [Fact]
    public void Production_Evidence_Handoff_Package_Archive_Nested_Entry_Guard_Should_Cleanup_Artifacts()
    {
        var root = FindRepositoryRoot();
        var regression = File.ReadAllText(Path.Combine(root, "scripts", "test-production-evidence-handoff-package-archive-nested-entry-guard.ps1"));
        var roadmap = File.ReadAllText(Path.Combine(root, "docs", "PRODUCT_COMPLETION_ROADMAP.md"));

        foreach (var expected in new[]
                 {
                     "Remove-EmptyDirectory",
                     "Get-ChildItem -LiteralPath $DirectoryPath -Force",
                     "Remove-Item -LiteralPath $archivePath -Force",
                     "Remove-EmptyDirectory -DirectoryPath $tmpDirectory"
                 })
        {
            Assert.Contains(expected, regression, StringComparison.OrdinalIgnoreCase);
        }

        Assert.Contains("[x] `P11-ACC-096`", roadmap, StringComparison.Ordinal);
    }

    [Fact]
    public void Production_Evidence_Handoff_Package_Archive_Directory_Entry_Guard_Should_Cleanup_Artifacts()
    {
        var root = FindRepositoryRoot();
        var regression = File.ReadAllText(Path.Combine(root, "scripts", "test-production-evidence-handoff-package-archive-directory-entry-guard.ps1"));
        var roadmap = File.ReadAllText(Path.Combine(root, "docs", "PRODUCT_COMPLETION_ROADMAP.md"));

        foreach (var expected in new[]
                 {
                     "Remove-EmptyDirectory",
                     "Get-ChildItem -LiteralPath $DirectoryPath -Force",
                     "Remove-Item -LiteralPath $archivePath -Force",
                     "Remove-EmptyDirectory -DirectoryPath $tmpDirectory"
                 })
        {
            Assert.Contains(expected, regression, StringComparison.OrdinalIgnoreCase);
        }

        Assert.Contains("[x] `P11-ACC-097`", roadmap, StringComparison.Ordinal);
    }

    [Fact]
    public void Production_Evidence_Handoff_Package_Archive_Backslash_Entry_Guard_Should_Cleanup_Artifacts()
    {
        var root = FindRepositoryRoot();
        var regression = File.ReadAllText(Path.Combine(root, "scripts", "test-production-evidence-handoff-package-archive-backslash-entry-guard.ps1"));
        var roadmap = File.ReadAllText(Path.Combine(root, "docs", "PRODUCT_COMPLETION_ROADMAP.md"));

        foreach (var expected in new[]
                 {
                     "Remove-EmptyDirectory",
                     "Get-ChildItem -LiteralPath $DirectoryPath -Force",
                     "Remove-Item -LiteralPath $archivePath -Force",
                     "Remove-EmptyDirectory -DirectoryPath $tmpDirectory"
                 })
        {
            Assert.Contains(expected, regression, StringComparison.OrdinalIgnoreCase);
        }

        Assert.Contains("[x] `P11-ACC-098`", roadmap, StringComparison.Ordinal);
    }

    [Fact]
    public void Production_Evidence_Handoff_Package_Archive_DotDot_Entry_Guard_Should_Cleanup_Artifacts()
    {
        var root = FindRepositoryRoot();
        var regression = File.ReadAllText(Path.Combine(root, "scripts", "test-production-evidence-handoff-package-archive-dotdot-entry-guard.ps1"));
        var roadmap = File.ReadAllText(Path.Combine(root, "docs", "PRODUCT_COMPLETION_ROADMAP.md"));

        foreach (var expected in new[]
                 {
                     "Remove-EmptyDirectory",
                     "Get-ChildItem -LiteralPath $DirectoryPath -Force",
                     "Remove-Item -LiteralPath $archivePath -Force",
                     "Remove-EmptyDirectory -DirectoryPath $tmpDirectory"
                 })
        {
            Assert.Contains(expected, regression, StringComparison.OrdinalIgnoreCase);
        }

        Assert.Contains("[x] `P11-ACC-099`", roadmap, StringComparison.Ordinal);
    }

    [Fact]
    public void Production_Evidence_Handoff_Package_Archive_Dot_Entry_Guard_Should_Cleanup_Artifacts()
    {
        var root = FindRepositoryRoot();
        var regression = File.ReadAllText(Path.Combine(root, "scripts", "test-production-evidence-handoff-package-archive-dot-entry-guard.ps1"));
        var roadmap = File.ReadAllText(Path.Combine(root, "docs", "PRODUCT_COMPLETION_ROADMAP.md"));

        foreach (var expected in new[]
                 {
                     "Remove-EmptyDirectory",
                     "Get-ChildItem -LiteralPath $DirectoryPath -Force",
                     "Remove-Item -LiteralPath $archivePath -Force",
                     "Remove-EmptyDirectory -DirectoryPath $tmpDirectory"
                 })
        {
            Assert.Contains(expected, regression, StringComparison.OrdinalIgnoreCase);
        }

        Assert.Contains("[x] `P11-ACC-100`", roadmap, StringComparison.Ordinal);
    }

    [Fact]
    public void Production_Evidence_Handoff_Package_Archive_Rooted_Entry_Guard_Should_Cleanup_Artifacts()
    {
        var root = FindRepositoryRoot();
        var regression = File.ReadAllText(Path.Combine(root, "scripts", "test-production-evidence-handoff-package-archive-rooted-entry-guard.ps1"));
        var roadmap = File.ReadAllText(Path.Combine(root, "docs", "PRODUCT_COMPLETION_ROADMAP.md"));

        foreach (var expected in new[]
                 {
                     "Remove-EmptyDirectory",
                     "Get-ChildItem -LiteralPath $DirectoryPath -Force",
                     "Remove-Item -LiteralPath $archivePath -Force",
                     "Remove-EmptyDirectory -DirectoryPath $tmpDirectory"
                 })
        {
            Assert.Contains(expected, regression, StringComparison.OrdinalIgnoreCase);
        }

        Assert.Contains("[x] `P11-ACC-101`", roadmap, StringComparison.Ordinal);
    }

    [Fact]
    public void Production_Evidence_Handoff_Package_Archive_Whitespace_Entry_Guard_Should_Cleanup_Artifacts()
    {
        var root = FindRepositoryRoot();
        var regression = File.ReadAllText(Path.Combine(root, "scripts", "test-production-evidence-handoff-package-archive-whitespace-entry-guard.ps1"));
        var roadmap = File.ReadAllText(Path.Combine(root, "docs", "PRODUCT_COMPLETION_ROADMAP.md"));

        foreach (var expected in new[]
                 {
                     "Remove-EmptyDirectory",
                     "Get-ChildItem -LiteralPath $DirectoryPath -Force",
                     "Remove-Item -LiteralPath $archivePath -Force",
                     "Remove-EmptyDirectory -DirectoryPath $tmpDirectory"
                 })
        {
            Assert.Contains(expected, regression, StringComparison.OrdinalIgnoreCase);
        }

        Assert.Contains("[x] `P11-ACC-102`", roadmap, StringComparison.Ordinal);
    }

    [Fact]
    public void Production_Evidence_Handoff_Package_Archive_Entry_Guards_Should_All_Cleanup_Artifacts()
    {
        var root = FindRepositoryRoot();
        var scriptDirectory = Path.Combine(root, "scripts");
        var roadmap = File.ReadAllText(Path.Combine(root, "docs", "PRODUCT_COMPLETION_ROADMAP.md"));
        var scripts = Directory
            .EnumerateFiles(scriptDirectory, "test-production-evidence-handoff-package-archive-*-entry-guard.ps1")
            .OrderBy(Path.GetFileName, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        Assert.Equal(8, scripts.Length);

        foreach (var scriptPath in scripts)
        {
            var regression = File.ReadAllText(scriptPath);

            foreach (var expected in new[]
                     {
                         "Remove-EmptyDirectory",
                         "Get-ChildItem -LiteralPath $DirectoryPath -Force",
                         "Remove-Item -LiteralPath $archivePath -Force",
                         "Remove-EmptyDirectory -DirectoryPath $tmpDirectory"
                     })
            {
                Assert.Contains(expected, regression, StringComparison.OrdinalIgnoreCase);
            }
        }

        Assert.Contains("[x] `P11-ACC-103`", roadmap, StringComparison.Ordinal);
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
    public void Production_Evidence_Handoff_Package_Archive_Flow_Should_Cleanup_Default_Output()
    {
        var root = FindRepositoryRoot();
        var script = File.ReadAllText(Path.Combine(root, "scripts", "test-production-evidence-handoff-package-archive-flow.ps1"));
        var docs = File.ReadAllText(Path.Combine(root, "docs", "production-readiness-gate.md"));
        var roadmap = File.ReadAllText(Path.Combine(root, "docs", "PRODUCT_COMPLETION_ROADMAP.md"));

        foreach (var expected in new[]
                 {
                     "$usingDefaultOutputDirectory",
                     "$shouldCleanupGeneratedOutput",
                     "$usingDefaultOutputDirectory -and -not $WriteJson",
                     "Remove-Item -LiteralPath $bundleDirectory -Recurse -Force",
                     "Get-ChildItem -LiteralPath $tmpDirectory -Force",
                     "Remove-Item -LiteralPath $tmpDirectory -Force"
                 })
        {
            Assert.Contains(expected, script, StringComparison.OrdinalIgnoreCase);
        }

        Assert.Contains("Default flow run removes its autogenerated `tmp/production-evidence-handoff-package-archive-flow-test` output", docs, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("[x] `P11-ACC-104`", roadmap, StringComparison.Ordinal);
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
    public void Production_Evidence_Handoff_Package_Archive_Flow_Result_Validator_Should_Verify_Result_Artifacts()
    {
        var root = FindRepositoryRoot();
        var validator = File.ReadAllText(Path.Combine(root, "scripts", "validate-production-evidence-handoff-package-archive-flow-result.ps1"));
        var flow = File.ReadAllText(Path.Combine(root, "scripts", "test-production-evidence-handoff-package-archive-flow.ps1"));
        var docs = File.ReadAllText(Path.Combine(root, "docs", "production-readiness-gate.md"));
        var roadmap = File.ReadAllText(Path.Combine(root, "docs", "PRODUCT_COMPLETION_ROADMAP.md"));

        foreach (var expected in new[]
                 {
                     "ResultJsonPath",
                     "ResultMarkdownPath",
                     "productionEvidenceArchiveSha256",
                     "handoffPackageArchiveSha256",
                     "regressionStatus must be passed",
                     "testedFailures",
                     "wrong-expected-sha256",
                     "unexpected-entry",
                     "missing-required-entry",
                     "validate-production-evidence-handoff-package-archive.ps1",
                     "production evidence handoff package archive flow result valid"
                 })
        {
            Assert.Contains(expected, validator, StringComparison.OrdinalIgnoreCase);
        }

        Assert.Contains("validate-production-evidence-handoff-package-archive-flow-result.ps1", flow, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("validate-production-evidence-handoff-package-archive-flow-result.ps1", docs, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("[x] `P11-ACC-031`", roadmap, StringComparison.Ordinal);
    }

    [Fact]
    public void Production_Evidence_Handoff_Package_Archive_Flow_Result_Validator_Should_Have_Regression_Harness()
    {
        var root = FindRepositoryRoot();
        var script = File.ReadAllText(Path.Combine(root, "scripts", "test-production-evidence-handoff-package-archive-flow-result-validator.ps1"));
        var docs = File.ReadAllText(Path.Combine(root, "docs", "production-readiness-gate.md"));
        var roadmap = File.ReadAllText(Path.Combine(root, "docs", "PRODUCT_COMPLETION_ROADMAP.md"));

        foreach (var expected in new[]
                 {
                     "validate-production-evidence-handoff-package-archive-flow-result.ps1",
                     "bad-status",
                     "bad-handoff-archive-sha256",
                     "missing-regression-failure",
                     "bad-markdown",
                     "status must be passed",
                     "SHA256 does not match",
                     "missing regression failure",
                     "markdown is missing",
                     "production evidence handoff package archive flow result validator regression passed"
                 })
        {
            Assert.Contains(expected, script, StringComparison.OrdinalIgnoreCase);
        }

        Assert.Contains("test-production-evidence-handoff-package-archive-flow-result-validator.ps1", docs, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("[x] `P11-ACC-032`", roadmap, StringComparison.Ordinal);
    }

    [Fact]
    public void Production_Evidence_Handoff_Package_Archive_Long_Path_Should_Have_Regression_Harness()
    {
        var root = FindRepositoryRoot();
        var script = File.ReadAllText(Path.Combine(root, "scripts", "test-production-evidence-handoff-package-archive-long-path.ps1"));
        var docs = File.ReadAllText(Path.Combine(root, "docs", "production-readiness-gate.md"));
        var roadmap = File.ReadAllText(Path.Combine(root, "docs", "PRODUCT_COMPLETION_ROADMAP.md"));

        foreach (var expected in new[]
                 {
                     "test-production-evidence-handoff-package-archive-flow.ps1",
                     "production-evidence-handoff-package-archive-long-release-id-path-regression-test",
                     "Get-TextSha256",
                     "Substring(0, 12)",
                     "archive name still contains full release id",
                     "archive name does not contain expected release hash",
                     "archive name is too long",
                     "result JSON lost full release id",
                     "production evidence handoff package archive long path regression passed"
                 })
        {
            Assert.Contains(expected, script, StringComparison.OrdinalIgnoreCase);
        }

        Assert.Contains("test-production-evidence-handoff-package-archive-long-path.ps1", docs, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("[x] `P11-ACC-033`", roadmap, StringComparison.Ordinal);
    }

    [Fact]
    public void Production_Evidence_Handoff_Package_Archive_Ci_Regression_Should_Run_All_Local_Harnesses()
    {
        var root = FindRepositoryRoot();
        var script = File.ReadAllText(Path.Combine(root, "scripts", "test-production-evidence-handoff-package-archive-ci-regression.ps1"));
        var docs = File.ReadAllText(Path.Combine(root, "docs", "production-readiness-gate.md"));
        var roadmap = File.ReadAllText(Path.Combine(root, "docs", "PRODUCT_COMPLETION_ROADMAP.md"));

        foreach (var expected in new[]
                 {
                     "test-production-evidence-handoff-package-archive-flow.ps1",
                     "test-production-evidence-handoff-package-archive-flow-result-validator.ps1",
                     "test-production-evidence-handoff-package-archive-long-path.ps1",
                     "test-production-evidence-handoff-package-archive-ci-summary-validator.ps1",
                     "validate-production-evidence-handoff-package-archive-ci-regression-result.ps1",
                     "test-production-evidence-handoff-package-archive-ci-regression-result-validator.ps1",
                     "production-evidence-handoff-package-archive-ci-regression-result.json",
                     "production-evidence-handoff-package-archive-ci-regression-result.md",
                     "ConvertTo-CiMarkdown",
                     "mainFlow",
                     "resultValidatorRegression",
                     "longPathRegression",
                     "ciSummaryValidatorRegression",
                     "ciResultValidatorRegression",
                     "production evidence handoff package archive CI regression passed"
                 })
        {
            Assert.Contains(expected, script, StringComparison.OrdinalIgnoreCase);
        }

        Assert.Contains("test-production-evidence-handoff-package-archive-ci-regression.ps1", docs, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("[x] `P11-ACC-034`", roadmap, StringComparison.Ordinal);
    }

    [Fact]
    public void Production_Evidence_Handoff_Package_Archive_Ci_Regression_Should_Cleanup_Default_Output()
    {
        var root = FindRepositoryRoot();
        var script = File.ReadAllText(Path.Combine(root, "scripts", "test-production-evidence-handoff-package-archive-ci-regression.ps1"));
        var docs = File.ReadAllText(Path.Combine(root, "docs", "production-readiness-gate.md"));
        var roadmap = File.ReadAllText(Path.Combine(root, "docs", "PRODUCT_COMPLETION_ROADMAP.md"));

        foreach (var expected in new[]
                 {
                     "$usingDefaultOutputDirectory",
                     "$shouldCleanupGeneratedOutput",
                     "$usingDefaultOutputDirectory -and -not $WriteJson",
                     "Remove-Item -LiteralPath $fullOutputDirectory -Recurse -Force",
                     "Get-ChildItem -LiteralPath $tmpDirectory -Force",
                     "Remove-Item -LiteralPath $tmpDirectory -Force"
                 })
        {
            Assert.Contains(expected, script, StringComparison.OrdinalIgnoreCase);
        }

        Assert.Contains("Default CI regression run removes its autogenerated `tmp/production-evidence-handoff-package-archive-ci-regression-test` output", docs, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("[x] `P11-ACC-105`", roadmap, StringComparison.Ordinal);
    }

    [Fact]
    public void Production_Evidence_Handoff_Package_Archive_Ci_Regression_Should_Run_In_GitHub_Actions()
    {
        var root = FindRepositoryRoot();
        var workflow = File.ReadAllText(Path.Combine(root, ".github", "workflows", "ci.yml"));
        var script = File.ReadAllText(Path.Combine(root, "scripts", "test-production-evidence-handoff-package-archive-ci-regression.ps1"));
        var docs = File.ReadAllText(Path.Combine(root, "docs", "production-readiness-gate.md"));
        var roadmap = File.ReadAllText(Path.Combine(root, "docs", "PRODUCT_COMPLETION_ROADMAP.md"));

        foreach (var expected in new[]
                 {
                     "production-evidence:",
                     "production evidence handoff archive regression",
                     "needs: backend",
                     "shell: pwsh",
                     "test-production-evidence-handoff-package-archive-ci-regression.ps1",
                     "tmp/production-evidence-handoff-package-archive-ci-regression",
                     "actions/upload-artifact@v4",
                     "production-evidence-handoff-package-archive-ci-regression-result.json",
                     "production-evidence-handoff-package-archive-ci-regression-result.md",
                     "if-no-files-found: error",
                     "GITHUB_STEP_SUMMARY"
                 })
        {
            Assert.Contains(expected, workflow + script, StringComparison.OrdinalIgnoreCase);
        }

        Assert.Contains("GitHub Actions", docs, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("production-evidence", docs, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("[x] `P11-ACC-035`", roadmap, StringComparison.Ordinal);
    }

    [Fact]
    public void Production_Evidence_Handoff_Package_Archive_Ci_Workflow_Should_Run_Workflow_Artifacts_Guard()
    {
        var root = FindRepositoryRoot();
        var workflowGuard = File.ReadAllText(Path.Combine(root, "scripts", "test-production-evidence-handoff-package-archive-ci-workflow-artifacts.ps1"));
        var workflow = File.ReadAllText(Path.Combine(root, ".github", "workflows", "ci.yml"));
        var docs = File.ReadAllText(Path.Combine(root, "docs", "production-readiness-gate.md"));
        var roadmap = File.ReadAllText(Path.Combine(root, "docs", "PRODUCT_COMPLETION_ROADMAP.md"));

        foreach (var expected in new[]
                 {
                     "production-evidence:",
                     "needs: backend",
                     "Guard production evidence workflow artifacts",
                     "test-production-evidence-handoff-package-archive-ci-workflow-artifacts.ps1 -WriteJson",
                     "test-production-evidence-handoff-package-archive-ci-regression.ps1",
                     "actions/upload-artifact@v4",
                     "if-no-files-found: error",
                     "production-evidence-handoff-package-archive-ci-regression-result.json",
                     "production-evidence-handoff-package-archive-ci-regression-result.md",
                     "production evidence CI workflow artifacts passed"
                 })
        {
            Assert.Contains(expected, workflowGuard, StringComparison.OrdinalIgnoreCase);
        }

        var guardStepIndex = workflow.IndexOf(
            "Guard production evidence workflow artifacts",
            StringComparison.OrdinalIgnoreCase);
        var guardCommandIndex = workflow.IndexOf(
            "test-production-evidence-handoff-package-archive-ci-workflow-artifacts.ps1 -WriteJson",
            StringComparison.OrdinalIgnoreCase);
        var wrapperIndex = workflow.IndexOf(
            "Run production evidence handoff archive CI regression",
            StringComparison.OrdinalIgnoreCase);
        var uploadIndex = workflow.IndexOf(
            "Upload production evidence regression artifacts",
            StringComparison.OrdinalIgnoreCase);

        Assert.True(guardStepIndex >= 0, "Production evidence workflow guard step must be present in CI.");
        Assert.True(guardCommandIndex > guardStepIndex, "Production evidence workflow guard command must be in the guard step.");
        Assert.True(guardCommandIndex < wrapperIndex, "Production evidence workflow artifacts guard must run before the CI wrapper.");
        Assert.True(wrapperIndex < uploadIndex, "Production evidence CI wrapper must still run before artifacts upload.");
        Assert.Contains("test-production-evidence-handoff-package-archive-ci-workflow-artifacts.ps1", docs, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("[x] `P11-ACC-054`", roadmap, StringComparison.Ordinal);
    }

    [Fact]
    public void Production_Evidence_Handoff_Package_Archive_Ci_Workflow_Guard_Should_Have_Fail_Closed_Regression()
    {
        var root = FindRepositoryRoot();
        var harness = File.ReadAllText(Path.Combine(root, "scripts", "test-production-evidence-handoff-package-archive-ci-workflow-artifacts-validator.ps1"));
        var docs = File.ReadAllText(Path.Combine(root, "docs", "production-readiness-gate.md"));
        var roadmap = File.ReadAllText(Path.Combine(root, "docs", "PRODUCT_COMPLETION_ROADMAP.md"));

        foreach (var expected in new[]
                 {
                     "test-production-evidence-handoff-package-archive-ci-workflow-artifacts.ps1",
                     "missing-guard-step",
                     "missing-result-json-artifact",
                     "bad-artifact-name",
                     "missing-if-no-files-found-error",
                     "testedFailures",
                     "production evidence CI workflow artifacts guard validator passed"
                 })
        {
            Assert.Contains(expected, harness, StringComparison.OrdinalIgnoreCase);
        }

        Assert.Contains("test-production-evidence-handoff-package-archive-ci-workflow-artifacts-validator.ps1", docs, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("[x] `P11-ACC-055`", roadmap, StringComparison.Ordinal);
    }

    [Fact]
    public void Production_Evidence_Handoff_Package_Archive_Ci_Regression_Should_Write_GitHub_Step_Summary()
    {
        var root = FindRepositoryRoot();
        var script = File.ReadAllText(Path.Combine(root, "scripts", "test-production-evidence-handoff-package-archive-ci-regression.ps1"));
        var docs = File.ReadAllText(Path.Combine(root, "docs", "production-readiness-gate.md"));
        var roadmap = File.ReadAllText(Path.Combine(root, "docs", "PRODUCT_COMPLETION_ROADMAP.md"));

        foreach (var expected in new[]
                 {
                     "Add-GitHubStepSummary",
                     "$env:GITHUB_STEP_SUMMARY",
                     "AppendAllText",
                     "ConvertTo-CiMarkdown",
                     "Production evidence handoff package archive CI regression",
                     "Main flow status",
                     "Result validator regression",
                     "Long path regression"
                 })
        {
            Assert.Contains(expected, script, StringComparison.OrdinalIgnoreCase);
        }

        Assert.Contains("GITHUB_STEP_SUMMARY", docs, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("[x] `P11-ACC-036`", roadmap, StringComparison.Ordinal);
    }

    [Fact]
    public void Production_Evidence_Handoff_Package_Archive_Ci_Summary_Should_Have_Fail_Closed_Validator()
    {
        var root = FindRepositoryRoot();
        var validator = File.ReadAllText(Path.Combine(root, "scripts", "validate-production-evidence-handoff-package-archive-ci-summary.ps1"));
        var wrapper = File.ReadAllText(Path.Combine(root, "scripts", "test-production-evidence-handoff-package-archive-ci-regression.ps1"));
        var docs = File.ReadAllText(Path.Combine(root, "docs", "production-readiness-gate.md"));
        var roadmap = File.ReadAllText(Path.Combine(root, "docs", "PRODUCT_COMPLETION_ROADMAP.md"));

        foreach (var expected in new[]
                 {
                     "ResultJsonPath",
                     "SummaryPath",
                     "must be passed",
                     "releaseId is required",
                     "Production evidence handoff package archive CI regression",
                     "Main flow status",
                     "Result validator regression",
                     "Long path regression",
                     "CI regression JSON",
                     "CI regression Markdown",
                     "production evidence handoff package archive CI summary valid"
                 })
        {
            Assert.Contains(expected, validator, StringComparison.OrdinalIgnoreCase);
        }

        Assert.Contains("validate-production-evidence-handoff-package-archive-ci-summary.ps1", wrapper, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("validate-production-evidence-handoff-package-archive-ci-summary.ps1", docs, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("[x] `P11-ACC-037`", roadmap, StringComparison.Ordinal);
    }

    [Fact]
    public void Production_Evidence_Handoff_Package_Archive_Ci_Summary_Validator_Should_Have_Regression_Harness()
    {
        var root = FindRepositoryRoot();
        var script = File.ReadAllText(Path.Combine(root, "scripts", "test-production-evidence-handoff-package-archive-ci-summary-validator.ps1"));
        var wrapper = File.ReadAllText(Path.Combine(root, "scripts", "test-production-evidence-handoff-package-archive-ci-regression.ps1"));
        var docs = File.ReadAllText(Path.Combine(root, "docs", "production-readiness-gate.md"));
        var roadmap = File.ReadAllText(Path.Combine(root, "docs", "PRODUCT_COMPLETION_ROADMAP.md"));

        foreach (var expected in new[]
                 {
                     "validate-production-evidence-handoff-package-archive-ci-summary.ps1",
                     "bad-main-flow-status",
                     "bad-release-summary",
                     "missing-artifact-path",
                     "bad-long-path-status",
                     "main flow status must be passed",
                     "markdown is missing",
                     "production evidence handoff package archive CI summary validator regression passed"
                 })
        {
            Assert.Contains(expected, script, StringComparison.OrdinalIgnoreCase);
        }

        Assert.Contains("test-production-evidence-handoff-package-archive-ci-summary-validator.ps1", wrapper, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("ciSummaryValidatorRegression", wrapper, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("test-production-evidence-handoff-package-archive-ci-summary-validator.ps1", docs, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("[x] `P11-ACC-038`", roadmap, StringComparison.Ordinal);
    }

    [Fact]
    public void Production_Evidence_Handoff_Package_Archive_Ci_Regression_Result_Should_Have_Standalone_Validator()
    {
        var root = FindRepositoryRoot();
        var validator = File.ReadAllText(Path.Combine(root, "scripts", "validate-production-evidence-handoff-package-archive-ci-regression-result.ps1"));
        var wrapper = File.ReadAllText(Path.Combine(root, "scripts", "test-production-evidence-handoff-package-archive-ci-regression.ps1"));
        var docs = File.ReadAllText(Path.Combine(root, "docs", "production-readiness-gate.md"));
        var roadmap = File.ReadAllText(Path.Combine(root, "docs", "PRODUCT_COMPLETION_ROADMAP.md"));

        foreach (var expected in new[]
                 {
                     "ResultJsonPath",
                     "ResultMarkdownPath",
                     "main flow status",
                     "result validator regression status",
                     "long path regression status",
                     "CI summary validator regression status",
                     "bad-main-flow-status",
                     "bad-release-summary",
                     "missing-artifact-path",
                     "bad-long-path-status",
                     "production evidence handoff package archive CI regression result valid"
                 })
        {
            Assert.Contains(expected, validator, StringComparison.OrdinalIgnoreCase);
        }

        Assert.Contains("validate-production-evidence-handoff-package-archive-ci-regression-result.ps1", wrapper, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("validate-production-evidence-handoff-package-archive-ci-regression-result.ps1", docs, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("[x] `P11-ACC-039`", roadmap, StringComparison.Ordinal);
    }

    [Fact]
    public void Production_Evidence_Handoff_Package_Archive_Ci_Regression_Result_Validator_Should_Have_Regression_Harness()
    {
        var root = FindRepositoryRoot();
        var script = File.ReadAllText(Path.Combine(root, "scripts", "test-production-evidence-handoff-package-archive-ci-regression-result-validator.ps1"));
        var wrapper = File.ReadAllText(Path.Combine(root, "scripts", "test-production-evidence-handoff-package-archive-ci-regression.ps1"));
        var docs = File.ReadAllText(Path.Combine(root, "docs", "production-readiness-gate.md"));
        var roadmap = File.ReadAllText(Path.Combine(root, "docs", "PRODUCT_COMPLETION_ROADMAP.md"));

        foreach (var expected in new[]
                 {
                     "validate-production-evidence-handoff-package-archive-ci-regression-result.ps1",
                     "bad-status",
                     "missing-release-id",
                     "missing-summary-validator-failure",
                     "bad-markdown",
                     "status must be passed",
                     "releaseId is required",
                     "missing summary validator failure",
                     "markdown is missing",
                     "production evidence handoff package archive CI regression result validator regression passed"
                 })
        {
            Assert.Contains(expected, script, StringComparison.OrdinalIgnoreCase);
        }

        Assert.Contains("test-production-evidence-handoff-package-archive-ci-regression-result-validator.ps1", wrapper, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("ciResultValidatorRegression", wrapper, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("test-production-evidence-handoff-package-archive-ci-regression-result-validator.ps1", docs, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("[x] `P11-ACC-040`", roadmap, StringComparison.Ordinal);
    }

    [Fact]
    public void Production_Evidence_Handoff_Checklist_Should_Reject_Stale_Release_In_Production_Ready_Mode()
    {
        var root = FindRepositoryRoot();
        var validator = File.ReadAllText(Path.Combine(root, "scripts", "validate-production-evidence-handoff-checklist.ps1"));
        var regression = File.ReadAllText(Path.Combine(root, "scripts", "test-production-evidence-handoff-checklist-latest-release-guard.ps1"));
        var docs = File.ReadAllText(Path.Combine(root, "docs", "production-readiness-gate.md"));
        var roadmap = File.ReadAllText(Path.Combine(root, "docs", "PRODUCT_COMPLETION_ROADMAP.md"));

        foreach (var expected in new[]
                 {
                     "Get-LatestActiveReleaseId",
                     "backend/src/VpnPlatform.Api/AppReleases/releases.json",
                     "must match latest active release",
                     "RequireProductionReady"
                 })
        {
            Assert.Contains(expected, validator, StringComparison.OrdinalIgnoreCase);
        }

        foreach (var expected in new[]
                 {
                     "stale-release-id",
                     "must match latest active release",
                     "production evidence handoff checklist latest release guard valid"
                 })
        {
            Assert.Contains(expected, regression, StringComparison.OrdinalIgnoreCase);
        }

        Assert.Contains("test-production-evidence-handoff-checklist-latest-release-guard.ps1", docs, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("[x] `P11-ACC-066`", roadmap, StringComparison.Ordinal);
    }

    [Fact]
    public void Production_Evidence_Handoff_Package_Should_Reject_Stale_Release_In_Production_Ready_Mode()
    {
        var root = FindRepositoryRoot();
        var validator = File.ReadAllText(Path.Combine(root, "scripts", "validate-production-evidence-handoff-package.ps1"));
        var regression = File.ReadAllText(Path.Combine(root, "scripts", "test-production-evidence-handoff-package-latest-release-guard.ps1"));
        var docs = File.ReadAllText(Path.Combine(root, "docs", "production-readiness-gate.md"));
        var roadmap = File.ReadAllText(Path.Combine(root, "docs", "PRODUCT_COMPLETION_ROADMAP.md"));

        foreach (var expected in new[]
                 {
                     "Get-LatestActiveReleaseId",
                     "backend/src/VpnPlatform.Api/AppReleases/releases.json",
                     "must match latest active release",
                     "RequireProductionReady"
                 })
        {
            Assert.Contains(expected, validator, StringComparison.OrdinalIgnoreCase);
        }

        foreach (var expected in new[]
                 {
                     "stale-release-id",
                     "must match latest active release",
                     "production evidence handoff package latest release guard valid"
                 })
        {
            Assert.Contains(expected, regression, StringComparison.OrdinalIgnoreCase);
        }

        Assert.Contains("test-production-evidence-handoff-package-latest-release-guard.ps1", docs, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("[x] `P11-ACC-067`", roadmap, StringComparison.Ordinal);
    }

    [Fact]
    public void Production_Evidence_Handoff_Package_Archive_Should_Reject_Stale_Release_In_Production_Ready_Mode()
    {
        var root = FindRepositoryRoot();
        var validator = File.ReadAllText(Path.Combine(root, "scripts", "validate-production-evidence-handoff-package-archive.ps1"));
        var regression = File.ReadAllText(Path.Combine(root, "scripts", "test-production-evidence-handoff-package-archive-latest-release-guard.ps1"));
        var docs = File.ReadAllText(Path.Combine(root, "docs", "production-readiness-gate.md"));
        var roadmap = File.ReadAllText(Path.Combine(root, "docs", "PRODUCT_COMPLETION_ROADMAP.md"));

        foreach (var expected in new[]
                 {
                     "Get-LatestActiveReleaseId",
                     "backend/src/VpnPlatform.Api/AppReleases/releases.json",
                     "must match latest active release",
                     "RequireProductionReady"
                 })
        {
            Assert.Contains(expected, validator, StringComparison.OrdinalIgnoreCase);
        }

        foreach (var expected in new[]
                 {
                     "stale-release-id",
                     "must match latest active release",
                     "production evidence handoff package archive latest release guard valid"
                 })
        {
            Assert.Contains(expected, regression, StringComparison.OrdinalIgnoreCase);
        }

        Assert.Contains("test-production-evidence-handoff-package-archive-latest-release-guard.ps1", docs, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("[x] `P11-ACC-074`", roadmap, StringComparison.Ordinal);
    }

    [Fact]
    public void Production_Evidence_Handoff_Package_Archive_Flow_Result_Should_Reject_Stale_Release_In_Production_Ready_Mode()
    {
        var root = FindRepositoryRoot();
        var validator = File.ReadAllText(Path.Combine(root, "scripts", "validate-production-evidence-handoff-package-archive-flow-result.ps1"));
        var regression = File.ReadAllText(Path.Combine(root, "scripts", "test-production-evidence-handoff-package-archive-flow-result-latest-release-guard.ps1"));
        var docs = File.ReadAllText(Path.Combine(root, "docs", "production-readiness-gate.md"));
        var roadmap = File.ReadAllText(Path.Combine(root, "docs", "PRODUCT_COMPLETION_ROADMAP.md"));

        foreach (var expected in new[]
                 {
                     "Get-LatestActiveReleaseId",
                     "backend/src/VpnPlatform.Api/AppReleases/releases.json",
                     "must match latest active release",
                     "RequireProductionReady"
                 })
        {
            Assert.Contains(expected, validator, StringComparison.OrdinalIgnoreCase);
        }

        foreach (var expected in new[]
                 {
                     "stale-release-id",
                     "must match latest active release",
                     "production evidence handoff package archive flow result latest release guard valid"
                 })
        {
            Assert.Contains(expected, regression, StringComparison.OrdinalIgnoreCase);
        }

        Assert.Contains("test-production-evidence-handoff-package-archive-flow-result-latest-release-guard.ps1", docs, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("[x] `P11-ACC-068`", roadmap, StringComparison.Ordinal);
    }

    [Fact]
    public void Production_Evidence_Handoff_Package_Archive_Ci_Regression_Result_Should_Reject_Stale_Release_In_Production_Ready_Mode()
    {
        var root = FindRepositoryRoot();
        var validator = File.ReadAllText(Path.Combine(root, "scripts", "validate-production-evidence-handoff-package-archive-ci-regression-result.ps1"));
        var regression = File.ReadAllText(Path.Combine(root, "scripts", "test-production-evidence-handoff-package-archive-ci-regression-result-latest-release-guard.ps1"));
        var docs = File.ReadAllText(Path.Combine(root, "docs", "production-readiness-gate.md"));
        var roadmap = File.ReadAllText(Path.Combine(root, "docs", "PRODUCT_COMPLETION_ROADMAP.md"));

        foreach (var expected in new[]
                 {
                     "Get-LatestActiveReleaseId",
                     "backend/src/VpnPlatform.Api/AppReleases/releases.json",
                     "must match latest active release",
                     "RequireProductionReady"
                 })
        {
            Assert.Contains(expected, validator, StringComparison.OrdinalIgnoreCase);
        }

        foreach (var expected in new[]
                 {
                     "stale-release-id",
                     "must match latest active release",
                     "production evidence handoff package archive CI regression result latest release guard valid"
                 })
        {
            Assert.Contains(expected, regression, StringComparison.OrdinalIgnoreCase);
        }

        Assert.Contains("test-production-evidence-handoff-package-archive-ci-regression-result-latest-release-guard.ps1", docs, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("[x] `P11-ACC-069`", roadmap, StringComparison.Ordinal);
    }

    [Fact]
    public void Production_Evidence_Handoff_Package_Archive_Ci_Summary_Should_Reject_Stale_Release_In_Production_Ready_Mode()
    {
        var root = FindRepositoryRoot();
        var validator = File.ReadAllText(Path.Combine(root, "scripts", "validate-production-evidence-handoff-package-archive-ci-summary.ps1"));
        var regression = File.ReadAllText(Path.Combine(root, "scripts", "test-production-evidence-handoff-package-archive-ci-summary-latest-release-guard.ps1"));
        var docs = File.ReadAllText(Path.Combine(root, "docs", "production-readiness-gate.md"));
        var roadmap = File.ReadAllText(Path.Combine(root, "docs", "PRODUCT_COMPLETION_ROADMAP.md"));

        foreach (var expected in new[]
                 {
                     "Get-LatestActiveReleaseId",
                     "backend/src/VpnPlatform.Api/AppReleases/releases.json",
                     "must match latest active release",
                     "RequireProductionReady"
                 })
        {
            Assert.Contains(expected, validator, StringComparison.OrdinalIgnoreCase);
        }

        foreach (var expected in new[]
                 {
                     "stale-release-id",
                     "must match latest active release",
                     "production evidence handoff package archive CI summary latest release guard valid"
                 })
        {
            Assert.Contains(expected, regression, StringComparison.OrdinalIgnoreCase);
        }

        Assert.Contains("test-production-evidence-handoff-package-archive-ci-summary-latest-release-guard.ps1", docs, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("[x] `P11-ACC-070`", roadmap, StringComparison.Ordinal);
    }

    [Fact]
    public void Production_Readiness_Summary_Should_Reject_Stale_Release_In_Production_Ready_Mode()
    {
        var root = FindRepositoryRoot();
        var validator = File.ReadAllText(Path.Combine(root, "scripts", "validate-production-readiness-summary.ps1"));
        var regression = File.ReadAllText(Path.Combine(root, "scripts", "test-production-readiness-summary-latest-release-guard.ps1"));
        var docs = File.ReadAllText(Path.Combine(root, "docs", "production-readiness-gate.md"));
        var roadmap = File.ReadAllText(Path.Combine(root, "docs", "PRODUCT_COMPLETION_ROADMAP.md"));

        foreach (var expected in new[]
                 {
                     "Get-LatestActiveReleaseId",
                     "backend/src/VpnPlatform.Api/AppReleases/releases.json",
                     "must match latest active release",
                     "RequireProductionReady"
                 })
        {
            Assert.Contains(expected, validator, StringComparison.OrdinalIgnoreCase);
        }

        foreach (var expected in new[]
                 {
                     "stale-release-id",
                     "must match latest active release",
                     "production readiness summary latest release guard valid"
                 })
        {
            Assert.Contains(expected, regression, StringComparison.OrdinalIgnoreCase);
        }

        Assert.Contains("test-production-readiness-summary-latest-release-guard.ps1", docs, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("[x] `P11-ACC-071`", roadmap, StringComparison.Ordinal);
    }

    [Fact]
    public void Production_Readiness_Assertion_Result_Should_Reject_Stale_Release_In_Production_Ready_Mode()
    {
        var root = FindRepositoryRoot();
        var validator = File.ReadAllText(Path.Combine(root, "scripts", "validate-production-readiness-assertion-result.ps1"));
        var regression = File.ReadAllText(Path.Combine(root, "scripts", "test-production-readiness-assertion-result-latest-release-guard.ps1"));
        var docs = File.ReadAllText(Path.Combine(root, "docs", "production-readiness-gate.md"));
        var roadmap = File.ReadAllText(Path.Combine(root, "docs", "PRODUCT_COMPLETION_ROADMAP.md"));

        foreach (var expected in new[]
                 {
                     "Get-LatestActiveReleaseId",
                     "backend/src/VpnPlatform.Api/AppReleases/releases.json",
                     "must match latest active release",
                     "RequireProductionReady"
                 })
        {
            Assert.Contains(expected, validator, StringComparison.OrdinalIgnoreCase);
        }

        foreach (var expected in new[]
                 {
                     "stale-release-id",
                     "must match latest active release",
                     "production readiness assertion result latest release guard valid"
                 })
        {
            Assert.Contains(expected, regression, StringComparison.OrdinalIgnoreCase);
        }

        Assert.Contains("test-production-readiness-assertion-result-latest-release-guard.ps1", docs, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("[x] `P11-ACC-072`", roadmap, StringComparison.Ordinal);
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
