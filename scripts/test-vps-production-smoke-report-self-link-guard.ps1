param()

$ErrorActionPreference = "Stop"

function Resolve-RepoPath {
    param([string]$RelativePath)

    $root = Split-Path -Parent $PSScriptRoot
    return Join-Path $root $RelativePath
}

$templatePath = Resolve-RepoPath "docs/vps-production-smoke-report.template.json"
$validatorPath = Resolve-RepoPath "scripts/validate-vps-production-smoke-report.ps1"
$tmpDirectory = Resolve-RepoPath "tmp"
New-Item -ItemType Directory -Force -Path $tmpDirectory | Out-Null

$reportPath = Join-Path $tmpDirectory "vps-production-smoke-self-link-guard.json"

try {
    $report = Get-Content -LiteralPath $templatePath -Raw -Encoding UTF8 | ConvertFrom-Json
    $report.reportId = "vps-production-smoke-self-link-guard"
    $report.smokeReportPath = "tmp/other-vps-production-smoke-report.json"
    $report.environmentName = "staging"
    $report.apiBaseUrl = "https://api.example.test"
    $report.publicWebUrl = "https://example.test"
    $report.cabinetWebUrl = "https://example.test/cabinet"
    $report.adminWebUrl = "https://example.test/admin"
    $report.startedAt = "2026-07-02T10:00:00+07:00"
    $report.completedAt = "2026-07-02T10:05:00+07:00"
    $report.releaseId = "self-link-guard"
    $report.operator = "vps-production-smoke-self-link-guard"
    $report.notes = "sanitized regression report without secrets"

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

    Write-Output "vps production smoke report self-link guard valid"
}
finally {
    if (Test-Path -LiteralPath $reportPath) {
        Remove-Item -LiteralPath $reportPath -Force
    }

    if ((Test-Path -LiteralPath $tmpDirectory) -and -not (Get-ChildItem -LiteralPath $tmpDirectory -Force)) {
        Remove-Item -LiteralPath $tmpDirectory -Force
    }
}
