param(
    [string]$ApiBaseUrl = $env:ADMIN_VPS_SMOKE_API_BASE_URL,
    [string]$AdminWebUrl = $env:ADMIN_VPS_SMOKE_ADMIN_WEB_URL,
    [string]$AdminEmail = $(if ($env:ADMIN_VPS_BOOTSTRAP_EMAIL) { $env:ADMIN_VPS_BOOTSTRAP_EMAIL } else { $env:ADMIN_VPS_SMOKE_ADMIN_EMAIL }),
    [string]$AdminPasswordEnvName = $(if ($env:ADMIN_VPS_BOOTSTRAP_SMOKE_PASSWORD_ENV) { $env:ADMIN_VPS_BOOTSTRAP_SMOKE_PASSWORD_ENV } else { "ADMIN_VPS_BOOTSTRAP_SMOKE_ADMIN_PASSWORD" }),
    [string]$DisplayName = $(if ($env:AdminBootstrap__DisplayName) { $env:AdminBootstrap__DisplayName } else { "Platform Admin" }),
    [string]$RolesCsv = $(if ($env:AdminBootstrap__RolesCsv) { $env:AdminBootstrap__RolesCsv } else { "SuperAdmin" }),
    [string]$Provider = $(if ($env:Database__Provider) { $env:Database__Provider } else { "Postgres" }),
    [string]$ConnectionString = $env:ConnectionStrings__DefaultConnection,
    [string]$ProjectPath = "backend/src/VpnPlatform.Api/VpnPlatform.Api.csproj",
    [string]$DataProtectionKeyPath = $env:DataProtection__KeyPath,
    [string]$SmokeReportPath = "tmp/admin-vps-smoke-report.json",
    [string]$PreflightReportPath = "tmp/admin-vps-smoke-preflight-report.json",
    [string]$EnvironmentName = $(if ($env:ADMIN_VPS_SMOKE_ENVIRONMENT) { $env:ADMIN_VPS_SMOKE_ENVIRONMENT } else { "Production" }),
    [string]$Operator = $env:ADMIN_VPS_SMOKE_OPERATOR,
    [string]$ReleaseId = $env:ADMIN_VPS_SMOKE_RELEASE_ID,
    [string]$FrontendPath = "frontend",
    [switch]$LocalSqlite,
    [switch]$ApplyMigrations,
    [switch]$ConfirmBootstrapReset,
    [switch]$DryRun
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$bootstrapScript = Join-Path $repoRoot "scripts/admin-bootstrap.ps1"
$smokeScript = Join-Path $repoRoot "scripts/admin-vps-smoke.ps1"

function Set-ProcessEnv {
    param(
        [Parameter(Mandatory = $true)][string]$Name,
        [AllowNull()][string]$Value
    )

    [Environment]::SetEnvironmentVariable($Name, $Value, "Process")
    if ($null -eq $Value) {
        Remove-Item -LiteralPath "Env:\$Name" -ErrorAction SilentlyContinue
    }
    else {
        Set-Item -LiteralPath "Env:\$Name" -Value $Value
    }
}

foreach ($requiredScript in @($bootstrapScript, $smokeScript)) {
    if (-not (Test-Path -LiteralPath $requiredScript -PathType Leaf)) {
        throw "Required admin VPS bootstrap smoke script was not found: $requiredScript"
    }
}

if ([string]::IsNullOrWhiteSpace($AdminEmail)) {
    throw "Admin email is required."
}

if ([string]::IsNullOrWhiteSpace($AdminPasswordEnvName)) {
    throw "Admin password env name is required."
}

$password = [Environment]::GetEnvironmentVariable($AdminPasswordEnvName, "Process")
if ([string]::IsNullOrWhiteSpace($password)) {
    throw "Admin password env '$AdminPasswordEnvName' is required."
}

if ($password.Length -lt 16) {
    throw "Admin password env '$AdminPasswordEnvName' must contain at least 16 characters."
}

if (-not $LocalSqlite -and -not $ConfirmBootstrapReset) {
    throw "Pass -ConfirmBootstrapReset to run admin bootstrap/reset against a non-local database."
}

if (-not $LocalSqlite -and [string]::IsNullOrWhiteSpace($ConnectionString)) {
    throw "Connection string is required for non-local admin bootstrap/reset."
}

$previousBootstrapPassword = [Environment]::GetEnvironmentVariable("AdminBootstrap__Password", "Process")
$previousSmokePassword = [Environment]::GetEnvironmentVariable("ADMIN_VPS_SMOKE_ADMIN_PASSWORD", "Process")

try {
    Write-Host "Admin VPS bootstrap+smoke flow is ready to run."
    Write-Host "Environment: $EnvironmentName"
    Write-Host "Provider: $Provider"
    Write-Host "API base URL: $ApiBaseUrl"
    Write-Host "Admin web URL: $AdminWebUrl"
    Write-Host "Admin email: $AdminEmail"
    Write-Host "Password: [hidden]"
    Write-Host "Smoke report path: $SmokeReportPath"
    Write-Host "Preflight report path: $PreflightReportPath"
    Write-Host "Bootstrap reset confirmed: $ConfirmBootstrapReset"

    $bootstrapArgs = @{
        EnvironmentName = $EnvironmentName
        Email = $AdminEmail
        DisplayName = $DisplayName
        RolesCsv = $RolesCsv
        Provider = $Provider
        ProjectPath = $ProjectPath
    }

    if (-not [string]::IsNullOrWhiteSpace($ConnectionString)) {
        $bootstrapArgs["ConnectionString"] = $ConnectionString
    }

    if (-not [string]::IsNullOrWhiteSpace($DataProtectionKeyPath)) {
        $bootstrapArgs["DataProtectionKeyPath"] = $DataProtectionKeyPath
    }

    if ($LocalSqlite) {
        $bootstrapArgs["LocalSqlite"] = $true
    }

    if ($ApplyMigrations) {
        $bootstrapArgs["ApplyMigrations"] = $true
    }

    if ($DryRun) {
        $bootstrapArgs["DryRun"] = $true
    }

    Set-ProcessEnv "AdminBootstrap__Password" $password
    & $bootstrapScript @bootstrapArgs

    if ($DryRun) {
        Write-Host "Dry-run mode: admin VPS smoke was not started."
        return
    }

    Set-ProcessEnv "ADMIN_VPS_SMOKE_ADMIN_PASSWORD" $password

    & $smokeScript `
        -ApiBaseUrl $ApiBaseUrl `
        -AdminWebUrl $AdminWebUrl `
        -AdminEmail $AdminEmail `
        -SmokeReportPath $SmokeReportPath `
        -PreflightReportPath $PreflightReportPath `
        -EnvironmentName $EnvironmentName `
        -Operator $Operator `
        -ReleaseId $ReleaseId `
        -FrontendPath $FrontendPath `
        -AccountBootstrapChecked

    Write-Host "Admin VPS bootstrap+smoke flow completed."
}
finally {
    Set-ProcessEnv "AdminBootstrap__Password" $previousBootstrapPassword
    Set-ProcessEnv "ADMIN_VPS_SMOKE_ADMIN_PASSWORD" $previousSmokePassword
}
