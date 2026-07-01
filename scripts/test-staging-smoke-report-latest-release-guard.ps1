param()

$ErrorActionPreference = "Stop"

function Resolve-RepoPath {
    param([string]$RelativePath)

    $root = Split-Path -Parent $PSScriptRoot
    return Join-Path $root $RelativePath
}

$templatePath = Resolve-RepoPath "docs/staging-smoke-report.template.json"
$validatorPath = Resolve-RepoPath "scripts/validate-staging-smoke-report.ps1"
$tmpDirectory = Resolve-RepoPath "tmp"
New-Item -ItemType Directory -Force -Path $tmpDirectory | Out-Null

$reportPath = Join-Path $tmpDirectory "staging-smoke-stale-release-guard.json"

try {
    $report = Get-Content -LiteralPath $templatePath -Raw -Encoding UTF8 | ConvertFrom-Json
    $report.reportId = "staging-smoke-stale-release-guard"
    $report.environmentName = "staging"
    $report.apiBaseUrl = "https://api.example.test"
    $report.publicWebUrl = "https://example.test/"
    $report.cabinetWebUrl = "https://example.test/cabinet/"
    $report.adminWebUrl = "https://example.test/admin/"
    $report.startedAt = "2026-07-01T10:00:00+07:00"
    $report.completedAt = "2026-07-01T10:30:00+07:00"
    $report.releaseId = "stale-release-id"
    $report.operator = "staging-smoke-latest-release-guard"

    foreach ($check in @($report.checks)) {
        $check.status = "passed"
        $check.evidence = "sanitized evidence for $($check.id)"
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

    Write-Output "staging smoke latest release guard valid"
}
finally {
    if (Test-Path -LiteralPath $reportPath) {
        Remove-Item -LiteralPath $reportPath -Force
    }
}
