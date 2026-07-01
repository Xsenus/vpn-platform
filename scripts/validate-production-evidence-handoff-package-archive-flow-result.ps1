param(
    [Parameter(Mandatory = $true)]
    [string]$ResultJsonPath,

    [string]$ResultMarkdownPath = "",
    [switch]$RequireProductionReady,
    [switch]$WriteJson
)

$ErrorActionPreference = "Stop"

function Resolve-RepoPath {
    param([string]$RelativePath)

    $repoRoot = Split-Path -Parent $PSScriptRoot
    return Join-Path $repoRoot $RelativePath
}

function Get-LatestActiveReleaseId {
    $releasesPath = Resolve-RepoPath "backend/src/VpnPlatform.Api/AppReleases/releases.json"
    $releases = Get-Content -LiteralPath $releasesPath -Raw -Encoding UTF8 | ConvertFrom-Json
    $latest = @($releases | Where-Object { $_.isActive } | Sort-Object -Property { [DateTimeOffset]::Parse([string]$_.releasedAt) } -Descending | Select-Object -First 1)

    if ($latest.Count -eq 0 -or [string]::IsNullOrWhiteSpace([string]$latest[0].releaseId)) {
        throw "Latest active release was not found in AppReleases seed."
    }

    return [string]$latest[0].releaseId
}

function Get-FileSha256 {
    param([string]$PathValue)

    return (Get-FileHash -LiteralPath $PathValue -Algorithm SHA256).Hash.ToLowerInvariant()
}

function Assert-ExistingFile {
    param(
        [string]$PathValue,
        [string]$Label
    )

    if ([string]::IsNullOrWhiteSpace($PathValue) -or -not (Test-Path -LiteralPath $PathValue -PathType Leaf)) {
        throw "Production evidence handoff package archive flow result $Label was not found: $PathValue"
    }

    return (Resolve-Path -LiteralPath $PathValue).Path
}

function Assert-Equal {
    param(
        [string]$Actual,
        [string]$Expected,
        [string]$Message
    )

    if (-not [string]::Equals($Actual, $Expected, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw $Message
    }
}

function Assert-MarkdownContains {
    param(
        [string]$Markdown,
        [string]$Expected
    )

    if ($Markdown -notlike "*$Expected*") {
        throw "Production evidence handoff package archive flow result markdown is missing: $Expected"
    }
}

$resultJsonFullPath = Assert-ExistingFile -PathValue $ResultJsonPath -Label "JSON"
$result = Get-Content -LiteralPath $resultJsonFullPath -Raw -Encoding UTF8 | ConvertFrom-Json

if ([string]$result.status -ne "passed") {
    throw "Production evidence handoff package archive flow result status must be passed."
}

if ([string]$result.regressionStatus -ne "passed") {
    throw "Production evidence handoff package archive flow result regressionStatus must be passed."
}

if ($RequireProductionReady -and (-not [bool]$result.productionReady -or [string]$result.packageStatus -ne "production-ready-handoff")) {
    throw "Production evidence handoff package archive flow result is not production-ready."
}

if ($RequireProductionReady) {
    $latestReleaseId = Get-LatestActiveReleaseId
    if (-not [string]::Equals([string]$result.releaseId, $latestReleaseId, [System.StringComparison]::Ordinal)) {
        throw "Production evidence handoff package archive flow result releaseId '$($result.releaseId)' must match latest active release '$latestReleaseId' when -RequireProductionReady is used."
    }
}

if ([string]::IsNullOrWhiteSpace($ResultMarkdownPath)) {
    $ResultMarkdownPath = [string]$result.resultMarkdownPath
}

$resultMarkdownFullPath = Assert-ExistingFile -PathValue $ResultMarkdownPath -Label "Markdown"
$productionEvidenceArchivePath = Assert-ExistingFile -PathValue ([string]$result.productionEvidenceArchivePath) -Label "production evidence archive"
$handoffPackageArchivePath = Assert-ExistingFile -PathValue ([string]$result.handoffPackageArchivePath) -Label "handoff package archive"

Assert-Equal `
    -Actual (Get-FileSha256 -PathValue $productionEvidenceArchivePath) `
    -Expected ([string]$result.productionEvidenceArchiveSha256).ToLowerInvariant() `
    -Message "Production evidence handoff package archive flow result production evidence archive SHA256 does not match."

Assert-Equal `
    -Actual (Get-FileSha256 -PathValue $handoffPackageArchivePath) `
    -Expected ([string]$result.handoffPackageArchiveSha256).ToLowerInvariant() `
    -Message "Production evidence handoff package archive flow result handoff package archive SHA256 does not match."

$archiveValidatorArgs = @{
    ArchivePath = $handoffPackageArchivePath
    ExpectedArchiveSha256 = [string]$result.handoffPackageArchiveSha256
    WriteJson = $true
}

if ($RequireProductionReady) {
    $archiveValidatorArgs.RequireProductionReady = $true
}

$archiveValidationJson = & (Resolve-RepoPath "scripts/validate-production-evidence-handoff-package-archive.ps1") @archiveValidatorArgs
$archiveValidation = $archiveValidationJson | ConvertFrom-Json

Assert-Equal `
    -Actual ([string]$archiveValidation.releaseId) `
    -Expected ([string]$result.releaseId) `
    -Message "Production evidence handoff package archive flow result release id does not match archive validation."

Assert-Equal `
    -Actual ([string]$archiveValidation.packageStatus) `
    -Expected ([string]$result.packageStatus) `
    -Message "Production evidence handoff package archive flow result package status does not match archive validation."

$testedFailures = @($result.testedFailures)
foreach ($expectedFailure in @("wrong-expected-sha256", "unexpected-entry", "missing-required-entry")) {
    if (-not ($testedFailures | Where-Object { [string]$_.name -eq $expectedFailure })) {
        throw "Production evidence handoff package archive flow result is missing regression failure: $expectedFailure"
    }
}

if ($testedFailures.Count -lt 3) {
    throw "Production evidence handoff package archive flow result must include regression tested failures."
}

$markdown = Get-Content -LiteralPath $resultMarkdownFullPath -Raw -Encoding UTF8
foreach ($expected in @(
        "Production evidence handoff package archive flow",
        [string]$result.releaseId,
        [string]$result.packageStatus,
        [string]$result.productionEvidenceArchiveSha256,
        [string]$result.handoffPackageArchiveSha256,
        "Tested failures",
        "Artifacts"
    )) {
    Assert-MarkdownContains -Markdown $markdown -Expected $expected
}

$validation = [ordered]@{
    status = "valid"
    releaseId = [string]$result.releaseId
    packageStatus = [string]$result.packageStatus
    productionReady = [bool]$result.productionReady
    resultJsonPath = $resultJsonFullPath
    resultMarkdownPath = $resultMarkdownFullPath
    productionEvidenceArchiveSha256 = [string]$result.productionEvidenceArchiveSha256
    handoffPackageArchiveSha256 = [string]$result.handoffPackageArchiveSha256
    regressionStatus = [string]$result.regressionStatus
    testedFailuresCount = $testedFailures.Count
}

if ($WriteJson) {
    Write-Output ($validation | ConvertTo-Json -Depth 8)
}
else {
    Write-Host "production evidence handoff package archive flow result valid $($validation | ConvertTo-Json -Depth 8 -Compress)"
}
