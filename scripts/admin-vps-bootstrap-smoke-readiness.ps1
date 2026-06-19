param(
    [string]$ApiBaseUrl = $env:ADMIN_VPS_SMOKE_API_BASE_URL,
    [string]$AdminWebUrl = $env:ADMIN_VPS_SMOKE_ADMIN_WEB_URL,
    [string]$AdminEmail = $(if ($env:ADMIN_VPS_BOOTSTRAP_EMAIL) { $env:ADMIN_VPS_BOOTSTRAP_EMAIL } else { $env:ADMIN_VPS_SMOKE_ADMIN_EMAIL }),
    [string]$AdminPasswordEnvName = $(if ($env:ADMIN_VPS_BOOTSTRAP_SMOKE_PASSWORD_ENV) { $env:ADMIN_VPS_BOOTSTRAP_SMOKE_PASSWORD_ENV } else { "ADMIN_VPS_BOOTSTRAP_SMOKE_ADMIN_PASSWORD" }),
    [string]$Provider = $(if ($env:Database__Provider) { $env:Database__Provider } else { "Postgres" }),
    [string]$ConnectionString = $env:ConnectionStrings__DefaultConnection,
    [string]$ProjectPath = "backend/src/VpnPlatform.Api/VpnPlatform.Api.csproj",
    [string]$SmokeReportPath = "tmp/admin-vps-smoke-report.json",
    [string]$PreflightReportPath = "tmp/admin-vps-smoke-preflight-report.json",
    [string]$BootstrapSmokeReportPath = "tmp/admin-vps-bootstrap-smoke-report.json",
    [string]$ReadinessReportPath = "tmp/admin-vps-bootstrap-smoke-readiness-report.json",
    [string]$EnvironmentName = $(if ($env:ADMIN_VPS_SMOKE_ENVIRONMENT) { $env:ADMIN_VPS_SMOKE_ENVIRONMENT } else { "Production" }),
    [string]$Operator = $env:ADMIN_VPS_SMOKE_OPERATOR,
    [string]$ReleaseId = $env:ADMIN_VPS_SMOKE_RELEASE_ID,
    [string]$FrontendPath = "frontend",
    [switch]$LocalSqlite,
    [switch]$ApplyMigrations,
    [switch]$ConfirmBootstrapReset,
    [switch]$RequireReady
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$checks = [System.Collections.Generic.List[object]]::new()

function Add-Check {
    param(
        [Parameter(Mandatory = $true)][string]$Name,
        [Parameter(Mandatory = $true)][bool]$Passed,
        [Parameter(Mandatory = $true)][string]$Message
    )

    $script:checks.Add([ordered]@{
        name = $Name
        passed = $Passed
        message = $Message
    })
}

function Test-HttpUrl {
    param([AllowEmptyString()][string]$Value)

    $parsed = $null
    return -not [string]::IsNullOrWhiteSpace($Value) `
        -and [Uri]::TryCreate($Value, [UriKind]::Absolute, [ref]$parsed) `
        -and ($parsed.Scheme -eq "http" -or $parsed.Scheme -eq "https")
}

function Resolve-WorkspacePath {
    param([Parameter(Mandatory = $true)][string]$Path)

    if ([System.IO.Path]::IsPathRooted($Path)) {
        return [System.IO.Path]::GetFullPath($Path)
    }

    return [System.IO.Path]::GetFullPath((Join-Path $repoRoot $Path))
}

$password = [Environment]::GetEnvironmentVariable($AdminPasswordEnvName, "Process")
$passwordPresent = -not [string]::IsNullOrWhiteSpace($password)
$passwordLengthOk = $passwordPresent -and $password.Length -ge 16
$connectionStringPresent = -not [string]::IsNullOrWhiteSpace($ConnectionString)
$providerValue = if ($LocalSqlite) { "Sqlite" } else { $Provider }

$projectFullPath = Resolve-WorkspacePath $ProjectPath
$frontendFullPath = Resolve-WorkspacePath $FrontendPath
$readinessReportFullPath = Resolve-WorkspacePath $ReadinessReportPath
$bootstrapScript = Join-Path $repoRoot "scripts/admin-bootstrap.ps1"
$smokeScript = Join-Path $repoRoot "scripts/admin-vps-smoke.ps1"
$readinessValidatorScript = Join-Path $repoRoot "scripts/validate-admin-vps-bootstrap-smoke-readiness-report.ps1"
$bootstrapReportValidatorScript = Join-Path $repoRoot "scripts/validate-admin-vps-bootstrap-smoke-report.ps1"
$packageJsonPath = Join-Path $frontendFullPath "package.json"

Add-Check "api-base-url" (Test-HttpUrl $ApiBaseUrl) "ADMIN_VPS_SMOKE_API_BASE_URL must be an absolute http/https URL."
Add-Check "admin-web-url" (Test-HttpUrl $AdminWebUrl) "ADMIN_VPS_SMOKE_ADMIN_WEB_URL must be an absolute http/https URL."
Add-Check "admin-email" (-not [string]::IsNullOrWhiteSpace($AdminEmail) -and $AdminEmail.Contains("@")) "Admin email must be set and contain @."
Add-Check "password-env-name" (-not [string]::IsNullOrWhiteSpace($AdminPasswordEnvName)) "Admin password env name must be set."
Add-Check "password-env-present" $passwordPresent "Admin password env must be present in the process environment and is never printed."
Add-Check "password-length" $passwordLengthOk "Admin password env value must contain at least 16 characters."
Add-Check "provider-supported" (@("Postgres", "Sqlite") -contains $providerValue) "Provider must be Postgres or Sqlite."
Add-Check "local-or-confirm-reset" ([bool]$LocalSqlite -or [bool]$ConfirmBootstrapReset) "Non-local admin bootstrap/reset requires -ConfirmBootstrapReset."
Add-Check "connection-string" ([bool]$LocalSqlite -or $connectionStringPresent) "Connection string is required for non-local admin bootstrap/reset."
Add-Check "project-file" (Test-Path -LiteralPath $projectFullPath -PathType Leaf) "Backend API project file must exist."
Add-Check "frontend-directory" (Test-Path -LiteralPath $frontendFullPath -PathType Container) "Frontend directory must exist."
Add-Check "package-command" ((Test-Path -LiteralPath $packageJsonPath -PathType Leaf) -and ((Get-Content -LiteralPath $packageJsonPath -Raw -Encoding UTF8).Contains("e2e:admin-vps-smoke"))) "frontend/package.json must expose e2e:admin-vps-smoke."
Add-Check "bootstrap-script" (Test-Path -LiteralPath $bootstrapScript -PathType Leaf) "scripts/admin-bootstrap.ps1 must exist."
Add-Check "smoke-wrapper" (Test-Path -LiteralPath $smokeScript -PathType Leaf) "scripts/admin-vps-smoke.ps1 must exist."
Add-Check "readiness-validator" (Test-Path -LiteralPath $readinessValidatorScript -PathType Leaf) "scripts/validate-admin-vps-bootstrap-smoke-readiness-report.ps1 must exist."
Add-Check "bootstrap-report-validator" (Test-Path -LiteralPath $bootstrapReportValidatorScript -PathType Leaf) "scripts/validate-admin-vps-bootstrap-smoke-report.ps1 must exist."

$failedCheck = $checks | Where-Object { -not $_.passed } | Select-Object -First 1
$readyForBootstrapSmoke = $null -eq $failedCheck
$generatedAt = [DateTimeOffset]::UtcNow
$operatorValue = if ([string]::IsNullOrWhiteSpace($Operator)) { "manual-operator" } else { $Operator.Trim() }
$releaseValue = if ([string]::IsNullOrWhiteSpace($ReleaseId)) { "manual-admin-vps-bootstrap-smoke-readiness" } else { $ReleaseId.Trim() }

$report = [ordered]@{
    reportId = "admin-vps-bootstrap-smoke-readiness-" + $generatedAt.ToString("yyyyMMdd-HHmmss")
    generatedAt = $generatedAt.ToString("o")
    environmentName = $EnvironmentName
    operator = $operatorValue
    releaseId = $releaseValue
    apiBaseUrl = $ApiBaseUrl
    adminWebUrl = $AdminWebUrl
    adminEmail = $AdminEmail
    provider = $providerValue
    localSqlite = [bool]$LocalSqlite
    applyMigrations = [bool]$ApplyMigrations
    confirmBootstrapReset = [bool]$ConfirmBootstrapReset
    connectionStringPresent = $connectionStringPresent
    passwordEnvName = $AdminPasswordEnvName
    passwordEnvPresent = $passwordPresent
    passwordLengthOk = $passwordLengthOk
    smokeReportPath = $SmokeReportPath
    preflightReportPath = $PreflightReportPath
    bootstrapSmokeReportPath = $BootstrapSmokeReportPath
    readinessReportPath = $ReadinessReportPath
    readyForBootstrapSmoke = $readyForBootstrapSmoke
    checks = @($checks)
}

$readinessReportParent = Split-Path -Parent $readinessReportFullPath
if (-not [string]::IsNullOrWhiteSpace($readinessReportParent) -and -not (Test-Path -LiteralPath $readinessReportParent -PathType Container)) {
    New-Item -ItemType Directory -Path $readinessReportParent -Force | Out-Null
}

[System.IO.File]::WriteAllText(
    $readinessReportFullPath,
    ($report | ConvertTo-Json -Depth 8),
    [System.Text.UTF8Encoding]::new($false))

Write-Host "Admin VPS bootstrap smoke readiness completed."
Write-Host "Environment: $EnvironmentName"
Write-Host "Provider: $providerValue"
Write-Host "API base URL: $ApiBaseUrl"
Write-Host "Admin web URL: $AdminWebUrl"
Write-Host "Admin email: $AdminEmail"
Write-Host "Password env: $(if ($passwordPresent) { 'present [hidden]' } else { 'missing' })"
Write-Host "Connection string: $(if ($connectionStringPresent) { 'present [hidden]' } else { 'missing' })"
Write-Host "Confirm bootstrap reset: $ConfirmBootstrapReset"
Write-Host "Ready for bootstrap smoke: $readyForBootstrapSmoke"
Write-Host "Readiness report path: $readinessReportFullPath"

& $readinessValidatorScript -ReportPath $readinessReportFullPath -RequireReady:$RequireReady | Out-Host

if ($RequireReady -and -not $readyForBootstrapSmoke) {
    throw "Admin VPS bootstrap smoke readiness failed. Fix failed checks before running bootstrap+smoke."
}
