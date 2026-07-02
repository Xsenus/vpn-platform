param()

$ErrorActionPreference = "Stop"

function Resolve-RepoPath {
    param([string]$RelativePath)

    $root = Split-Path -Parent $PSScriptRoot
    return Join-Path $root $RelativePath
}

$validatorPath = Resolve-RepoPath "scripts/validate-production-readiness-summary.ps1"
$tmpDirectory = Resolve-RepoPath "tmp"
New-Item -ItemType Directory -Force -Path $tmpDirectory | Out-Null

$summaryPath = Join-Path $tmpDirectory "production-readiness-summary-self-link-guard.md"
$summaryJsonPath = [System.IO.Path]::ChangeExtension($summaryPath, ".json")

try {
    $reportPaths = [ordered]@{
        staging = "tmp/staging-smoke-report.json"
        paymentProviders = "tmp/payment-provider-smoke-report.json"
        adminVps = "tmp/admin-vps-smoke-report.json"
        vpnLive = "tmp/vpn-live-smoke-report.json"
    }

    $reports = @(
        [ordered]@{ name = "staging-vps"; status = "passed"; path = $reportPaths.staging; counts = [ordered]@{ total = 1; passed = 1; failed = 0; blocked = 0; skipped = 0; other = 0 }; flagCounts = [ordered]@{ total = 1; passed = 1; blocked = 0 } },
        [ordered]@{ name = "payment-providers"; status = "passed"; path = $reportPaths.paymentProviders; counts = [ordered]@{ total = 1; passed = 1; failed = 0; blocked = 0; skipped = 0; other = 0 }; flagCounts = [ordered]@{ total = 1; passed = 1; blocked = 0 } },
        [ordered]@{ name = "admin-vps"; status = "passed"; path = $reportPaths.adminVps; counts = [ordered]@{ total = 1; passed = 1; failed = 0; blocked = 0; skipped = 0; other = 0 }; flagCounts = [ordered]@{ total = 1; passed = 1; blocked = 0 } },
        [ordered]@{ name = "vpn-live"; status = "passed"; path = $reportPaths.vpnLive; counts = [ordered]@{ total = 1; passed = 1; failed = 0; blocked = 0; skipped = 0; other = 0 }; flagCounts = [ordered]@{ total = 1; passed = 1; blocked = 0 } }
    )

    $summary = [ordered]@{
        status = "production-ready"
        releaseId = "2026-07-02-vpn-live-smoke-report-self-link"
        generatedAt = "2026-07-02T10:40:00+07:00"
        summaryPath = "tmp/other-production-readiness-summary.md"
        jsonSummaryPath = $summaryJsonPath
        roadmapPath = "docs/PRODUCT_COMPLETION_ROADMAP.md"
        releaseDecisionPath = "docs/release-decision.md"
        reportPaths = $reportPaths
        reports = $reports
        roadmapBlockers = @()
    }

    $summary | ConvertTo-Json -Depth 10 | Set-Content -LiteralPath $summaryJsonPath -Encoding UTF8
    @"
# Production readiness summary

## Evidence reports

- staging-vps
- payment-providers
- admin-vps
- vpn-live

## Payment providers

All required providers passed.

## Roadmap blockers

None.

## Safety

Sanitized summary only.
"@ | Set-Content -LiteralPath $summaryPath -Encoding UTF8

    $previousErrorActionPreference = $ErrorActionPreference
    $ErrorActionPreference = "Continue"
    $output = & powershell -NoProfile -ExecutionPolicy Bypass -File $validatorPath -SummaryPath $summaryPath -JsonSummaryPath $summaryJsonPath 2>&1
    $validatorExitCode = $LASTEXITCODE
    $ErrorActionPreference = $previousErrorActionPreference

    if ($validatorExitCode -eq 0) {
        throw "Validator accepted mismatched summaryPath."
    }

    $text = [string]::Join("`n", @($output | ForEach-Object { [string]$_ }))
    if ($text.IndexOf("summaryPath must match validated summary path", [System.StringComparison]::OrdinalIgnoreCase) -lt 0) {
        throw "Validator failed for an unexpected reason: $text"
    }

    Write-Output "production readiness summary self-link guard valid"
}
finally {
    foreach ($path in @($summaryPath, $summaryJsonPath)) {
        if (Test-Path -LiteralPath $path) {
            Remove-Item -LiteralPath $path -Force
        }
    }

    if ((Test-Path -LiteralPath $tmpDirectory) -and -not (Get-ChildItem -LiteralPath $tmpDirectory -Force)) {
        Remove-Item -LiteralPath $tmpDirectory -Force
    }
}
