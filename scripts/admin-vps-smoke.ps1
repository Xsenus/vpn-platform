param(
    [string]$ApiBaseUrl = $env:ADMIN_VPS_SMOKE_API_BASE_URL,
    [string]$AdminWebUrl = $env:ADMIN_VPS_SMOKE_ADMIN_WEB_URL,
    [string]$AdminEmail = $env:ADMIN_VPS_SMOKE_ADMIN_EMAIL,
    [string]$SmokeReportPath = "tmp/admin-vps-smoke-report.json",
    [string]$PreflightReportPath = "tmp/admin-vps-smoke-preflight-report.json",
    [string]$EnvironmentName = $(if ($env:ADMIN_VPS_SMOKE_ENVIRONMENT) { $env:ADMIN_VPS_SMOKE_ENVIRONMENT } else { "staging" }),
    [string]$Operator = $env:ADMIN_VPS_SMOKE_OPERATOR,
    [string]$ReleaseId = $env:ADMIN_VPS_SMOKE_RELEASE_ID,
    [string]$FrontendPath = "frontend",
    [ValidateRange(1, 1440)]
    [int]$MaxEvidenceChainMinutes = $(if ($env:ADMIN_VPS_SMOKE_MAX_EVIDENCE_CHAIN_MINUTES) { [int]$env:ADMIN_VPS_SMOKE_MAX_EVIDENCE_CHAIN_MINUTES } else { 120 }),
    [switch]$AccountBootstrapChecked
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$preflightScript = Join-Path $repoRoot "scripts/admin-vps-smoke-preflight.ps1"
$browserSmokeScript = Join-Path $repoRoot "scripts/admin-vps-browser-smoke.ps1"
$reportValidatorScript = Join-Path $repoRoot "scripts/validate-admin-vps-smoke-report.ps1"
$preflightValidatorScript = Join-Path $repoRoot "scripts/validate-admin-vps-smoke-preflight-report.ps1"
$evidenceValidatorScript = Join-Path $repoRoot "scripts/validate-admin-vps-smoke-evidence.ps1"

function Get-LatestReleaseId {
    $releasesPath = Join-Path $repoRoot "backend/src/VpnPlatform.Api/AppReleases/releases.json"
    if (-not (Test-Path -LiteralPath $releasesPath -PathType Leaf)) {
        return "manual-admin-vps-smoke-flow"
    }

    $releases = Get-Content -LiteralPath $releasesPath -Raw -Encoding UTF8 | ConvertFrom-Json
    $latest = @($releases | Where-Object { $_.isActive } | Sort-Object -Property { [System.DateTimeOffset]::Parse([string]$_.releasedAt, [System.Globalization.CultureInfo]::InvariantCulture) } -Descending | Select-Object -First 1)
    if ($latest.Count -eq 0 -or [string]::IsNullOrWhiteSpace([string]$latest[0].releaseId)) {
        return "manual-admin-vps-smoke-flow"
    }

    return [string]$latest[0].releaseId
}

foreach ($requiredScript in @($preflightScript, $browserSmokeScript, $reportValidatorScript, $preflightValidatorScript, $evidenceValidatorScript)) {
    if (-not (Test-Path -LiteralPath $requiredScript -PathType Leaf)) {
        throw "Required admin VPS smoke script was not found: $requiredScript"
    }
}

if ($MaxEvidenceChainMinutes -le 0) {
    throw "MaxEvidenceChainMinutes must be greater than 0."
}

$releaseValue = if ([string]::IsNullOrWhiteSpace($ReleaseId)) { Get-LatestReleaseId } else { $ReleaseId.Trim() }

Write-Host "Admin VPS smoke flow is ready to run."
Write-Host "API base URL: $ApiBaseUrl"
Write-Host "Admin web URL: $AdminWebUrl"
Write-Host "Admin email: $AdminEmail"
Write-Host "Password: [hidden]"
Write-Host "Smoke report path: $SmokeReportPath"
Write-Host "Preflight report path: $PreflightReportPath"
Write-Host "Release id: $releaseValue"
Write-Host "Max evidence chain minutes: $MaxEvidenceChainMinutes"
Write-Host "Account bootstrap checked: $AccountBootstrapChecked"

& $preflightScript `
    -ApiBaseUrl $ApiBaseUrl `
    -AdminWebUrl $AdminWebUrl `
    -AdminEmail $AdminEmail `
    -SmokeReportPath $SmokeReportPath `
    -PreflightReportPath $PreflightReportPath `
    -EnvironmentName $EnvironmentName `
    -Operator $Operator `
    -ReleaseId $releaseValue `
    -FrontendPath $FrontendPath `
    -RequirePassword

& $browserSmokeScript `
    -ApiBaseUrl $ApiBaseUrl `
    -AdminWebUrl $AdminWebUrl `
    -AdminEmail $AdminEmail `
    -OutputPath $SmokeReportPath `
    -EnvironmentName $EnvironmentName `
    -Operator $Operator `
    -ReleaseId $releaseValue `
    -FrontendPath $FrontendPath `
    -AccountBootstrapChecked:$AccountBootstrapChecked `
    -RequireAllPassed

& $evidenceValidatorScript `
    -PreflightReportPath $PreflightReportPath `
    -SmokeReportPath $SmokeReportPath `
    -MaxEvidenceChainMinutes $MaxEvidenceChainMinutes

Write-Host "Admin VPS smoke flow completed."
Write-Host "Validated preflight report: $PreflightReportPath"
Write-Host "Validated smoke report: $SmokeReportPath"
