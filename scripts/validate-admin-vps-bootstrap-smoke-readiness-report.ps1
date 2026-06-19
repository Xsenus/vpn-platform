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

$requiredChecks = @(
    "api-base-url",
    "admin-web-url",
    "admin-email",
    "password-env-name",
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

if (@("Postgres", "Sqlite") -notcontains [string]$report.provider) {
    throw "Admin VPS bootstrap smoke readiness report provider must be Postgres or Sqlite."
}

$generatedAt = [DateTimeOffset]::MinValue
if (-not [DateTimeOffset]::TryParse([string]$report.generatedAt, [ref]$generatedAt)) {
    throw "Admin VPS bootstrap smoke readiness report generatedAt is not a valid DateTimeOffset."
}

foreach ($booleanName in @("localSqlite", "applyMigrations", "confirmBootstrapReset", "connectionStringPresent", "passwordEnvPresent", "passwordLengthOk", "readyForBootstrapSmoke")) {
    Assert-BooleanField -Report $report -Name $booleanName
}

if ($RequireReady) {
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
