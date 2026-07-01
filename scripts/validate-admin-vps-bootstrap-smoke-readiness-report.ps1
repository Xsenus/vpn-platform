param(
    [Parameter(Mandatory = $true)]
    [string]$ReportPath,

    [switch]$RequireReady
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot

function Resolve-WorkspacePath {
    param([Parameter(Mandatory = $true)][string]$Path)

    if ([System.IO.Path]::IsPathRooted($Path)) {
        return [System.IO.Path]::GetFullPath($Path)
    }

    return [System.IO.Path]::GetFullPath((Join-Path $repoRoot $Path))
}

function Get-LatestActiveReleaseId {
    $releasesPath = Join-Path $repoRoot "backend/src/VpnPlatform.Api/AppReleases/releases.json"
    $releases = Get-Content -LiteralPath $releasesPath -Raw -Encoding UTF8 | ConvertFrom-Json
    $latest = @($releases | Where-Object { $_.isActive } | Sort-Object -Property { [DateTimeOffset]::Parse([string]$_.releasedAt) } -Descending | Select-Object -First 1)

    if ($latest.Count -eq 0 -or [string]::IsNullOrWhiteSpace([string]$latest[0].releaseId)) {
        throw "Latest active release was not found in AppReleases seed."
    }

    return [string]$latest[0].releaseId
}

function Assert-HttpUrl {
    param(
        [AllowEmptyString()][string]$Value,
        [Parameter(Mandatory = $true)][string]$Name
    )

    $parsed = $null
    $isInvalid = [string]::IsNullOrWhiteSpace($Value) `
        -or -not [Uri]::TryCreate($Value, [UriKind]::Absolute, [ref]$parsed) `
        -or ($parsed.Scheme -ne "http" -and $parsed.Scheme -ne "https")

    if ($isInvalid) {
        throw "Admin VPS bootstrap smoke readiness report field $Name must be an absolute http or https URL."
    }
}

function Assert-BooleanField {
    param(
        [Parameter(Mandatory = $true)]$Report,
        [Parameter(Mandatory = $true)][string]$Name
    )

    if (-not $Report.PSObject.Properties.Name.Contains($Name)) {
        throw "Admin VPS bootstrap smoke readiness report is missing boolean field: $Name"
    }

    if ($Report.$Name -isnot [bool]) {
        throw "Admin VPS bootstrap smoke readiness report field $Name must be boolean."
    }
}

function Assert-Same {
    param(
        [AllowEmptyString()][string]$Left,
        [AllowEmptyString()][string]$Right,
        [Parameter(Mandatory = $true)][string]$Name
    )

    if (-not [string]::Equals($Left, $Right, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "Admin VPS bootstrap smoke readiness report mismatch for $Name."
    }
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

$requiredChecks = @(
    "api-base-url",
    "admin-web-url",
    "admin-email",
    "password-env-name",
    "password-env-name-safe",
    "password-env-present",
    "password-length",
    "provider-supported",
    "local-or-confirm-reset",
    "connection-string",
    "project-file",
    "frontend-directory",
    "package-command",
    "bootstrap-script",
    "smoke-wrapper",
    "readiness-validator",
    "bootstrap-report-validator"
)

$secretMarkers = @(
    "password=",
    "authorization:",
    "bearer ",
    "cookie:",
    "set-cookie:",
    ".env",
    "client_secret",
    "api_key",
    "private header",
    "x-api-key",
    "secretkey",
    "webhook secret",
    "vps_ssh_key",
    "connectionstrings__defaultconnection",
    "begin private key",
    "begin rsa private key",
    "begin openssh private key"
)

$fullReportPath = Resolve-WorkspacePath $ReportPath
if (-not (Test-Path -LiteralPath $fullReportPath -PathType Leaf)) {
    throw "Admin VPS bootstrap smoke readiness report was not found: $fullReportPath"
}

$raw = Get-Content -LiteralPath $fullReportPath -Raw -Encoding UTF8
$lowerRaw = $raw.ToLowerInvariant()
foreach ($marker in $secretMarkers) {
    if ($lowerRaw.Contains($marker)) {
        throw "Admin VPS bootstrap smoke readiness report contains forbidden secret marker: $marker"
    }
}

try {
    $report = $raw | ConvertFrom-Json
}
catch {
    throw "Admin VPS bootstrap smoke readiness report is not valid JSON: $($_.Exception.Message)"
}

foreach ($field in @("reportId", "generatedAt", "environmentName", "operator", "releaseId", "apiBaseUrl", "adminWebUrl", "adminEmail", "provider", "passwordEnvName", "smokeReportPath", "preflightReportPath", "bootstrapSmokeReportPath", "readinessReportPath")) {
    if (-not $report.PSObject.Properties.Name.Contains($field)) {
        throw "Admin VPS bootstrap smoke readiness report is missing required field: $field"
    }

    if ([string]::IsNullOrWhiteSpace([string]$report.$field)) {
        throw "Admin VPS bootstrap smoke readiness report field is empty: $field"
    }
}

if (-not $report.PSObject.Properties.Name.Contains("checks")) {
    throw "Admin VPS bootstrap smoke readiness report is missing required field: checks"
}

Assert-HttpUrl -Value ([string]$report.apiBaseUrl) -Name "apiBaseUrl"
Assert-HttpUrl -Value ([string]$report.adminWebUrl) -Name "adminWebUrl"

if (-not ([string]$report.adminEmail).Contains("@")) {
    throw "Admin VPS bootstrap smoke readiness report field adminEmail must contain an email address."
}

if (-not (Test-AdminPasswordEnvNameValue -Value ([string]$report.passwordEnvName))) {
    throw "Admin VPS bootstrap smoke readiness report field passwordEnvName must be a safe environment variable name containing PASSWORD."
}

if (@("Postgres", "Sqlite") -notcontains [string]$report.provider) {
    throw "Admin VPS bootstrap smoke readiness report provider must be Postgres or Sqlite."
}

if ($report.localSqlite -and -not [string]::Equals([string]$report.provider, "Sqlite", [System.StringComparison]::Ordinal)) {
    throw "Admin VPS bootstrap smoke readiness report provider must be Sqlite when localSqlite is true."
}

$generatedAt = [DateTimeOffset]::MinValue
if (-not [DateTimeOffset]::TryParse([string]$report.generatedAt, [ref]$generatedAt)) {
    throw "Admin VPS bootstrap smoke readiness report generatedAt is not a valid DateTimeOffset."
}

foreach ($booleanName in @("localSqlite", "applyMigrations", "confirmBootstrapReset", "connectionStringPresent", "passwordEnvPresent", "passwordLengthOk", "readyForBootstrapSmoke")) {
    Assert-BooleanField -Report $report -Name $booleanName
}

if ($RequireReady) {
    $latestReleaseId = Get-LatestActiveReleaseId
    if (-not [string]::Equals([string]$report.releaseId, $latestReleaseId, [System.StringComparison]::Ordinal)) {
        throw "Admin VPS bootstrap smoke readiness report releaseId '$($report.releaseId)' must match latest active release '$latestReleaseId' when -RequireReady is used."
    }

    Assert-Same (Resolve-WorkspacePath ([string]$report.readinessReportPath)) $fullReportPath "readinessReportPath"

    foreach ($booleanName in @("passwordEnvPresent", "passwordLengthOk")) {
        if (-not $report.$booleanName) {
            throw "Admin VPS bootstrap smoke readiness report field $booleanName must be true when -RequireReady is used."
        }
    }

    if (-not $report.localSqlite -and -not $report.confirmBootstrapReset) {
        throw "Admin VPS bootstrap smoke readiness report confirmBootstrapReset must be true for non-local database when -RequireReady is used."
    }

    if (-not $report.localSqlite -and -not $report.connectionStringPresent) {
        throw "Admin VPS bootstrap smoke readiness report connectionStringPresent must be true for non-local database when -RequireReady is used."
    }

    if (-not $report.readyForBootstrapSmoke) {
        throw "Admin VPS bootstrap smoke readiness report field readyForBootstrapSmoke must be true when -RequireReady is used."
    }
}

if ($null -eq $report.checks -or $report.checks.Count -eq 0) {
    throw "Admin VPS bootstrap smoke readiness report must contain checks array."
}

$checkNames = @($report.checks | ForEach-Object { [string]$_.name })
foreach ($check in $requiredChecks) {
    if ($checkNames -notcontains $check) {
        throw "Admin VPS bootstrap smoke readiness report is missing check: $check"
    }
}

$duplicates = $checkNames | Group-Object | Where-Object { $_.Count -gt 1 }
if ($duplicates) {
    throw "Admin VPS bootstrap smoke readiness report contains duplicated check: $($duplicates[0].Name)"
}

foreach ($entry in $report.checks) {
    $name = [string]$entry.name
    if ($requiredChecks -notcontains $name) {
        throw "Admin VPS bootstrap smoke readiness report contains unsupported check: $name"
    }

    if ($entry.PSObject.Properties.Name -notcontains "passed" -or $entry.passed -isnot [bool]) {
        throw "Admin VPS bootstrap smoke readiness report check $name must contain boolean passed."
    }

    if ([string]::IsNullOrWhiteSpace([string]$entry.message)) {
        throw "Admin VPS bootstrap smoke readiness report check $name must contain message."
    }

    if ($RequireReady -and -not $entry.passed) {
        throw "Admin VPS bootstrap smoke readiness report check $name must be passed when -RequireReady is used."
    }
}

$failedChecks = @($report.checks | Where-Object { -not $_.passed })
$expectedReadyForBootstrapSmoke = $failedChecks.Count -eq 0
if ($report.readyForBootstrapSmoke -ne $expectedReadyForBootstrapSmoke) {
    throw "Admin VPS bootstrap smoke readiness report field readyForBootstrapSmoke must match checks."
}

$summary = [ordered]@{
    reportId = $report.reportId
    environmentName = $report.environmentName
    releaseId = $report.releaseId
    provider = $report.provider
    readyForBootstrapSmoke = $report.readyForBootstrapSmoke
    checks = $checkNames.Count
    localSqlite = $report.localSqlite
}

Write-Host "admin vps bootstrap smoke readiness report valid $($summary | ConvertTo-Json -Compress)"
