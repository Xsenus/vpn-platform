param(
    [Parameter(Mandatory = $true)]
    [string]$ReportPath,

    [switch]$RequirePassed
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
    $isInvalid = [string]::IsNullOrWhiteSpace($Value) -or -not [Uri]::TryCreate($Value, [UriKind]::Absolute, [ref]$parsed) -or ($parsed.Scheme -ne "http" -and $parsed.Scheme -ne "https")
    if ($isInvalid) {
        throw "Admin VPS bootstrap smoke report field $Name must be an absolute http or https URL."
    }
}

function Assert-BooleanField {
    param(
        [Parameter(Mandatory = $true)]$Report,
        [Parameter(Mandatory = $true)][string]$Name
    )

    if (-not $Report.PSObject.Properties.Name.Contains($Name)) {
        throw "Admin VPS bootstrap smoke report is missing boolean field: $Name"
    }

    if ($Report.$Name -isnot [bool]) {
        throw "Admin VPS bootstrap smoke report field $Name must be boolean."
    }
}

function Assert-Same {
    param(
        [AllowEmptyString()][string]$Left,
        [AllowEmptyString()][string]$Right,
        [Parameter(Mandatory = $true)][string]$Name
    )

    if (-not [string]::Equals($Left, $Right, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "Admin VPS bootstrap smoke report mismatch for $Name."
    }
}

$fullReportPath = Resolve-WorkspacePath $ReportPath
if (-not (Test-Path -LiteralPath $fullReportPath -PathType Leaf)) {
    throw "Admin VPS bootstrap smoke report was not found: $fullReportPath"
}

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
    "begin private key",
    "begin rsa private key",
    "begin openssh private key"
)

$raw = Get-Content -LiteralPath $fullReportPath -Raw -Encoding UTF8
$lowerRaw = $raw.ToLowerInvariant()
foreach ($marker in $secretMarkers) {
    if ($lowerRaw.Contains($marker)) {
        throw "Admin VPS bootstrap smoke report contains forbidden secret marker: $marker"
    }
}

try {
    $report = $raw | ConvertFrom-Json
}
catch {
    throw "Admin VPS bootstrap smoke report is not valid JSON: $($_.Exception.Message)"
}

foreach ($field in @("reportId", "environmentName", "apiBaseUrl", "adminWebUrl", "adminEmail", "provider", "passwordEnvName", "smokeReportPath", "preflightReportPath", "generatedAt", "completedAt", "releaseId", "operator", "status")) {
    if (-not $report.PSObject.Properties.Name.Contains($field)) {
        throw "Admin VPS bootstrap smoke report is missing required field: $field"
    }

    if ([string]::IsNullOrWhiteSpace([string]$report.$field)) {
        throw "Admin VPS bootstrap smoke report field is empty: $field"
    }
}

Assert-HttpUrl -Value ([string]$report.apiBaseUrl) -Name "apiBaseUrl"
Assert-HttpUrl -Value ([string]$report.adminWebUrl) -Name "adminWebUrl"

foreach ($booleanName in @("bootstrapResetConfirmed", "localSqlite", "dryRun", "accountBootstrapChecked", "passwordEnvPresent")) {
    Assert-BooleanField -Report $report -Name $booleanName
}

$generatedAt = [DateTimeOffset]::MinValue
$completedAt = [DateTimeOffset]::MinValue
if (-not [DateTimeOffset]::TryParse([string]$report.generatedAt, [ref]$generatedAt)) {
    throw "Admin VPS bootstrap smoke report generatedAt is not a valid DateTimeOffset."
}

if (-not [DateTimeOffset]::TryParse([string]$report.completedAt, [ref]$completedAt)) {
    throw "Admin VPS bootstrap smoke report completedAt is not a valid DateTimeOffset."
}

if ($completedAt -lt $generatedAt) {
    throw "Admin VPS bootstrap smoke report completedAt must be greater than or equal to generatedAt."
}

if ($RequirePassed) {
    if ([string]$report.status -ne "passed") {
        throw "Admin VPS bootstrap smoke report status must be passed when -RequirePassed is used."
    }

    if ($report.dryRun) {
        throw "Admin VPS bootstrap smoke report dryRun must be false when -RequirePassed is used."
    }

    if (-not $report.accountBootstrapChecked) {
        throw "Admin VPS bootstrap smoke report accountBootstrapChecked must be true when -RequirePassed is used."
    }

    if (-not $report.passwordEnvPresent) {
        throw "Admin VPS bootstrap smoke report passwordEnvPresent must be true when -RequirePassed is used."
    }

    if (-not $report.localSqlite -and -not $report.bootstrapResetConfirmed) {
        throw "Admin VPS bootstrap smoke report bootstrapResetConfirmed must be true for non-local database when -RequirePassed is used."
    }

    $evidenceValidator = Join-Path $repoRoot "scripts/validate-admin-vps-smoke-evidence.ps1"
    & $evidenceValidator `
        -PreflightReportPath ([string]$report.preflightReportPath) `
        -SmokeReportPath ([string]$report.smokeReportPath) | Out-Host

    $preflightReport = Get-Content -LiteralPath (Resolve-WorkspacePath ([string]$report.preflightReportPath)) -Raw -Encoding UTF8 | ConvertFrom-Json
    $smokeReport = Get-Content -LiteralPath (Resolve-WorkspacePath ([string]$report.smokeReportPath)) -Raw -Encoding UTF8 | ConvertFrom-Json
    Assert-Same ([string]$report.releaseId) ([string]$preflightReport.releaseId) "preflight releaseId"
    Assert-Same ([string]$report.releaseId) ([string]$smokeReport.releaseId) "smoke releaseId"
}

$summary = [ordered]@{
    reportId = $report.reportId
    environmentName = $report.environmentName
    releaseId = $report.releaseId
    provider = $report.provider
    status = $report.status
    localSqlite = $report.localSqlite
    smokeReportPath = $report.smokeReportPath
}

Write-Host "admin vps bootstrap smoke report valid $($summary | ConvertTo-Json -Compress)"
