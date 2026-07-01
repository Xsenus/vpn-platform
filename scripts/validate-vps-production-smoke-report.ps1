param(
    [Parameter(Mandatory = $true)]
    [string]$ReportPath,

    [switch]$RequireAllPassed
)

$ErrorActionPreference = "Stop"

if (-not (Test-Path -LiteralPath $ReportPath)) {
    throw "VPS production smoke report was not found: $ReportPath"
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

$requiredSteps = @(
    "health-live",
    "health-ready",
    "web-public",
    "web-cabinet",
    "web-admin",
    "admin-login",
    "public-checkout",
    "payment-init",
    "payment-confirmation",
    "subscription-active",
    "vpn-access",
    "latest-release"
)

$requiredBooleans = @(
    "liveHealthPassed",
    "readyHealthPassed",
    "adminLoginPassed",
    "checkoutCreated",
    "paymentInitialized",
    "paymentConfirmed",
    "subscriptionActivated",
    "vpnAccessIssued",
    "latestReleaseMatched",
    "noJsErrors",
    "noSecretsInEvidence"
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
    "begin private key",
    "begin rsa private key",
    "begin openssh private key",
    "vless://",
    "vmess://",
    "trojan://"
)

function Assert-ReportHttpUrl {
    param(
        [string]$Value,
        [string]$Name
    )

    $parsed = $null
    $isInvalid = [string]::IsNullOrWhiteSpace($Value) -or -not [Uri]::TryCreate($Value, [UriKind]::Absolute, [ref]$parsed) -or ($parsed.Scheme -ne "http" -and $parsed.Scheme -ne "https")
    if ($isInvalid) {
        throw "VPS production smoke report field $Name must be an absolute http or https URL."
    }
}

$raw = Get-Content -LiteralPath $ReportPath -Raw -Encoding UTF8
$lowerRaw = $raw.ToLowerInvariant()
foreach ($marker in $secretMarkers) {
    if ($lowerRaw.Contains($marker)) {
        throw "VPS production smoke report contains forbidden secret marker: $marker"
    }
}

try {
    $report = $raw | ConvertFrom-Json
}
catch {
    throw "VPS production smoke report is not valid JSON: $($_.Exception.Message)"
}

foreach ($propertyName in @("reportId", "environmentName", "apiBaseUrl", "publicWebUrl", "cabinetWebUrl", "adminWebUrl", "startedAt", "completedAt", "releaseId", "operator", "notes")) {
    if (-not $report.PSObject.Properties.Name.Contains($propertyName)) {
        throw "VPS production smoke report is missing required field: $propertyName"
    }

    if ([string]::IsNullOrWhiteSpace([string]$report.$propertyName)) {
        throw "VPS production smoke report field is empty: $propertyName"
    }
}

if ($RequireAllPassed) {
    $latestReleaseId = Get-LatestActiveReleaseId
    if (-not [string]::Equals([string]$report.releaseId, $latestReleaseId, [System.StringComparison]::Ordinal)) {
        throw "VPS production smoke report releaseId '$($report.releaseId)' must match latest active release '$latestReleaseId' when -RequireAllPassed is used."
    }
}

Assert-ReportHttpUrl -Value ([string]$report.apiBaseUrl) -Name "apiBaseUrl"
Assert-ReportHttpUrl -Value ([string]$report.publicWebUrl) -Name "publicWebUrl"
Assert-ReportHttpUrl -Value ([string]$report.cabinetWebUrl) -Name "cabinetWebUrl"
Assert-ReportHttpUrl -Value ([string]$report.adminWebUrl) -Name "adminWebUrl"

$startedAt = [DateTimeOffset]::MinValue
$completedAt = [DateTimeOffset]::MinValue
if (-not [DateTimeOffset]::TryParse([string]$report.startedAt, [ref]$startedAt)) {
    throw "VPS production smoke report field startedAt is not a valid DateTimeOffset."
}

if (-not [DateTimeOffset]::TryParse([string]$report.completedAt, [ref]$completedAt)) {
    throw "VPS production smoke report field completedAt is not a valid DateTimeOffset."
}

if ($completedAt -lt $startedAt) {
    throw "VPS production smoke report completedAt must be greater than or equal to startedAt."
}

foreach ($booleanName in $requiredBooleans) {
    if (-not $report.PSObject.Properties.Name.Contains($booleanName)) {
        throw "VPS production smoke report is missing boolean field: $booleanName"
    }

    if ($report.$booleanName -isnot [bool]) {
        throw "VPS production smoke report field $booleanName must be boolean."
    }

    if ($RequireAllPassed -and -not $report.$booleanName) {
        throw "VPS production smoke report field $booleanName must be true when -RequireAllPassed is used."
    }
}

if ($null -eq $report.steps -or $report.steps.Count -eq 0) {
    throw "VPS production smoke report must contain steps array."
}

$stepIds = @($report.steps | ForEach-Object { [string]$_.id })
foreach ($step in $requiredSteps) {
    if ($stepIds -notcontains $step) {
        throw "VPS production smoke report is missing smoke step: $step"
    }
}

$duplicates = $stepIds | Group-Object | Where-Object { $_.Count -gt 1 }
if ($duplicates) {
    throw "VPS production smoke report contains duplicated smoke step: $($duplicates[0].Name)"
}

foreach ($entry in $report.steps) {
    $step = [string]$entry.id
    if ($requiredSteps -notcontains $step) {
        throw "VPS production smoke report contains unsupported smoke step: $step"
    }

    $status = [string]$entry.status
    if ($allowedStatuses -notcontains $status) {
        throw "VPS production smoke report step $step has unsupported status: $status"
    }

    if ($entry.PSObject.Properties.Name -notcontains "httpStatus" -or ($entry.httpStatus -isnot [int] -and $entry.httpStatus -isnot [long])) {
        throw "VPS production smoke report step $step must contain integer httpStatus."
    }

    if ([string]::IsNullOrWhiteSpace([string]$entry.evidence)) {
        throw "VPS production smoke report step $step must contain safe evidence."
    }

    if ($RequireAllPassed -and $status -ne "passed") {
        throw "VPS production smoke report step $step must be passed when -RequireAllPassed is used."
    }
}

$summary = [ordered]@{
    reportId = $report.reportId
    environmentName = $report.environmentName
    releaseId = $report.releaseId
    steps = $stepIds.Count
    passed = @($report.steps | Where-Object { $_.status -eq "passed" }).Count
    failed = @($report.steps | Where-Object { $_.status -eq "failed" }).Count
    blocked = @($report.steps | Where-Object { $_.status -eq "blocked" }).Count
    skipped = @($report.steps | Where-Object { $_.status -eq "skipped" }).Count
}

Write-Host "vps production smoke report valid $($summary | ConvertTo-Json -Compress)"
