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

function Assert-KnownReleaseId {
    param([Parameter(Mandatory = $true)][string]$Value)

    $releasesPath = Join-Path $repoRoot "backend/src/VpnPlatform.Api/AppReleases/releases.json"
    if (-not (Test-Path -LiteralPath $releasesPath -PathType Leaf)) {
        return
    }

    $releases = Get-Content -LiteralPath $releasesPath -Raw -Encoding UTF8 | ConvertFrom-Json
    $matchedRelease = @($releases | Where-Object { [string]$_.releaseId -eq $Value } | Select-Object -First 1)
    if ($matchedRelease.Count -eq 0) {
        throw "ReleaseId must exist in backend/src/VpnPlatform.Api/AppReleases/releases.json."
    }
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

function Get-HttpUrlValue {
    param([AllowEmptyString()][string]$Value)

    if ([string]::IsNullOrWhiteSpace($Value)) {
        return ""
    }

    return $Value.Trim()
}

function Assert-AdminEmail {
    param([AllowEmptyString()][string]$Value)

    if ([string]::IsNullOrWhiteSpace($Value) -or -not $Value.Trim().Contains("@")) {
        throw "AdminEmail must contain an email address."
    }
}

function Get-AdminEmailValue {
    param([AllowEmptyString()][string]$Value)

    if ([string]::IsNullOrWhiteSpace($Value)) {
        return ""
    }

    return $Value.Trim()
}

function Get-AdminPasswordEnvNameValue {
    param([AllowEmptyString()][string]$Value)

    if ([string]::IsNullOrWhiteSpace($Value)) {
        return ""
    }

    return $Value.Trim()
}

function Test-AdminPasswordEnvNameValue {
    param([AllowEmptyString()][string]$Value)

    if ([string]::IsNullOrWhiteSpace($Value)) {
        return $false
    }

    if (-not [System.Text.RegularExpressions.Regex]::IsMatch($Value, "^[A-Za-z_][A-Za-z0-9_]*$")) {
        return $false
    }

    return $Value.IndexOf("PASSWORD", [System.StringComparison]::OrdinalIgnoreCase) -ge 0
}

function Assert-AdminPasswordEnvNameValue {
    param([AllowEmptyString()][string]$Value)

    if (-not (Test-AdminPasswordEnvNameValue -Value $Value)) {
        throw "Admin password env name must be a safe environment variable name containing PASSWORD."
    }
}

function Get-ReportPathValue {
    param([AllowEmptyString()][string]$Value)

    if ([string]::IsNullOrWhiteSpace($Value)) {
        return ""
    }

    return $Value.Trim()
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

function Get-ReportPathFullName {
    param(
        [AllowEmptyString()][string]$Path,
        [Parameter(Mandatory = $true)][string]$Name
    )

    $pathValue = Get-ReportPathValue -Value $Path
    if ([string]::IsNullOrWhiteSpace($pathValue)) {
        throw "$Name must not be empty."
    }

    $candidate = if ([System.IO.Path]::IsPathRooted($pathValue)) {
        $pathValue
    }
    else {
        Join-Path $repoRoot $pathValue
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

function Get-EnvironmentNameValue {
    param([AllowEmptyString()][string]$Value)

    if ([string]::IsNullOrWhiteSpace($Value)) {
        return "Production"
    }

    return $Value.Trim()
}

function Get-ProviderValue {
    param(
        [AllowEmptyString()][string]$Value,
        [Parameter(Mandatory = $true)][bool]$UseLocalSqlite
    )

    if ($UseLocalSqlite) {
        return "Sqlite"
    }

    if ([string]::IsNullOrWhiteSpace($Value)) {
        throw "Provider must be Postgres or Sqlite."
    }

    $trimmed = $Value.Trim()
    if ([string]::Equals($trimmed, "Postgres", [System.StringComparison]::OrdinalIgnoreCase)) {
        return "Postgres"
    }

    if ([string]::Equals($trimmed, "Sqlite", [System.StringComparison]::OrdinalIgnoreCase)) {
        return "Sqlite"
    }

    throw "Provider must be Postgres or Sqlite."
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
$smokeReportPathValue = Get-ReportPathValue -Value $SmokeReportPath
$preflightReportPathValue = Get-ReportPathValue -Value $PreflightReportPath
$bootstrapSmokeReportPathValue = Get-ReportPathValue -Value $BootstrapSmokeReportPath
$readinessReportPathValue = Get-ReportPathValue -Value $ReadinessReportPath
$projectPathValue = Get-WorkspacePathValue -Value $ProjectPath
$frontendPathValue = Get-WorkspacePathValue -Value $FrontendPath
$dataProtectionKeyPathValue = Get-WorkspacePathValue -Value $DataProtectionKeyPath
Assert-DistinctReportPaths -Reports @(
    @{ Name = "SmokeReportPath"; Path = $smokeReportPathValue },
    @{ Name = "PreflightReportPath"; Path = $preflightReportPathValue },
    @{ Name = "BootstrapSmokeReportPath"; Path = $bootstrapSmokeReportPathValue },
    @{ Name = "ReadinessReportPath"; Path = $readinessReportPathValue }
)
$providerValue = Get-ProviderValue -Value $Provider -UseLocalSqlite ([bool]$LocalSqlite)
$adminPasswordEnvNameValue = Get-AdminPasswordEnvNameValue -Value $AdminPasswordEnvName

if ([string]::IsNullOrWhiteSpace($AdminEmail)) {
    throw "Admin email is required."
}

if ([string]::IsNullOrWhiteSpace($adminPasswordEnvNameValue)) {
    throw "Admin password env name is required."
}

Assert-AdminPasswordEnvNameValue -Value $adminPasswordEnvNameValue

$password = [Environment]::GetEnvironmentVariable($adminPasswordEnvNameValue, "Process")
if ([string]::IsNullOrWhiteSpace($password)) {
    throw "Admin password env '$adminPasswordEnvNameValue' is required."
}

if ($password.Length -lt 16) {
    throw "Admin password env '$adminPasswordEnvNameValue' must contain at least 16 characters."
}

if (-not $LocalSqlite -and -not $ConfirmBootstrapReset) {
    throw "Pass -ConfirmBootstrapReset to run admin bootstrap/reset against a non-local database."
}

if (-not $LocalSqlite -and [string]::IsNullOrWhiteSpace($ConnectionString)) {
    throw "Connection string is required for non-local admin bootstrap/reset."
}

$releaseValue = if ([string]::IsNullOrWhiteSpace($ReleaseId)) { Get-LatestReleaseId } else { $ReleaseId.Trim() }
Assert-KnownReleaseId -Value $releaseValue
$operatorValue = Get-OperatorValue -Value $Operator
$environmentNameValue = Get-EnvironmentNameValue -Value $EnvironmentName
$apiBaseUrlValue = Get-HttpUrlValue -Value $ApiBaseUrl
$adminWebUrlValue = Get-HttpUrlValue -Value $AdminWebUrl
$adminEmailValue = Get-AdminEmailValue -Value $AdminEmail
$displayNameValue = Get-AdminBootstrapTextValue -Value $DisplayName -DefaultValue "Platform Admin"
$rolesCsvValue = Get-AdminBootstrapTextValue -Value $RolesCsv -DefaultValue "SuperAdmin"

$previousBootstrapPassword = [Environment]::GetEnvironmentVariable("AdminBootstrap__Password", "Process")
$previousSmokePassword = [Environment]::GetEnvironmentVariable("ADMIN_VPS_SMOKE_ADMIN_PASSWORD", "Process")

try {
    Write-Host "Admin VPS bootstrap+smoke flow is ready to run."
    Write-Host "Environment: $environmentNameValue"
    Write-Host "Provider: $providerValue"
    Write-Host "API base URL: $apiBaseUrlValue"
    Write-Host "Admin web URL: $adminWebUrlValue"
    Write-Host "Admin email: $adminEmailValue"
    Write-Host "Operator: $operatorValue"
    Write-Host "Password: [hidden]"
    Write-Host "Smoke report path: $smokeReportPathValue"
    Write-Host "Preflight report path: $preflightReportPathValue"
    Write-Host "Bootstrap smoke report path: $bootstrapSmokeReportPathValue"
    Write-Host "Readiness report path: $readinessReportPathValue"
    Write-Host "Release id: $releaseValue"
    Write-Host "Max evidence chain minutes: $maxEvidenceChainMinutesValue"
    Write-Host "Bootstrap reset confirmed: $ConfirmBootstrapReset"

    $readinessArgs = @{
        ApiBaseUrl = $apiBaseUrlValue
        AdminWebUrl = $adminWebUrlValue
        AdminEmail = $adminEmailValue
        AdminPasswordEnvName = $adminPasswordEnvNameValue
        Provider = $providerValue
        ProjectPath = $projectPathValue
        SmokeReportPath = $smokeReportPathValue
        PreflightReportPath = $preflightReportPathValue
        BootstrapSmokeReportPath = $bootstrapSmokeReportPathValue
        ReadinessReportPath = $readinessReportPathValue
        EnvironmentName = $environmentNameValue
        Operator = $operatorValue
        ReleaseId = $releaseValue
        FrontendPath = $frontendPathValue
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
        EnvironmentName = $environmentNameValue
        Email = $adminEmailValue
        DisplayName = $displayNameValue
        RolesCsv = $rolesCsvValue
        Provider = $providerValue
        ProjectPath = $projectPathValue
    }

    if (-not [string]::IsNullOrWhiteSpace($ConnectionString)) {
        $bootstrapArgs["ConnectionString"] = $ConnectionString
    }

    if (-not [string]::IsNullOrWhiteSpace($dataProtectionKeyPathValue)) {
        $bootstrapArgs["DataProtectionKeyPath"] = $dataProtectionKeyPathValue
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
        -ApiBaseUrl $apiBaseUrlValue `
        -AdminWebUrl $adminWebUrlValue `
        -AdminEmail $adminEmailValue `
        -SmokeReportPath $smokeReportPathValue `
        -PreflightReportPath $preflightReportPathValue `
        -EnvironmentName $environmentNameValue `
        -Operator $operatorValue `
        -ReleaseId $releaseValue `
        -FrontendPath $frontendPathValue `
        -MaxEvidenceChainMinutes $maxEvidenceChainMinutesValue `
        -AccountBootstrapChecked

    $now = [DateTimeOffset]::UtcNow

    $bootstrapSmokeReportFullPath = if ([System.IO.Path]::IsPathRooted($bootstrapSmokeReportPathValue)) {
        [System.IO.Path]::GetFullPath($bootstrapSmokeReportPathValue)
    }
    else {
        [System.IO.Path]::GetFullPath((Join-Path $repoRoot $bootstrapSmokeReportPathValue))
    }

    $bootstrapSmokeReportParent = Split-Path -Parent $bootstrapSmokeReportFullPath
    if (-not [string]::IsNullOrWhiteSpace($bootstrapSmokeReportParent) -and -not (Test-Path -LiteralPath $bootstrapSmokeReportParent -PathType Container)) {
        New-Item -ItemType Directory -Path $bootstrapSmokeReportParent | Out-Null
    }

    $bootstrapSmokeReport = [ordered]@{
        reportId = "admin-vps-bootstrap-smoke-" + $now.ToString("yyyyMMdd-HHmmss")
        environmentName = $environmentNameValue
        apiBaseUrl = $apiBaseUrlValue.TrimEnd("/")
        adminWebUrl = $adminWebUrlValue.TrimEnd("/")
        adminEmail = $adminEmailValue
        provider = $providerValue
        bootstrapResetConfirmed = [bool]$ConfirmBootstrapReset
        localSqlite = [bool]$LocalSqlite
        dryRun = $false
        accountBootstrapChecked = $true
        passwordEnvName = $adminPasswordEnvNameValue
        passwordEnvPresent = $true
        smokeReportPath = $smokeReportPathValue
        preflightReportPath = $preflightReportPathValue
        readinessReportPath = $readinessReportPathValue
        bootstrapSmokeReportPath = $bootstrapSmokeReportPathValue
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
    & $bootstrapSmokeEvidenceValidatorScript -ReadinessReportPath $readinessReportPathValue -BootstrapSmokeReportPath $bootstrapSmokeReportFullPath -MaxEvidenceChainMinutes $maxEvidenceChainMinutesValue | Out-Host

    Write-Host "Admin VPS bootstrap+smoke flow completed."
    Write-Host "Validated bootstrap smoke report: $bootstrapSmokeReportPathValue"
}
finally {
    Set-ProcessEnv "AdminBootstrap__Password" $previousBootstrapPassword
    Set-ProcessEnv "ADMIN_VPS_SMOKE_ADMIN_PASSWORD" $previousSmokePassword
}
