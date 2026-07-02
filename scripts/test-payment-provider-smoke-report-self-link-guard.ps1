param()

$ErrorActionPreference = "Stop"

function Resolve-RepoPath {
    param([string]$RelativePath)

    $root = Split-Path -Parent $PSScriptRoot
    return Join-Path $root $RelativePath
}

$templatePath = Resolve-RepoPath "docs/payment-provider-smoke-report.template.json"
$validatorPath = Resolve-RepoPath "scripts/validate-payment-provider-smoke-report.ps1"
$tmpDirectory = Resolve-RepoPath "tmp"
New-Item -ItemType Directory -Force -Path $tmpDirectory | Out-Null

$reportPath = Join-Path $tmpDirectory "payment-provider-smoke-self-link-guard.json"

try {
    $report = Get-Content -LiteralPath $templatePath -Raw -Encoding UTF8 | ConvertFrom-Json
    $report.reportId = "payment-provider-smoke-self-link-guard"
    $report.environmentName = "staging"
    $report.startedAt = "2026-07-02T10:20:00+07:00"
    $report.completedAt = "2026-07-02T10:25:00+07:00"
    $report.smokeReportPath = "tmp/other-payment-provider-smoke-report.json"
    $report.releaseId = "payment-provider-smoke-self-link-guard"
    $report.operator = "payment-provider-smoke-self-link-guard"
    $report.notes = "sanitized regression report without secrets"

    foreach ($provider in @($report.providers)) {
        $provider.mode = "sandbox"
        $provider.status = "blocked"
        $provider.accountConfigured = $false
        $provider.checkoutCreated = $false
        $provider.providerConfirmation = $false
        $provider.webhookProcessed = $false
        $provider.subscriptionActivated = $false
        $provider.refundChecked = $false
        $provider.evidence = "sanitized draft evidence for $($provider.provider)"
    }

    $report | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $reportPath -Encoding UTF8

    $previousErrorActionPreference = $ErrorActionPreference
    $ErrorActionPreference = "Continue"
    $output = & powershell -NoProfile -ExecutionPolicy Bypass -File $validatorPath -ReportPath $reportPath 2>&1
    $validatorExitCode = $LASTEXITCODE
    $ErrorActionPreference = $previousErrorActionPreference

    if ($validatorExitCode -eq 0) {
        throw "Validator accepted mismatched smokeReportPath."
    }

    $text = [string]::Join("`n", @($output | ForEach-Object { [string]$_ }))
    if ($text.IndexOf("smokeReportPath must match ReportPath", [System.StringComparison]::OrdinalIgnoreCase) -lt 0) {
        throw "Validator failed for an unexpected reason: $text"
    }

    Write-Output "payment provider smoke report self-link guard valid"
}
finally {
    if (Test-Path -LiteralPath $reportPath) {
        Remove-Item -LiteralPath $reportPath -Force
    }

    if ((Test-Path -LiteralPath $tmpDirectory) -and -not (Get-ChildItem -LiteralPath $tmpDirectory -Force)) {
        Remove-Item -LiteralPath $tmpDirectory -Force
    }
}
