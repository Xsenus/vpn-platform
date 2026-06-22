param(
    [Parameter(Mandatory = $true)]
    [string]$PreflightReportPath,

    [Parameter(Mandatory = $true)]
    [string]$SmokeReportPath,

    [string]$ExpectedPreflightReportSha256 = "",

    [string]$ExpectedSmokeReportSha256 = ""
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

$preflightFullPath = Resolve-WorkspacePath $PreflightReportPath
$smokeFullPath = Resolve-WorkspacePath $SmokeReportPath

$preflightValidator = Join-Path $repoRoot "scripts/validate-admin-vps-smoke-preflight-report.ps1"
$smokeValidator = Join-Path $repoRoot "scripts/validate-admin-vps-smoke-report.ps1"

& $preflightValidator -ReportPath $preflightFullPath -RequireReady | Out-Host
& $smokeValidator -ReportPath $smokeFullPath -RequireAllPassed | Out-Host

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

if ($generatedAt -gt $completedAt) {
    throw "Admin VPS smoke evidence preflight generatedAt must not be after smoke completedAt."
}

if ($startedAt -lt $generatedAt) {
    throw "Admin VPS smoke evidence smoke startedAt must not be before preflight generatedAt."
}

if ($completedAt -lt $startedAt) {
    throw "Admin VPS smoke evidence smoke completedAt must not be before smoke startedAt."
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
