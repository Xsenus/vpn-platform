param(
    [Parameter(Mandatory = $true)]
    [string]$ReadinessReportPath,

    [Parameter(Mandatory = $true)]
    [string]$BootstrapSmokeReportPath
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$readinessValidatorScript = Join-Path $repoRoot "scripts/validate-admin-vps-bootstrap-smoke-readiness-report.ps1"
$bootstrapValidatorScript = Join-Path $repoRoot "scripts/validate-admin-vps-bootstrap-smoke-report.ps1"

function Resolve-WorkspacePath {
    param([Parameter(Mandatory = $true)][string]$Path)

    if ([System.IO.Path]::IsPathRooted($Path)) {
        return [System.IO.Path]::GetFullPath($Path)
    }

    return [System.IO.Path]::GetFullPath((Join-Path $repoRoot $Path))
}

function Normalize-Url {
    param([AllowEmptyString()][string]$Value)

    if ([string]::IsNullOrWhiteSpace($Value)) {
        return ""
    }

    return $Value.Trim().TrimEnd("/")
}

function Assert-Same {
    param(
        [AllowEmptyString()][string]$Left,
        [AllowEmptyString()][string]$Right,
        [Parameter(Mandatory = $true)][string]$Name
    )

    if (-not [string]::Equals($Left, $Right, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "Admin VPS bootstrap smoke evidence mismatch for $Name."
    }
}

$readinessFullPath = Resolve-WorkspacePath $ReadinessReportPath
$bootstrapFullPath = Resolve-WorkspacePath $BootstrapSmokeReportPath

& $readinessValidatorScript -ReportPath $readinessFullPath -RequireReady | Out-Host
& $bootstrapValidatorScript -ReportPath $bootstrapFullPath -RequirePassed | Out-Host

$readiness = Get-Content -LiteralPath $readinessFullPath -Raw -Encoding UTF8 | ConvertFrom-Json
$bootstrap = Get-Content -LiteralPath $bootstrapFullPath -Raw -Encoding UTF8 | ConvertFrom-Json

Assert-Same (Normalize-Url ([string]$readiness.apiBaseUrl)) (Normalize-Url ([string]$bootstrap.apiBaseUrl)) "apiBaseUrl"
Assert-Same (Normalize-Url ([string]$readiness.adminWebUrl)) (Normalize-Url ([string]$bootstrap.adminWebUrl)) "adminWebUrl"
Assert-Same ([string]$readiness.adminEmail) ([string]$bootstrap.adminEmail) "adminEmail"
Assert-Same ([string]$readiness.environmentName) ([string]$bootstrap.environmentName) "environmentName"
Assert-Same ([string]$readiness.operator) ([string]$bootstrap.operator) "operator"
Assert-Same ([string]$readiness.releaseId) ([string]$bootstrap.releaseId) "releaseId"
Assert-Same ([string]$readiness.provider) ([string]$bootstrap.provider) "provider"
Assert-Same (Resolve-WorkspacePath ([string]$readiness.readinessReportPath)) $readinessFullPath "readinessReportPath"
Assert-Same (Resolve-WorkspacePath ([string]$bootstrap.readinessReportPath)) $readinessFullPath "bootstrap readinessReportPath"
Assert-Same (Resolve-WorkspacePath ([string]$bootstrap.bootstrapSmokeReportPath)) $bootstrapFullPath "bootstrap bootstrapSmokeReportPath"
Assert-Same (Resolve-WorkspacePath ([string]$readiness.smokeReportPath)) (Resolve-WorkspacePath ([string]$bootstrap.smokeReportPath)) "smokeReportPath"
Assert-Same (Resolve-WorkspacePath ([string]$readiness.preflightReportPath)) (Resolve-WorkspacePath ([string]$bootstrap.preflightReportPath)) "preflightReportPath"
Assert-Same (Resolve-WorkspacePath ([string]$readiness.bootstrapSmokeReportPath)) $bootstrapFullPath "bootstrapSmokeReportPath"

if ($readiness.localSqlite -isnot [bool] -or $bootstrap.localSqlite -isnot [bool] -or $readiness.localSqlite -ne $bootstrap.localSqlite) {
    throw "Admin VPS bootstrap smoke evidence mismatch for localSqlite."
}

if (-not $readiness.localSqlite -and -not $readiness.confirmBootstrapReset) {
    throw "Admin VPS bootstrap smoke evidence requires confirmBootstrapReset for non-local database."
}

if (-not $readiness.localSqlite -and -not $bootstrap.bootstrapResetConfirmed) {
    throw "Admin VPS bootstrap smoke evidence requires bootstrapResetConfirmed for non-local database."
}

if (-not $readiness.passwordEnvPresent -or -not $bootstrap.passwordEnvPresent) {
    throw "Admin VPS bootstrap smoke evidence requires password env confirmation in both reports."
}

if (-not $readiness.readyForBootstrapSmoke) {
    throw "Admin VPS bootstrap smoke evidence requires readyForBootstrapSmoke."
}

if ([string]$bootstrap.status -ne "passed") {
    throw "Admin VPS bootstrap smoke evidence requires passed bootstrap smoke status."
}

$readinessGeneratedAt = [DateTimeOffset]::Parse([string]$readiness.generatedAt)
$bootstrapGeneratedAt = [DateTimeOffset]::Parse([string]$bootstrap.generatedAt)
$bootstrapCompletedAt = [DateTimeOffset]::Parse([string]$bootstrap.completedAt)

if ($bootstrapGeneratedAt -lt $readinessGeneratedAt) {
    throw "Admin VPS bootstrap smoke evidence bootstrap report must be generated after readiness report."
}

if ($bootstrapCompletedAt -lt $bootstrapGeneratedAt) {
    throw "Admin VPS bootstrap smoke evidence bootstrap completedAt must not be earlier than generatedAt."
}

$sectionsContractPath = Resolve-WorkspacePath "docs/admin-vps-smoke-sections.json"

$summary = [ordered]@{
    environmentName = $bootstrap.environmentName
    releaseId = $bootstrap.releaseId
    apiBaseUrl = $bootstrap.apiBaseUrl
    adminWebUrl = $bootstrap.adminWebUrl
    adminEmail = $bootstrap.adminEmail
    operator = $bootstrap.operator
    provider = $bootstrap.provider
    localSqlite = $bootstrap.localSqlite
    passwordEnvName = $bootstrap.passwordEnvName
    passwordEnvPresent = $bootstrap.passwordEnvPresent
    passwordLengthOk = $readiness.passwordLengthOk
    connectionStringPresent = $readiness.connectionStringPresent
    applyMigrations = $readiness.applyMigrations
    confirmBootstrapReset = $readiness.confirmBootstrapReset
    bootstrapResetConfirmed = $bootstrap.bootstrapResetConfirmed
    readyForBootstrapSmoke = $readiness.readyForBootstrapSmoke
    bootstrapStatus = $bootstrap.status
    sectionsContractPath = $sectionsContractPath
    readinessReportPath = $ReadinessReportPath
    bootstrapSmokeReportPath = $BootstrapSmokeReportPath
    preflightReportPath = $bootstrap.preflightReportPath
    smokeReportPath = $bootstrap.smokeReportPath
}

Write-Host "admin vps bootstrap smoke evidence valid $($summary | ConvertTo-Json -Compress)"
