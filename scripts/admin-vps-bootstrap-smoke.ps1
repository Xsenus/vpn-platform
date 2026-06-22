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
    [string]$BootstrapSmokeReportPath = "tmp/admin-vps-bootstrap-smoke-report.json",
    [string]$ReadinessReportPath = "tmp/admin-vps-bootstrap-smoke-readiness-report.json",
    [string]$EnvironmentName = $(if ($env:ADMIN_VPS_SMOKE_ENVIRONMENT) { $env:ADMIN_VPS_SMOKE_ENVIRONMENT } else { "Production" }),
    [string]$Operator = $env:ADMIN_VPS_SMOKE_OPERATOR,
    [string]$ReleaseId = $env:ADMIN_VPS_SMOKE_RELEASE_ID,
    [string]$FrontendPath = "frontend",
    [string]$MaxEvidenceChainMinutes = $(if ($env:ADMIN_VPS_SMOKE_MAX_EVIDENCE_CHAIN_MINUTES) { $env:ADMIN_VPS_SMOKE_MAX_EVIDENCE_CHAIN_MINUTES } else { "120" }),
    [switch]$LocalSqlite,
    [switch]$ApplyMigrations,
    [switch]$ConfirmBootstrapReset,
    [switch]$DryRun
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$bootstrapScript = Join-Path $repoRoot "scripts/admin-bootstrap.ps1"
$smokeScript = Join-Path $repoRoot "scripts/admin-vps-smoke.ps1"
$readinessScript = Join-Path $repoRoot "scripts/admin-vps-bootstrap-smoke-readiness.ps1"
$bootstrapSmokeReportValidatorScript = Join-Path $repoRoot "scripts/validate-admin-vps-bootstrap-smoke-report.ps1"
$bootstrapSmokeEvidenceValidatorScript = Join-Path $repoRoot "scripts/validate-admin-vps-bootstrap-smoke-evidence.ps1"

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

function Get-LatestReleaseId {
    $releasesPath = Join-Path $repoRoot "backend/src/VpnPlatform.Api/AppReleases/releases.json"
    if (-not (Test-Path -LiteralPath $releasesPath -PathType Leaf)) {
        return "manual-admin-vps-bootstrap-smoke"
    }

    $releases = Get-Content -LiteralPath $releasesPath -Raw -Encoding UTF8 | ConvertFrom-Json
    $latest = @($releases | Where-Object { $_.isActive } | Sort-Object -Property { [System.DateTimeOffset]::Parse([string]$_.releasedAt, [System.Globalization.CultureInfo]::InvariantCulture) } -Descending | Select-Object -First 1)
    if ($latest.Count -eq 0 -or [string]::IsNullOrWhiteSpace([string]$latest[0].releaseId)) {
        return "manual-admin-vps-bootstrap-smoke"
    }

    return [string]$latest[0].releaseId
}

function Convert-MaxEvidenceChainMinutes {
    param([AllowEmptyString()][string]$Value)

    if ([string]::IsNullOrWhiteSpace($Value)) {
        throw "MaxEvidenceChainMinutes must be an integer."
    }

    $parsed = 0
    if (-not [int]::TryParse($Value.Trim(), [System.Globalization.NumberStyles]::Integer, [System.Globalization.CultureInfo]::InvariantCulture, [ref]$parsed)) {
        throw "MaxEvidenceChainMinutes must be an integer."
    }

    if ($parsed -le 0) {
        throw "MaxEvidenceChainMinutes must be greater than 0."
    }

    if ($parsed -gt 1440) {
        throw "MaxEvidenceChainMinutes must be less than or equal to 1440."
    }

    return $parsed
}

function Assert-HttpUrl {
    param(
        [AllowEmptyString()][string]$Value,
        [Parameter(Mandatory = $true)][string]$Name
    )

    $parsed = $null
    $isInvalid = [string]::IsNullOrWhiteSpace($Value) -or -not [Uri]::TryCreate($Value.Trim(), [UriKind]::Absolute, [ref]$parsed) -or ($parsed.Scheme -ne "http" -and $parsed.Scheme -ne "https")
    if ($isInvalid) {
        throw "$Name must be an absolute http or https URL."
    }
}

function Assert-AdminEmail {
    param([AllowEmptyString()][string]$Value)

    if ([string]::IsNullOrWhiteSpace($Value) -or -not $Value.Trim().Contains("@")) {
        throw "AdminEmail must contain an email address."
    }
}

function Get-ReportPathFullName {
    param(
        [AllowEmptyString()][string]$Path,
        [Parameter(Mandatory = $true)][string]$Name
    )

    if ([string]::IsNullOrWhiteSpace($Path)) {
        throw "$Name must not be empty."
    }

    $candidate = if ([System.IO.Path]::IsPathRooted($Path)) {
        $Path
    }
    else {
        Join-Path $repoRoot $Path
    }

    return [System.IO.Path]::GetFullPath($candidate)
}

function Assert-DistinctReportPaths {
    param([Parameter(Mandatory = $true)][object[]]$Reports)

    $seen = [System.Collections.Generic.Dictionary[string, string]]::new([System.StringComparer]::OrdinalIgnoreCase)
    foreach ($report in $Reports) {
        $name = [string]$report.Name
        $fullPath = Get-ReportPathFullName -Path ([string]$report.Path) -Name $name
        if ($seen.ContainsKey($fullPath)) {
            throw "$name must be different from $($seen[$fullPath])."
        }

        $seen.Add($fullPath, $name)
    }
}

function Get-OperatorValue {
    param([AllowEmptyString()][string]$Value)

    if ([string]::IsNullOrWhiteSpace($Value)) {
        return "manual-operator"
    }

    return $Value.Trim()
}

foreach ($requiredScript in @($bootstrapScript, $smokeScript, $readinessScript, $bootstrapSmokeReportValidatorScript, $bootstrapSmokeEvidenceValidatorScript)) {
    if (-not (Test-Path -LiteralPath $requiredScript -PathType Leaf)) {
        throw "Required admin VPS bootstrap smoke script was not found: $requiredScript"
    }
}

$maxEvidenceChainMinutesValue = Convert-MaxEvidenceChainMinutes -Value $MaxEvidenceChainMinutes
Assert-HttpUrl -Value $ApiBaseUrl -Name "ApiBaseUrl"
Assert-HttpUrl -Value $AdminWebUrl -Name "AdminWebUrl"
Assert-AdminEmail -Value $AdminEmail
Assert-DistinctReportPaths -Reports @(
    @{ Name = "SmokeReportPath"; Path = $SmokeReportPath },
    @{ Name = "PreflightReportPath"; Path = $PreflightReportPath },
    @{ Name = "BootstrapSmokeReportPath"; Path = $BootstrapSmokeReportPath },
    @{ Name = "ReadinessReportPath"; Path = $ReadinessReportPath }
)

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

$releaseValue = if ([string]::IsNullOrWhiteSpace($ReleaseId)) { Get-LatestReleaseId } else { $ReleaseId.Trim() }
$operatorValue = Get-OperatorValue -Value $Operator

$previousBootstrapPassword = [Environment]::GetEnvironmentVariable("AdminBootstrap__Password", "Process")
$previousSmokePassword = [Environment]::GetEnvironmentVariable("ADMIN_VPS_SMOKE_ADMIN_PASSWORD", "Process")

try {
    Write-Host "Admin VPS bootstrap+smoke flow is ready to run."
    Write-Host "Environment: $EnvironmentName"
    Write-Host "Provider: $Provider"
    Write-Host "API base URL: $ApiBaseUrl"
    Write-Host "Admin web URL: $AdminWebUrl"
    Write-Host "Admin email: $AdminEmail"
    Write-Host "Operator: $operatorValue"
    Write-Host "Password: [hidden]"
    Write-Host "Smoke report path: $SmokeReportPath"
    Write-Host "Preflight report path: $PreflightReportPath"
    Write-Host "Bootstrap smoke report path: $BootstrapSmokeReportPath"
    Write-Host "Readiness report path: $ReadinessReportPath"
    Write-Host "Release id: $releaseValue"
    Write-Host "Max evidence chain minutes: $maxEvidenceChainMinutesValue"
    Write-Host "Bootstrap reset confirmed: $ConfirmBootstrapReset"

    $readinessArgs = @{
        ApiBaseUrl = $ApiBaseUrl
        AdminWebUrl = $AdminWebUrl
        AdminEmail = $AdminEmail
        AdminPasswordEnvName = $AdminPasswordEnvName
        Provider = $Provider
        ProjectPath = $ProjectPath
        SmokeReportPath = $SmokeReportPath
        PreflightReportPath = $PreflightReportPath
        BootstrapSmokeReportPath = $BootstrapSmokeReportPath
        ReadinessReportPath = $ReadinessReportPath
        EnvironmentName = $EnvironmentName
        Operator = $operatorValue
        ReleaseId = $releaseValue
        FrontendPath = $FrontendPath
        RequireReady = $true
    }

    if (-not [string]::IsNullOrWhiteSpace($ConnectionString)) {
        $readinessArgs["ConnectionString"] = $ConnectionString
    }

    if ($LocalSqlite) {
        $readinessArgs["LocalSqlite"] = $true
    }

    if ($ApplyMigrations) {
        $readinessArgs["ApplyMigrations"] = $true
    }

    if ($ConfirmBootstrapReset) {
        $readinessArgs["ConfirmBootstrapReset"] = $true
    }

    & $readinessScript @readinessArgs | Out-Host

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
        -Operator $operatorValue `
        -ReleaseId $releaseValue `
        -FrontendPath $FrontendPath `
        -MaxEvidenceChainMinutes $maxEvidenceChainMinutesValue `
        -AccountBootstrapChecked

    $now = [DateTimeOffset]::UtcNow

    $bootstrapSmokeReportFullPath = if ([System.IO.Path]::IsPathRooted($BootstrapSmokeReportPath)) {
        [System.IO.Path]::GetFullPath($BootstrapSmokeReportPath)
    }
    else {
        [System.IO.Path]::GetFullPath((Join-Path $repoRoot $BootstrapSmokeReportPath))
    }

    $bootstrapSmokeReportParent = Split-Path -Parent $bootstrapSmokeReportFullPath
    if (-not [string]::IsNullOrWhiteSpace($bootstrapSmokeReportParent) -and -not (Test-Path -LiteralPath $bootstrapSmokeReportParent -PathType Container)) {
        New-Item -ItemType Directory -Path $bootstrapSmokeReportParent | Out-Null
    }

    $providerValue = if ($LocalSqlite) { "Sqlite" } else { $Provider }

    $bootstrapSmokeReport = [ordered]@{
        reportId = "admin-vps-bootstrap-smoke-" + $now.ToString("yyyyMMdd-HHmmss")
        environmentName = $EnvironmentName.Trim()
        apiBaseUrl = $ApiBaseUrl.TrimEnd("/")
        adminWebUrl = $AdminWebUrl.TrimEnd("/")
        adminEmail = $AdminEmail.Trim()
        provider = $providerValue
        bootstrapResetConfirmed = [bool]$ConfirmBootstrapReset
        localSqlite = [bool]$LocalSqlite
        dryRun = $false
        accountBootstrapChecked = $true
        passwordEnvName = $AdminPasswordEnvName
        passwordEnvPresent = $true
        smokeReportPath = $SmokeReportPath
        preflightReportPath = $PreflightReportPath
        readinessReportPath = $ReadinessReportPath
        bootstrapSmokeReportPath = $BootstrapSmokeReportPath
        generatedAt = $now.ToString("o")
        completedAt = ([DateTimeOffset]::UtcNow).ToString("o")
        releaseId = $releaseValue
        operator = $operatorValue
        status = "passed"
        notes = "Sanitized bootstrap+smoke evidence. No credentials, cookies, auth headers, tokens or raw provider secrets are stored."
    }

    [System.IO.File]::WriteAllText(
        $bootstrapSmokeReportFullPath,
        ($bootstrapSmokeReport | ConvertTo-Json -Depth 6),
        [System.Text.UTF8Encoding]::new($false))
    & $bootstrapSmokeReportValidatorScript -ReportPath $bootstrapSmokeReportFullPath -RequirePassed | Out-Host
    & $bootstrapSmokeEvidenceValidatorScript -ReadinessReportPath $ReadinessReportPath -BootstrapSmokeReportPath $bootstrapSmokeReportFullPath -MaxEvidenceChainMinutes $maxEvidenceChainMinutesValue | Out-Host

    Write-Host "Admin VPS bootstrap+smoke flow completed."
    Write-Host "Validated bootstrap smoke report: $BootstrapSmokeReportPath"
}
finally {
    Set-ProcessEnv "AdminBootstrap__Password" $previousBootstrapPassword
    Set-ProcessEnv "ADMIN_VPS_SMOKE_ADMIN_PASSWORD" $previousSmokePassword
}
