param(
    [string]$EnvironmentName = $(if ($env:ASPNETCORE_ENVIRONMENT) { $env:ASPNETCORE_ENVIRONMENT } else { "Production" }),
    [string]$Email = $env:AdminBootstrap__Email,
    [string]$Password = $env:AdminBootstrap__Password,
    [string]$DisplayName = $(if ($env:AdminBootstrap__DisplayName) { $env:AdminBootstrap__DisplayName } else { "Platform Admin" }),
    [string]$RolesCsv = $(if ($env:AdminBootstrap__RolesCsv) { $env:AdminBootstrap__RolesCsv } else { "SuperAdmin" }),
    [string]$Provider = $(if ($env:Database__Provider) { $env:Database__Provider } else { "Postgres" }),
    [string]$ConnectionString = $env:ConnectionStrings__DefaultConnection,
    [string]$ProjectPath = "backend/src/VpnPlatform.Api/VpnPlatform.Api.csproj",
    [string]$DataProtectionKeyPath = $env:DataProtection__KeyPath,
    [switch]$LocalSqlite,
    [switch]$ApplyMigrations,
    [switch]$DryRun
)

$ErrorActionPreference = "Stop"

function Set-ProcessEnv {
    param(
        [Parameter(Mandatory = $true)][string]$Name,
        [AllowEmptyString()][string]$Value
    )

    if (-not [string]::IsNullOrWhiteSpace($Value)) {
        [System.Environment]::SetEnvironmentVariable($Name, $Value, "Process")
    }
}

function Require-Value {
    param(
        [Parameter(Mandatory = $true)][string]$Name,
        [AllowEmptyString()][string]$Value
    )

    if ([string]::IsNullOrWhiteSpace($Value)) {
        throw "$Name is required."
    }
}

function Get-WorkspacePathValue {
    param([AllowEmptyString()][string]$Value)

    if ([string]::IsNullOrWhiteSpace($Value)) {
        return ""
    }

    return $Value.Trim()
}

function Get-AdminBootstrapTextValue {
    param(
        [AllowEmptyString()][string]$Value,
        [Parameter(Mandatory = $true)][string]$DefaultValue
    )

    if ([string]::IsNullOrWhiteSpace($Value)) {
        return $DefaultValue
    }

    return $Value.Trim()
}

$environmentNameValue = Get-AdminBootstrapTextValue -Value $EnvironmentName -DefaultValue "Production"
$emailValue = if ([string]::IsNullOrWhiteSpace($Email)) { "" } else { $Email.Trim() }
$displayNameValue = Get-AdminBootstrapTextValue -Value $DisplayName -DefaultValue "Platform Admin"
$rolesCsvValue = Get-AdminBootstrapTextValue -Value $RolesCsv -DefaultValue "SuperAdmin"
$providerValue = Get-AdminBootstrapTextValue -Value $Provider -DefaultValue "Postgres"

Require-Value "Admin email" $emailValue
Require-Value "Admin password" $Password

if ($Password.Length -lt 16) {
    throw "Admin password must contain at least 16 characters."
}

if ($LocalSqlite) {
    $providerValue = "Sqlite"
    if ([string]::IsNullOrWhiteSpace($ConnectionString)) {
        $ConnectionString = "Data Source=data/vpnplatform-local.db"
    }
}

$normalizedProjectPath = Get-WorkspacePathValue -Value $ProjectPath
if (-not [System.IO.Path]::IsPathRooted($normalizedProjectPath)) {
    $normalizedProjectPath = Join-Path (Get-Location) $normalizedProjectPath
}

if (-not (Test-Path -LiteralPath $normalizedProjectPath -PathType Leaf)) {
    throw "API project was not found: $normalizedProjectPath"
}

Set-ProcessEnv "ASPNETCORE_ENVIRONMENT" $environmentNameValue
Set-ProcessEnv "AdminBootstrap__Enabled" "true"
Set-ProcessEnv "AdminBootstrap__Email" $emailValue
Set-ProcessEnv "AdminBootstrap__Password" $Password
Set-ProcessEnv "AdminBootstrap__DisplayName" $displayNameValue
Set-ProcessEnv "AdminBootstrap__RolesCsv" $rolesCsvValue
Set-ProcessEnv "Database__Provider" $providerValue
Set-ProcessEnv "Database__ApplyMigrationsOnStartup" $(if ($ApplyMigrations -or $LocalSqlite) { "true" } else { "false" })
Set-ProcessEnv "Database__UseEnsureCreatedForLocalSqlite" $(if ($LocalSqlite) { "true" } else { "false" })
Set-ProcessEnv "ConnectionStrings__DefaultConnection" $ConnectionString
Set-ProcessEnv "DataProtection__KeyPath" (Get-WorkspacePathValue -Value $DataProtectionKeyPath)

Write-Host "Admin bootstrap/reset is ready to run."
Write-Host "Environment: $environmentNameValue"
Write-Host "Provider: $providerValue"
Write-Host "Email: $emailValue"
Write-Host "Roles: $rolesCsvValue"
Write-Host "Apply migrations: $($ApplyMigrations -or $LocalSqlite)"
Write-Host "Password: [hidden]"

if ($DryRun) {
    Write-Host "Dry-run mode: database was not changed."
    return
}

dotnet run --project $normalizedProjectPath -- admin-bootstrap
