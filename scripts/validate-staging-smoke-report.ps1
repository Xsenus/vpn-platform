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

if (-not (Test-ReportIsoDate ([string]$report.startedAt))) {
    throw "startedAt must be an ISO-compatible DateTimeOffset value."
}

if (-not (Test-ReportIsoDate ([string]$report.completedAt))) {
    throw "completedAt must be an ISO-compatible DateTimeOffset value."
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

    $status = ([string]$check.status).ToLowerInvariant()
    if ($allowedStatuses -notcontains $status) {
        throw "Check '$($check.id)' has unsupported status '$($check.status)'."
    }

    if ($RequireAllPassed -and $status -ne "passed") {
        throw "Check '$($check.id)' must be passed when -RequireAllPassed is set."
    }

    $checkIds[[string]$check.id] = $true
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
    "BEGIN OPENSSH PRIVATE KEY",
    "BEGIN RSA PRIVATE KEY",
    "x-api-key",
    "bot token",
    "webhook secret"
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
