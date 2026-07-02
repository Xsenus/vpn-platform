param(
    [Parameter(Mandatory = $true)]
    [string]$ReportPath,

    [switch]$RequireAllPassed
)

$ErrorActionPreference = "Stop"

if (-not (Test-Path -LiteralPath $ReportPath)) {
    throw "Payment provider smoke report was not found: $ReportPath"
}

$fullReportPath = (Resolve-Path -LiteralPath $ReportPath).Path

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

function Resolve-WorkspacePath {
    param([string]$Value)

    if ([string]::IsNullOrWhiteSpace($Value)) {
        return ""
    }

    if ([System.IO.Path]::IsPathRooted($Value)) {
        return [System.IO.Path]::GetFullPath($Value)
    }

    $root = Resolve-RepositoryRoot
    return [System.IO.Path]::GetFullPath((Join-Path $root $Value))
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

$requiredProviders = @(
    "YooKassa",
    "RoboKassa",
    "YooMoney",
    "CloudPayments",
    "TBankAcquiring",
    "Prodamus",
    "Stripe",
    "PayPal"
)

$allowedModes = @("sandbox", "live")
$allowedStatuses = @("passed", "failed", "blocked", "skipped")
$requiredBooleans = @(
    "accountConfigured",
    "checkoutCreated",
    "providerConfirmation",
    "webhookProcessed",
    "subscriptionActivated",
    "refundChecked"
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
    "x-telegram-bot-api-secret-token",
    "begin private key",
    "begin rsa private key",
    "begin openssh private key"
)

$raw = Get-Content -LiteralPath $fullReportPath -Raw -Encoding UTF8
$lowerRaw = $raw.ToLowerInvariant()
foreach ($marker in $secretMarkers) {
    if ($lowerRaw.Contains($marker)) {
        throw "Payment provider smoke report contains forbidden secret marker: $marker"
    }
}

try {
    $report = $raw | ConvertFrom-Json
}
catch {
    throw "Payment provider smoke report is not valid JSON: $($_.Exception.Message)"
}

foreach ($propertyName in @("reportId", "environmentName", "startedAt", "completedAt", "smokeReportPath", "releaseId", "operator", "notes")) {
    if (-not $report.PSObject.Properties.Name.Contains($propertyName)) {
        throw "Payment provider smoke report is missing required field: $propertyName"
    }

    if ([string]::IsNullOrWhiteSpace([string]$report.$propertyName)) {
        throw "Payment provider smoke report field is empty: $propertyName"
    }
}

$resolvedSmokeReportPath = Resolve-WorkspacePath ([string]$report.smokeReportPath)
if (-not [string]::Equals($resolvedSmokeReportPath, $fullReportPath, [System.StringComparison]::OrdinalIgnoreCase)) {
    throw "Payment provider smoke report smokeReportPath must match ReportPath."
}

if ($RequireAllPassed) {
    $latestReleaseId = Get-LatestActiveReleaseId
    if (-not [string]::Equals([string]$report.releaseId, $latestReleaseId, [System.StringComparison]::Ordinal)) {
        throw "Payment provider smoke report releaseId '$($report.releaseId)' must match latest active release '$latestReleaseId' when -RequireAllPassed is used."
    }
}

$startedAt = [DateTimeOffset]::MinValue
$completedAt = [DateTimeOffset]::MinValue
if (-not [DateTimeOffset]::TryParse([string]$report.startedAt, [ref]$startedAt)) {
    throw "Payment provider smoke report field startedAt is not a valid DateTimeOffset."
}

if (-not [DateTimeOffset]::TryParse([string]$report.completedAt, [ref]$completedAt)) {
    throw "Payment provider smoke report field completedAt is not a valid DateTimeOffset."
}

if ($completedAt -lt $startedAt) {
    throw "Payment provider smoke report completedAt must be greater than or equal to startedAt."
}

if ($null -eq $report.providers -or $report.providers.Count -eq 0) {
    throw "Payment provider smoke report must contain providers array."
}

$providerNames = @($report.providers | ForEach-Object { [string]$_.provider })
foreach ($provider in $requiredProviders) {
    if ($providerNames -notcontains $provider) {
        throw "Payment provider smoke report is missing provider: $provider"
    }
}

$duplicates = $providerNames | Group-Object | Where-Object { $_.Count -gt 1 }
if ($duplicates) {
    throw "Payment provider smoke report contains duplicated provider: $($duplicates[0].Name)"
}

foreach ($entry in $report.providers) {
    $provider = [string]$entry.provider
    if ($requiredProviders -notcontains $provider) {
        throw "Payment provider smoke report contains unsupported provider: $provider"
    }

    if ($allowedModes -notcontains ([string]$entry.mode)) {
        throw "Payment provider smoke report provider $provider has unsupported mode: $($entry.mode)"
    }

    $status = [string]$entry.status
    if ($allowedStatuses -notcontains $status) {
        throw "Payment provider smoke report provider $provider has unsupported status: $status"
    }

    foreach ($booleanName in $requiredBooleans) {
        if (-not $entry.PSObject.Properties.Name.Contains($booleanName)) {
            throw "Payment provider smoke report provider $provider is missing boolean field: $booleanName"
        }

        if ($entry.$booleanName -isnot [bool]) {
            throw "Payment provider smoke report provider $provider field $booleanName must be boolean."
        }
    }

    if ([string]::IsNullOrWhiteSpace([string]$entry.evidence)) {
        throw "Payment provider smoke report provider $provider must contain safe evidence."
    }

    if ($RequireAllPassed -and $status -ne "passed") {
        throw "Payment provider smoke report provider $provider must be passed when -RequireAllPassed is used."
    }

    if ($RequireAllPassed) {
        foreach ($booleanName in $requiredBooleans) {
            if (-not $entry.$booleanName) {
                throw "Payment provider smoke report provider $provider field $booleanName must be true when -RequireAllPassed is used."
            }
        }
    }
}

$summary = [ordered]@{
    reportId = $report.reportId
    environmentName = $report.environmentName
    releaseId = $report.releaseId
    smokeReportPath = $resolvedSmokeReportPath
    providers = $providerNames.Count
    passed = @($report.providers | Where-Object { $_.status -eq "passed" }).Count
    failed = @($report.providers | Where-Object { $_.status -eq "failed" }).Count
    blocked = @($report.providers | Where-Object { $_.status -eq "blocked" }).Count
    skipped = @($report.providers | Where-Object { $_.status -eq "skipped" }).Count
}

Write-Host "payment provider smoke report valid $($summary | ConvertTo-Json -Compress)"
