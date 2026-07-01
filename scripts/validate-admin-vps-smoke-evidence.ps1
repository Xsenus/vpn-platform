param(
    [Parameter(Mandatory = $true)]
    [string]$PreflightReportPath,

    [Parameter(Mandatory = $true)]
    [string]$SmokeReportPath,

    [string]$ExpectedPreflightReportSha256 = "",

    [string]$ExpectedSmokeReportSha256 = "",

    [string]$MaxEvidenceChainMinutes = "120"
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot

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

$maxEvidenceChainMinutesValue = Convert-MaxEvidenceChainMinutes -Value $MaxEvidenceChainMinutes

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

function Read-JsonFile {
    param([Parameter(Mandatory = $true)][string]$Path)

    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) {
        throw "Admin VPS smoke evidence file was not found: $Path"
    }

    try {
        return Get-Content -LiteralPath $Path -Raw -Encoding UTF8 | ConvertFrom-Json
    }
    catch {
        throw "Admin VPS smoke evidence file is not valid JSON: $Path. $($_.Exception.Message)"
    }
}

function Normalize-Url {
    param([AllowEmptyString()][string]$Value)
    return ([string]$Value).Trim().TrimEnd("/")
}

function Get-FileSha256 {
    param([Parameter(Mandatory = $true)][string]$Path)

    $sha256 = [System.Security.Cryptography.SHA256]::Create()
    try {
        $bytes = [System.IO.File]::ReadAllBytes($Path)
        $hash = $sha256.ComputeHash($bytes)
        return [System.BitConverter]::ToString($hash).Replace("-", "").ToLowerInvariant()
    }
    finally {
        $sha256.Dispose()
    }
}

function Assert-ExpectedSha256 {
    param(
        [Parameter(Mandatory = $true)][string]$Actual,
        [AllowEmptyString()][string]$Expected,
        [Parameter(Mandatory = $true)][string]$Name
    )

    if ([string]::IsNullOrWhiteSpace($Expected)) {
        return
    }

    $normalizedExpected = $Expected.Trim().ToLowerInvariant()
    if (-not ($normalizedExpected -match "^[0-9a-f]{64}$")) {
        throw "Admin VPS smoke evidence expected $Name must be a 64-character SHA256 hex string."
    }

    if (-not [string]::Equals($Actual, $normalizedExpected, [System.StringComparison]::Ordinal)) {
        throw "Admin VPS smoke evidence $Name does not match expected SHA256."
    }
}

function Assert-Equal {
    param(
        [AllowEmptyString()][string]$Actual,
        [AllowEmptyString()][string]$Expected,
        [Parameter(Mandatory = $true)][string]$Name
    )

    if (-not [string]::Equals($Actual, $Expected, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "Admin VPS smoke evidence mismatch for $Name. Preflight='$Expected', smoke='$Actual'."
    }
}

function Assert-ReportIdFormat {
    param(
        [Parameter(Mandatory = $true)][string]$Value,
        [Parameter(Mandatory = $true)][string]$Name,
        [Parameter(Mandatory = $true)][string]$Prefix
    )

    $timestampPattern = "^" + [regex]::Escape($Prefix) + "\d{8}-\d{6}$"
    if ($Value -notmatch $timestampPattern) {
        throw "Admin VPS smoke evidence $Name reportId must match $($Prefix)yyyyMMdd-HHmmss."
    }
}

function Assert-ReportIdTimestampMatches {
    param(
        [Parameter(Mandatory = $true)][string]$Value,
        [Parameter(Mandatory = $true)][string]$Name,
        [Parameter(Mandatory = $true)][string]$Prefix,
        [Parameter(Mandatory = $true)][DateTimeOffset]$ExpectedAt,
        [Parameter(Mandatory = $true)][string]$TimestampField
    )

    $actualTimestamp = $Value.Substring($Prefix.Length)
    $expectedTimestamp = $ExpectedAt.ToUniversalTime().ToString("yyyyMMdd-HHmmss")
    if (-not [string]::Equals($actualTimestamp, $expectedTimestamp, [System.StringComparison]::Ordinal)) {
        throw "Admin VPS smoke evidence $Name reportId timestamp must match $TimestampField."
    }
}

$preflightFullPath = Resolve-WorkspacePath $PreflightReportPath
$smokeFullPath = Resolve-WorkspacePath $SmokeReportPath

$preflightValidator = Join-Path $repoRoot "scripts/validate-admin-vps-smoke-preflight-report.ps1"
$smokeValidator = Join-Path $repoRoot "scripts/validate-admin-vps-smoke-report.ps1"

$preflight = Read-JsonFile -Path $preflightFullPath
$smoke = Read-JsonFile -Path $smokeFullPath

Assert-Equal -Actual (Normalize-Url $smoke.apiBaseUrl) -Expected (Normalize-Url $preflight.apiBaseUrl) -Name "apiBaseUrl"
Assert-Equal -Actual (Normalize-Url $smoke.adminWebUrl) -Expected (Normalize-Url $preflight.adminWebUrl) -Name "adminWebUrl"
Assert-Equal -Actual ([string]$smoke.adminEmail) -Expected ([string]$preflight.adminEmail) -Name "adminEmail"
Assert-Equal -Actual ([string]$smoke.environmentName) -Expected ([string]$preflight.environmentName) -Name "environmentName"
Assert-Equal -Actual ([string]$smoke.operator) -Expected ([string]$preflight.operator) -Name "operator"

$preflightSmokeReportPath = Resolve-WorkspacePath ([string]$preflight.smokeReportPath)
if (-not [string]::Equals($preflightSmokeReportPath, $smokeFullPath, [System.StringComparison]::OrdinalIgnoreCase)) {
    throw "Admin VPS smoke evidence mismatch for smokeReportPath. Preflight='$preflightSmokeReportPath', smoke='$smokeFullPath'."
}

$preflightReportPath = Resolve-WorkspacePath ([string]$preflight.preflightReportPath)
if (-not [string]::Equals($preflightReportPath, $preflightFullPath, [System.StringComparison]::OrdinalIgnoreCase)) {
    throw "Admin VPS smoke evidence mismatch for preflightReportPath. Preflight='$preflightReportPath', actual='$preflightFullPath'."
}

$preflightReleaseId = ([string]$preflight.releaseId).Trim()
$smokeReleaseId = ([string]$smoke.releaseId).Trim()
$releaseIdsDiffer = -not [string]::Equals($preflightReleaseId, $smokeReleaseId, [System.StringComparison]::Ordinal)

if ([string]::IsNullOrWhiteSpace($preflightReleaseId)) {
    throw "Admin VPS smoke evidence preflight releaseId is required."
}

if ([string]::IsNullOrWhiteSpace($smokeReleaseId)) {
    throw "Admin VPS smoke evidence smoke releaseId is required."
}

if ($releaseIdsDiffer) {
    throw "Admin VPS smoke evidence mismatch for releaseId. Preflight='$preflightReleaseId', smoke='$($smoke.releaseId)'."
}

$latestReleaseId = Get-LatestActiveReleaseId
if (-not [string]::Equals($smokeReleaseId, $latestReleaseId, [System.StringComparison]::Ordinal)) {
    throw "Admin VPS smoke evidence releaseId '$smokeReleaseId' must match latest active release '$latestReleaseId' when -RequireReady and -RequireAllPassed are used."
}

& $preflightValidator -ReportPath $preflightFullPath -RequireReady | Out-Host
& $smokeValidator -ReportPath $smokeFullPath -RequireAllPassed | Out-Host

$preflightReportId = ([string]$preflight.reportId).Trim()
$smokeReportId = ([string]$smoke.reportId).Trim()
if ([string]::IsNullOrWhiteSpace($preflightReportId)) {
    throw "Admin VPS smoke evidence preflight reportId is required."
}

if ([string]::IsNullOrWhiteSpace($smokeReportId)) {
    throw "Admin VPS smoke evidence smoke reportId is required."
}

if ([string]::Equals($preflightReportId, $smokeReportId, [System.StringComparison]::OrdinalIgnoreCase)) {
    throw "Admin VPS smoke evidence report ids must be unique. Preflight='$preflightReportId', smoke='$smokeReportId'."
}

if (-not $preflightReportId.StartsWith("admin-vps-smoke-preflight-", [System.StringComparison]::OrdinalIgnoreCase)) {
    throw "Admin VPS smoke evidence preflight reportId must start with admin-vps-smoke-preflight-."
}

if (-not $smokeReportId.StartsWith("admin-vps-smoke-", [System.StringComparison]::OrdinalIgnoreCase) `
    -or $smokeReportId.StartsWith("admin-vps-smoke-preflight-", [System.StringComparison]::OrdinalIgnoreCase)) {
    throw "Admin VPS smoke evidence smoke reportId must start with admin-vps-smoke- and must not use the preflight prefix."
}

Assert-ReportIdFormat -Value $preflightReportId -Name "preflight" -Prefix "admin-vps-smoke-preflight-"
Assert-ReportIdFormat -Value $smokeReportId -Name "smoke" -Prefix "admin-vps-smoke-"

$generatedAt = [DateTimeOffset]::MinValue
$startedAt = [DateTimeOffset]::MinValue
$completedAt = [DateTimeOffset]::MinValue
if (-not [DateTimeOffset]::TryParse([string]$preflight.generatedAt, [ref]$generatedAt)) {
    throw "Admin VPS smoke evidence preflight generatedAt is not a valid DateTimeOffset."
}

if (-not [DateTimeOffset]::TryParse([string]$smoke.startedAt, [ref]$startedAt)) {
    throw "Admin VPS smoke evidence smoke startedAt is not a valid DateTimeOffset."
}

if (-not [DateTimeOffset]::TryParse([string]$smoke.completedAt, [ref]$completedAt)) {
    throw "Admin VPS smoke evidence smoke completedAt is not a valid DateTimeOffset."
}

Assert-ReportIdTimestampMatches -Value $preflightReportId -Name "preflight" -Prefix "admin-vps-smoke-preflight-" -ExpectedAt $generatedAt -TimestampField "generatedAt"
Assert-ReportIdTimestampMatches -Value $smokeReportId -Name "smoke" -Prefix "admin-vps-smoke-" -ExpectedAt $startedAt -TimestampField "startedAt"

if ($generatedAt -gt $completedAt) {
    throw "Admin VPS smoke evidence preflight generatedAt must not be after smoke completedAt."
}

if ($startedAt -lt $generatedAt) {
    throw "Admin VPS smoke evidence smoke startedAt must not be before preflight generatedAt."
}

if ($completedAt -lt $startedAt) {
    throw "Admin VPS smoke evidence smoke completedAt must not be before smoke startedAt."
}

if (($completedAt - $generatedAt) -gt [TimeSpan]::FromMinutes($maxEvidenceChainMinutesValue)) {
    throw "Admin VPS smoke evidence chain duration exceeds MaxEvidenceChainMinutes ($maxEvidenceChainMinutesValue)."
}

$sectionsContractPath = Resolve-WorkspacePath "docs/admin-vps-smoke-sections.json"
$preflightReportSha256 = Get-FileSha256 $preflightFullPath
$smokeReportSha256 = Get-FileSha256 $smokeFullPath
$sectionStatuses = @($smoke.sections | ForEach-Object { ([string]$_.status).Trim().ToLowerInvariant() })
$passedSections = @($sectionStatuses | Where-Object { $_ -eq "passed" }).Count
$failedSections = @($sectionStatuses | Where-Object { $_ -eq "failed" }).Count
$blockedSections = @($sectionStatuses | Where-Object { $_ -eq "blocked" }).Count
$skippedSections = @($sectionStatuses | Where-Object { $_ -eq "skipped" }).Count

Assert-ExpectedSha256 -Actual $preflightReportSha256 -Expected $ExpectedPreflightReportSha256 -Name "preflightReportSha256"
Assert-ExpectedSha256 -Actual $smokeReportSha256 -Expected $ExpectedSmokeReportSha256 -Name "smokeReportSha256"

$summary = [ordered]@{
    environmentName = $smoke.environmentName
    releaseId = $smoke.releaseId
    apiBaseUrl = (Normalize-Url $smoke.apiBaseUrl)
    adminWebUrl = (Normalize-Url $smoke.adminWebUrl)
    adminEmail = $smoke.adminEmail
    operator = $smoke.operator
    accountBootstrapChecked = $smoke.accountBootstrapChecked
    adminLoginPassed = $smoke.adminLoginPassed
    noJsErrors = $smoke.noJsErrors
    noUnauthorizedAfterLogin = $smoke.noUnauthorizedAfterLogin
    preflightReportId = $preflightReportId
    smokeReportId = $smokeReportId
    preflightReportSha256 = $preflightReportSha256
    smokeReportSha256 = $smokeReportSha256
    preflightGeneratedAt = $preflight.generatedAt
    smokeStartedAt = $smoke.startedAt
    smokeCompletedAt = $smoke.completedAt
    preflightToSmokeSeconds = [int][Math]::Round(($startedAt - $generatedAt).TotalSeconds)
    smokeDurationSeconds = [int][Math]::Round(($completedAt - $startedAt).TotalSeconds)
    evidenceChainDurationSeconds = [int][Math]::Round(($completedAt - $generatedAt).TotalSeconds)
    evidenceChronology = "preflight|smoke"
    maxEvidenceChainMinutes = $maxEvidenceChainMinutesValue
    sections = @($smoke.sections).Count
    passed = $passedSections
    failed = $failedSections
    blocked = $blockedSections
    skipped = $skippedSections
    sectionsContractPath = $sectionsContractPath
    preflightReady = $preflight.readyForLiveSmoke
    preflightReportPath = $preflightFullPath
    smokeReportPath = $smokeFullPath
}

Write-Host "admin vps smoke evidence valid $($summary | ConvertTo-Json -Compress)"
