param(
    [Parameter(Mandatory = $true)]
    [string]$ReportPath,

    [switch]$RequireAllPassed
)

$ErrorActionPreference = "Stop"

if (-not (Test-Path -LiteralPath $ReportPath)) {
    throw "VPN live smoke report was not found: $ReportPath"
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

$requiredChecks = @(
    "panel-connection",
    "inbound-sync",
    "node-ready",
    "order-create",
    "payment-webhook",
    "subscription-activated",
    "vpn-client-created",
    "access-uri-qr",
    "fail-closed-disabled-inbound"
)

$allowedStatuses = @("passed", "failed", "blocked", "skipped")
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
    "vless://",
    "vmess://",
    "trojan://",
    "begin private key",
    "begin rsa private key",
    "begin openssh private key"
)

function Assert-ReportHttpUrl {
    param(
        [string]$Value,
        [string]$Name
    )

    $parsed = $null
    $isInvalid = [string]::IsNullOrWhiteSpace($Value) -or -not [Uri]::TryCreate($Value, [UriKind]::Absolute, [ref]$parsed) -or ($parsed.Scheme -ne "http" -and $parsed.Scheme -ne "https")
    if ($isInvalid) {
        throw "VPN live smoke report field $Name must be an absolute http or https URL."
    }
}

$raw = Get-Content -LiteralPath $ReportPath -Raw -Encoding UTF8
$lowerRaw = $raw.ToLowerInvariant()
foreach ($marker in $secretMarkers) {
    if ($lowerRaw.Contains($marker)) {
        throw "VPN live smoke report contains forbidden secret marker: $marker"
    }
}

try {
    $report = $raw | ConvertFrom-Json
}
catch {
    throw "VPN live smoke report is not valid JSON: $($_.Exception.Message)"
}

foreach ($propertyName in @("reportId", "environmentName", "apiBaseUrl", "adminWebUrl", "x3uiPanelUrl", "startedAt", "completedAt", "releaseId", "operator", "notes")) {
    if (-not $report.PSObject.Properties.Name.Contains($propertyName)) {
        throw "VPN live smoke report is missing required field: $propertyName"
    }

    if ([string]::IsNullOrWhiteSpace([string]$report.$propertyName)) {
        throw "VPN live smoke report field is empty: $propertyName"
    }
}

if ($RequireAllPassed) {
    $latestReleaseId = Get-LatestActiveReleaseId
    if (-not [string]::Equals([string]$report.releaseId, $latestReleaseId, [System.StringComparison]::Ordinal)) {
        throw "VPN live smoke report releaseId '$($report.releaseId)' must match latest active release '$latestReleaseId' when -RequireAllPassed is used."
    }
}

Assert-ReportHttpUrl -Value ([string]$report.apiBaseUrl) -Name "apiBaseUrl"
Assert-ReportHttpUrl -Value ([string]$report.adminWebUrl) -Name "adminWebUrl"
Assert-ReportHttpUrl -Value ([string]$report.x3uiPanelUrl) -Name "x3uiPanelUrl"

$startedAt = [DateTimeOffset]::MinValue
$completedAt = [DateTimeOffset]::MinValue
if (-not [DateTimeOffset]::TryParse([string]$report.startedAt, [ref]$startedAt)) {
    throw "VPN live smoke report field startedAt is not a valid DateTimeOffset."
}

if (-not [DateTimeOffset]::TryParse([string]$report.completedAt, [ref]$completedAt)) {
    throw "VPN live smoke report field completedAt is not a valid DateTimeOffset."
}

if ($completedAt -lt $startedAt) {
    throw "VPN live smoke report completedAt must be greater than or equal to startedAt."
}

foreach ($booleanName in @("panelConnected", "inboundSynced", "nodeReady", "productionProvisioningEnabled", "noSandboxFallback", "failClosedChecked")) {
    if (-not $report.PSObject.Properties.Name.Contains($booleanName)) {
        throw "VPN live smoke report is missing boolean field: $booleanName"
    }

    if ($report.$booleanName -isnot [bool]) {
        throw "VPN live smoke report field $booleanName must be boolean."
    }

    if ($RequireAllPassed -and -not $report.$booleanName) {
        throw "VPN live smoke report field $booleanName must be true when -RequireAllPassed is used."
    }
}

if ($null -eq $report.checks -or $report.checks.Count -eq 0) {
    throw "VPN live smoke report must contain checks array."
}

$checkIds = @($report.checks | ForEach-Object { [string]$_.id })
foreach ($check in $requiredChecks) {
    if ($checkIds -notcontains $check) {
        throw "VPN live smoke report is missing check: $check"
    }
}

$duplicates = $checkIds | Group-Object | Where-Object { $_.Count -gt 1 }
if ($duplicates) {
    throw "VPN live smoke report contains duplicated check: $($duplicates[0].Name)"
}

foreach ($entry in $report.checks) {
    $check = [string]$entry.id
    if ($requiredChecks -notcontains $check) {
        throw "VPN live smoke report contains unsupported check: $check"
    }

    $status = [string]$entry.status
    if ($allowedStatuses -notcontains $status) {
        throw "VPN live smoke report check $check has unsupported status: $status"
    }

    if ([string]::IsNullOrWhiteSpace([string]$entry.evidence)) {
        throw "VPN live smoke report check $check must contain safe evidence."
    }

    if ($RequireAllPassed -and $status -ne "passed") {
        throw "VPN live smoke report check $check must be passed when -RequireAllPassed is used."
    }
}

$summary = [ordered]@{
    reportId = $report.reportId
    environmentName = $report.environmentName
    releaseId = $report.releaseId
    checks = $checkIds.Count
    passed = @($report.checks | Where-Object { $_.status -eq "passed" }).Count
    failed = @($report.checks | Where-Object { $_.status -eq "failed" }).Count
    blocked = @($report.checks | Where-Object { $_.status -eq "blocked" }).Count
    skipped = @($report.checks | Where-Object { $_.status -eq "skipped" }).Count
}

Write-Host "vpn live smoke report valid $($summary | ConvertTo-Json -Compress)"
