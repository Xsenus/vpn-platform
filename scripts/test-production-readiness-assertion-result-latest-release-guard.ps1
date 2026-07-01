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

$resultJsonPath = Join-Path $tmpDirectory "production-readiness-assertion-result-stale-release-guard.json"

try {
    $result = [ordered]@{
        status = "production-ready"
        releaseId = "stale-release-id"
        reportPath = "tmp/staging-smoke-report.json"
        paymentProviderReportPath = "tmp/payment-provider-smoke-report.json"
        adminVpsReportPath = "tmp/admin-vps-smoke-report.json"
        vpnLiveReportPath = "tmp/vpn-live-smoke-report.json"
        roadmapPath = "docs/PRODUCT_COMPLETION_ROADMAP.md"
        releaseDecisionPath = "docs/release-decision.md"
        resultMarkdownPath = "tmp/production-readiness-assertion.md"
        evidenceReports = @(
            [ordered]@{ name = "staging-vps"; status = "passed"; reportPath = "tmp/staging-smoke-report.json"; validatorPath = "scripts/validate-staging-smoke-report.ps1" },
            [ordered]@{ name = "payment-providers"; status = "passed"; reportPath = "tmp/payment-provider-smoke-report.json"; validatorPath = "scripts/validate-payment-provider-smoke-report.ps1" },
            [ordered]@{ name = "admin-vps"; status = "passed"; reportPath = "tmp/admin-vps-smoke-report.json"; validatorPath = "scripts/validate-admin-vps-smoke-report.ps1" },
            [ordered]@{ name = "vpn-live"; status = "passed"; reportPath = "tmp/vpn-live-smoke-report.json"; validatorPath = "scripts/validate-vpn-live-smoke-report.ps1" }
        )
        failedEvidenceReportsCount = 0
        blockers = @()
        blockersCount = 0
    }

    $result | ConvertTo-Json -Depth 10 | Set-Content -LiteralPath $resultJsonPath -Encoding UTF8

    $previousErrorActionPreference = $ErrorActionPreference
    $ErrorActionPreference = "Continue"
    $output = & powershell -NoProfile -ExecutionPolicy Bypass -File $validatorPath -ResultJsonPath $resultJsonPath -RequireProductionReady 2>&1
    $validatorExitCode = $LASTEXITCODE
    $ErrorActionPreference = $previousErrorActionPreference

    if ($validatorExitCode -eq 0) {
        throw "Validator accepted stale releaseId in -RequireProductionReady mode."
    }

    $text = [string]::Join("`n", @($output | ForEach-Object { [string]$_ }))
    if ($text.IndexOf("must match latest active release", [System.StringComparison]::OrdinalIgnoreCase) -lt 0) {
        throw "Validator failed for an unexpected reason: $text"
    }

    Write-Output "production readiness assertion result latest release guard valid"
}
finally {
    if (Test-Path -LiteralPath $resultJsonPath) {
        Remove-Item -LiteralPath $resultJsonPath -Force
    }
}
