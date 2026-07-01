param()

$ErrorActionPreference = "Stop"

function Resolve-RepoPath {
    param([string]$RelativePath)

    $root = Split-Path -Parent $PSScriptRoot
    return Join-Path $root $RelativePath
}

$templatePath = Resolve-RepoPath "docs/admin-vps-smoke-report.template.json"
$validatorPath = Resolve-RepoPath "scripts/validate-admin-vps-smoke-report.ps1"
$tmpDirectory = Resolve-RepoPath "tmp"
New-Item -ItemType Directory -Force -Path $tmpDirectory | Out-Null

$reportPath = Join-Path $tmpDirectory "admin-vps-smoke-stale-release-guard.json"

try {
    $report = Get-Content -LiteralPath $templatePath -Raw -Encoding UTF8 | ConvertFrom-Json
    $report.reportId = "admin-vps-smoke-stale-release-guard"
    $report.environmentName = "staging"
    $report.apiBaseUrl = "https://api.example.test"
    $report.adminWebUrl = "https://admin.example.test"
    $report.adminEmail = "admin@example.test"
    $report.smokeReportPath = "tmp/admin-vps-smoke-stale-release-guard.json"
    $report.startedAt = "2026-07-01T14:00:00+07:00"
    $report.completedAt = "2026-07-01T14:30:00+07:00"
    $report.releaseId = "stale-release-id"
    $report.operator = "admin-vps-smoke-latest-release-guard"
    $report.notes = "sanitized regression report without secrets"
    $report.accountBootstrapChecked = $true
    $report.adminLoginPassed = $true
    $report.noJsErrors = $true
    $report.noUnauthorizedAfterLogin = $true

    foreach ($section in @($report.sections)) {
        $section.status = "passed"
        $section.httpStatus = 200
        $section.loaded = $true
        $section.evidence = "sanitized evidence for $($section.id)"
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

    Write-Output "admin vps smoke latest release guard valid"
}
finally {
    if (Test-Path -LiteralPath $reportPath) {
        Remove-Item -LiteralPath $reportPath -Force
    }

    if ((Test-Path -LiteralPath $tmpDirectory) -and -not (Get-ChildItem -LiteralPath $tmpDirectory -Force)) {
        Remove-Item -LiteralPath $tmpDirectory -Force
    }
}
