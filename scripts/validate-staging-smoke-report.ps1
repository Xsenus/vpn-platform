param(
    [Parameter(Mandatory = $true)]
    [string]$ReportPath,
    [switch]$RequireAllPassed
)

$ErrorActionPreference = "Stop"

function Assert-ReportValue {
    param(
        [object]$Value,
        [string]$Message
    )

    if ($null -eq $Value -or [string]::IsNullOrWhiteSpace([string]$Value)) {
        throw $Message
    }
}

function ConvertTo-ReportArray {
    param([object]$Value)
    @($Value | ForEach-Object { $_ })
}

function Test-ReportIsoDate {
    param([string]$Value)

    $parsed = [DateTimeOffset]::MinValue
    return [DateTimeOffset]::TryParse($Value, [ref]$parsed)
}

function Assert-ReportHttpUrl {
    param(
        [object]$Value,
        [string]$FieldName,
        [bool]$Required
    )

    if ($null -eq $Value -or [string]::IsNullOrWhiteSpace([string]$Value)) {
        if ($Required) {
            throw "Required report field '$FieldName' is empty."
        }

        return
    }

    $parsed = $null
    $isAbsoluteUrl = [Uri]::TryCreate([string]$Value, [UriKind]::Absolute, [ref]$parsed)
    $isHttpUrl = $isAbsoluteUrl -and ($parsed.Scheme -eq "http" -or $parsed.Scheme -eq "https")
    if (-not $isHttpUrl) {
        throw "Report field '$FieldName' must be an absolute http or https URL."
    }
}

function Resolve-RepositoryRoot {
    $directory = Get-Item -LiteralPath $PSScriptRoot
    while ($null -ne $directory) {
        if ((Test-Path -LiteralPath (Join-Path $directory.FullName "README.md")) -and
            (Test-Path -LiteralPath (Join-Path $directory.FullName "backend/src/VpnPlatform.Api/AppReleases/releases.json"))) {
            return $directory.FullName
        }

        $directory = $directory.Parent
    }

    throw "Repository root was not found."
}

function Get-LatestActiveReleaseId {
    $root = Resolve-RepositoryRoot
    $releasesPath = Join-Path $root "backend/src/VpnPlatform.Api/AppReleases/releases.json"
    $releases = Get-Content -LiteralPath $releasesPath -Raw -Encoding UTF8 | ConvertFrom-Json
    $latest = @($releases | Where-Object { $_.isActive } | Sort-Object -Property { [DateTimeOffset]::Parse([string]$_.releasedAt) } -Descending | Select-Object -First 1)

    if ($latest.Count -eq 0 -or [string]::IsNullOrWhiteSpace([string]$latest[0].releaseId)) {
        throw "Latest active release was not found in AppReleases seed."
    }

    return [string]$latest[0].releaseId
}

$fullPath = if (Test-Path -LiteralPath $ReportPath) {
    (Resolve-Path -LiteralPath $ReportPath).Path
}
else {
    throw "Report file was not found: $ReportPath"
}

$raw = Get-Content -LiteralPath $fullPath -Raw -Encoding UTF8
if ([string]::IsNullOrWhiteSpace($raw)) {
    throw "Report file is empty: $fullPath"
}

$report = $raw | ConvertFrom-Json

foreach ($field in @("reportId", "environmentName", "apiBaseUrl", "startedAt", "completedAt", "releaseId", "operator")) {
    Assert-ReportValue $report.$field "Required report field '$field' is empty."
}

if ($RequireAllPassed) {
    $latestReleaseId = Get-LatestActiveReleaseId
    if (-not [string]::Equals([string]$report.releaseId, $latestReleaseId, [System.StringComparison]::Ordinal)) {
        throw "Report releaseId '$($report.releaseId)' must match latest active release '$latestReleaseId' when -RequireAllPassed is set."
    }
}

Assert-ReportHttpUrl -Value $report.apiBaseUrl -FieldName "apiBaseUrl" -Required $true
Assert-ReportHttpUrl -Value $report.publicWebUrl -FieldName "publicWebUrl" -Required $false
Assert-ReportHttpUrl -Value $report.cabinetWebUrl -FieldName "cabinetWebUrl" -Required $false
Assert-ReportHttpUrl -Value $report.adminWebUrl -FieldName "adminWebUrl" -Required $false

$startedAt = [DateTimeOffset]::MinValue
if (-not [DateTimeOffset]::TryParse([string]$report.startedAt, [ref]$startedAt)) {
    throw "startedAt must be an ISO-compatible DateTimeOffset value."
}

$completedAt = [DateTimeOffset]::MinValue
if (-not [DateTimeOffset]::TryParse([string]$report.completedAt, [ref]$completedAt)) {
    throw "completedAt must be an ISO-compatible DateTimeOffset value."
}

if ($completedAt -lt $startedAt) {
    throw "completedAt must be greater than or equal to startedAt."
}

$checks = ConvertTo-ReportArray $report.checks
if ($checks.Count -eq 0) {
    throw "Report must contain at least one check."
}

$requiredCheckIds = @(
    "deploy",
    "health-live",
    "health-ready",
    "public-web",
    "cabinet-web",
    "admin-web",
    "admin-login",
    "tariffs",
    "payment-providers",
    "checkout",
    "payment-init",
    "provider-confirmation",
    "subscription",
    "vpn-access",
    "support",
    "no-console-errors",
    "secret-rotation",
    "no-secret-leak"
)

$allowedStatuses = @("passed", "failed", "blocked", "skipped")
$checkIds = @{}
foreach ($check in $checks) {
    Assert-ReportValue $check.id "Every check must contain id."
    Assert-ReportValue $check.status "Check '$($check.id)' must contain status."
    Assert-ReportValue $check.evidence "Check '$($check.id)' must contain evidence."

    $checkId = [string]$check.id
    if ($checkIds.ContainsKey($checkId)) {
        throw "Duplicate staging smoke check id '$checkId'."
    }

    $status = ([string]$check.status).ToLowerInvariant()
    if ($allowedStatuses -notcontains $status) {
        throw "Check '$($check.id)' has unsupported status '$($check.status)'."
    }

    if ($RequireAllPassed -and $status -ne "passed") {
        throw "Check '$($check.id)' must be passed when -RequireAllPassed is set."
    }

    if ($RequireAllPassed -and ([string]$check.evidence).IndexOf("TODO", [System.StringComparison]::OrdinalIgnoreCase) -ge 0) {
        throw "Check '$($check.id)' must contain real evidence without TODO placeholders when -RequireAllPassed is set."
    }

    $checkIds[$checkId] = $true
}

foreach ($requiredId in $requiredCheckIds) {
    if (-not $checkIds.ContainsKey($requiredId)) {
        throw "Required staging smoke check '$requiredId' is missing."
    }
}

$secretMarkers = @(
    "password=",
    "Authorization:",
    "Bearer ",
    "Cookie:",
    "Set-Cookie:",
    "BEGIN OPENSSH PRIVATE KEY",
    "BEGIN RSA PRIVATE KEY",
    "x-api-key",
    "api_key",
    "api-key",
    "client_secret",
    "client-secret",
    "private header",
    "bot token",
    "webhook secret",
    "X-Telegram-Bot-Api-Secret-Token",
    ".env",
    "PRODUCTION_ENV_FILE",
    "VPS_SSH_KEY"
)

foreach ($marker in $secretMarkers) {
    if ($raw.IndexOf($marker, [System.StringComparison]::OrdinalIgnoreCase) -ge 0) {
        throw "Report contains a forbidden secret marker: $marker"
    }
}

$summary = @{
    reportId = [string]$report.reportId
    environmentName = [string]$report.environmentName
    releaseId = [string]$report.releaseId
    checks = $checks.Count
    passed = ($checks | Where-Object { ([string]$_.status).ToLowerInvariant() -eq "passed" }).Count
    blocked = ($checks | Where-Object { ([string]$_.status).ToLowerInvariant() -eq "blocked" }).Count
    failed = ($checks | Where-Object { ([string]$_.status).ToLowerInvariant() -eq "failed" }).Count
    skipped = ($checks | Where-Object { ([string]$_.status).ToLowerInvariant() -eq "skipped" }).Count
}

Write-Output ("staging smoke report valid " + ($summary | ConvertTo-Json -Compress))
