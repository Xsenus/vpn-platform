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

$reportPath = Join-Path $tmpDirectory "payment-provider-smoke-stale-release-guard.json"

try {
    $report = Get-Content -LiteralPath $templatePath -Raw -Encoding UTF8 | ConvertFrom-Json
    $report.reportId = "payment-provider-smoke-stale-release-guard"
    $report.environmentName = "staging"
    $report.startedAt = "2026-07-01T11:00:00+07:00"
    $report.completedAt = "2026-07-01T11:30:00+07:00"
    $report.releaseId = "stale-release-id"
    $report.operator = "payment-provider-smoke-latest-release-guard"
    $report.notes = "sanitized regression report without secrets"

    foreach ($provider in @($report.providers)) {
        $provider.mode = "sandbox"
        $provider.status = "passed"
        $provider.accountConfigured = $true
        $provider.checkoutCreated = $true
        $provider.providerConfirmation = $true
        $provider.webhookProcessed = $true
        $provider.subscriptionActivated = $true
        $provider.refundChecked = $true
        $provider.evidence = "sanitized evidence for $($provider.provider)"
    }

    $report | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $reportPath -Encoding UTF8

    $previousErrorActionPreference = $ErrorActionPreference
    $ErrorActionPreference = "Continue"
    $output = & powershell -NoProfile -ExecutionPolicy Bypass -File $validatorPath -ReportPath $reportPath -RequireAllPassed 2>&1
    $validatorExitCode = $LASTEXITCODE
    $ErrorActionPreference = $previousErrorActionPreference

    if ($validatorExitCode -eq 0) {
        throw "Validator accepted stale releaseId in -RequireAllPassed mode."
    }

    $text = [string]::Join("`n", @($output | ForEach-Object { [string]$_ }))
    if ($text.IndexOf("must match latest active release", [System.StringComparison]::OrdinalIgnoreCase) -lt 0) {
        throw "Validator failed for an unexpected reason: $text"
    }

    Write-Output "payment provider smoke latest release guard valid"
}
finally {
    if (Test-Path -LiteralPath $reportPath) {
        Remove-Item -LiteralPath $reportPath -Force
    }
}
