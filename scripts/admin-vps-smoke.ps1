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
    [switch]$AccountBootstrapChecked
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$preflightScript = Join-Path $repoRoot "scripts/admin-vps-smoke-preflight.ps1"
$browserSmokeScript = Join-Path $repoRoot "scripts/admin-vps-browser-smoke.ps1"
$reportValidatorScript = Join-Path $repoRoot "scripts/validate-admin-vps-smoke-report.ps1"
$preflightValidatorScript = Join-Path $repoRoot "scripts/validate-admin-vps-smoke-preflight-report.ps1"
$evidenceValidatorScript = Join-Path $repoRoot "scripts/validate-admin-vps-smoke-evidence.ps1"

foreach ($requiredScript in @($preflightScript, $browserSmokeScript, $reportValidatorScript, $preflightValidatorScript, $evidenceValidatorScript)) {
    if (-not (Test-Path -LiteralPath $requiredScript -PathType Leaf)) {
        throw "Required admin VPS smoke script was not found: $requiredScript"
    }
}

Write-Host "Admin VPS smoke flow is ready to run."
Write-Host "API base URL: $ApiBaseUrl"
Write-Host "Admin web URL: $AdminWebUrl"
Write-Host "Admin email: $AdminEmail"
Write-Host "Password: [hidden]"
Write-Host "Smoke report path: $SmokeReportPath"
Write-Host "Preflight report path: $PreflightReportPath"
Write-Host "Account bootstrap checked: $AccountBootstrapChecked"

& $preflightScript `
    -ApiBaseUrl $ApiBaseUrl `
    -AdminWebUrl $AdminWebUrl `
    -AdminEmail $AdminEmail `
    -SmokeReportPath $SmokeReportPath `
    -PreflightReportPath $PreflightReportPath `
    -EnvironmentName $EnvironmentName `
    -Operator $Operator `
    -ReleaseId $ReleaseId `
    -FrontendPath $FrontendPath `
    -RequirePassword

& $browserSmokeScript `
    -ApiBaseUrl $ApiBaseUrl `
    -AdminWebUrl $AdminWebUrl `
    -AdminEmail $AdminEmail `
    -OutputPath $SmokeReportPath `
    -EnvironmentName $EnvironmentName `
    -Operator $Operator `
    -ReleaseId $ReleaseId `
    -FrontendPath $FrontendPath `
    -AccountBootstrapChecked:$AccountBootstrapChecked `
    -RequireAllPassed

& $evidenceValidatorScript `
    -PreflightReportPath $PreflightReportPath `
    -SmokeReportPath $SmokeReportPath

Write-Host "Admin VPS smoke flow completed."
Write-Host "Validated preflight report: $PreflightReportPath"
Write-Host "Validated smoke report: $SmokeReportPath"
