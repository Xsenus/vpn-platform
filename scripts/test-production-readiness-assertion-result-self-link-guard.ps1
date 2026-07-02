param()

$ErrorActionPreference = "Stop"

function Resolve-RepoPath {
    param([string]$RelativePath)

    $root = Split-Path -Parent $PSScriptRoot
    return Join-Path $root $RelativePath
}

$validatorPath = Resolve-RepoPath "scripts/validate-production-readiness-assertion-result.ps1"
$tmpDirectory = Resolve-RepoPath "tmp"
New-Item -ItemType Directory -Force -Path $tmpDirectory | Out-Null

$resultJsonPath = Join-Path $tmpDirectory "production-readiness-assertion-result-self-link-guard.json"

try {
    $result = [ordered]@{
        status = "blocked"
        releaseId = "2026-07-02-production-readiness-summary-self-link"
        reportPath = "docs/staging-smoke-report.template.json"
        paymentProviderReportPath = "docs/payment-provider-smoke-report.template.json"
        adminVpsReportPath = "docs/admin-vps-smoke-report.template.json"
        vpnLiveReportPath = "docs/vpn-live-smoke-report.template.json"
        roadmapPath = "docs/PRODUCT_COMPLETION_ROADMAP.md"
        releaseDecisionPath = "docs/release-decision.md"
        resultJsonPath = "tmp/other-production-readiness-assertion-result.json"
        evidenceReports = @(
            [ordered]@{ name = "staging-vps"; status = "failed"; reportPath = "docs/staging-smoke-report.template.json"; validatorPath = "scripts/validate-staging-smoke-report.ps1" },
            [ordered]@{ name = "payment-providers"; status = "failed"; reportPath = "docs/payment-provider-smoke-report.template.json"; validatorPath = "scripts/validate-payment-provider-smoke-report.ps1" },
            [ordered]@{ name = "admin-vps"; status = "failed"; reportPath = "docs/admin-vps-smoke-report.template.json"; validatorPath = "scripts/validate-admin-vps-smoke-report.ps1" },
            [ordered]@{ name = "vpn-live"; status = "failed"; reportPath = "docs/vpn-live-smoke-report.template.json"; validatorPath = "scripts/validate-vpn-live-smoke-report.ps1" }
        )
        failedEvidenceReportsCount = 4
        blockers = @("release-decision:not production-ready")
        blockersCount = 1
    }

    $result | ConvertTo-Json -Depth 10 | Set-Content -LiteralPath $resultJsonPath -Encoding UTF8

    $previousErrorActionPreference = $ErrorActionPreference
    $ErrorActionPreference = "Continue"
    $output = & powershell -NoProfile -ExecutionPolicy Bypass -File $validatorPath -ResultJsonPath $resultJsonPath 2>&1
    $validatorExitCode = $LASTEXITCODE
    $ErrorActionPreference = $previousErrorActionPreference

    if ($validatorExitCode -eq 0) {
        throw "Validator accepted mismatched resultJsonPath."
    }

    $text = [string]::Join("`n", @($output | ForEach-Object { [string]$_ }))
    if ($text.IndexOf("resultJsonPath must match validated result path", [System.StringComparison]::OrdinalIgnoreCase) -lt 0) {
        throw "Validator failed for an unexpected reason: $text"
    }

    Write-Output "production readiness assertion result self-link guard valid"
}
finally {
    if (Test-Path -LiteralPath $resultJsonPath) {
        Remove-Item -LiteralPath $resultJsonPath -Force
    }

    if ((Test-Path -LiteralPath $tmpDirectory) -and -not (Get-ChildItem -LiteralPath $tmpDirectory -Force)) {
        Remove-Item -LiteralPath $tmpDirectory -Force
    }
}
