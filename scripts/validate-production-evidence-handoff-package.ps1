param(
    [Parameter(Mandatory = $true)]
    [string]$PackageDirectory,

    [string]$ExpectedArchiveSha256 = "",
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

function Resolve-RequiredFile {
    param(
        [string]$PathValue,
        [string]$Description
    )

    if ([string]::IsNullOrWhiteSpace($PathValue) -or -not (Test-Path -LiteralPath $PathValue -PathType Leaf)) {
        throw "$Description was not found: $PathValue"
    }

    return (Resolve-Path -LiteralPath $PathValue).Path
}

function Assert-StringField {
    param(
        [object]$Object,
        [string]$PropertyName,
        [string]$Context
    )

    if (-not $Object.PSObject.Properties.Name.Contains($PropertyName)) {
        throw "$Context is missing required field: $PropertyName"
    }

    if ([string]::IsNullOrWhiteSpace([string]$Object.$PropertyName)) {
        throw "$Context field is empty: $PropertyName"
    }
}

function Get-FileSha256 {
    param([string]$PathValue)

    return (Get-FileHash -LiteralPath $PathValue -Algorithm SHA256).Hash.ToLowerInvariant()
}

function Read-JsonFile {
    param(
        [string]$PathValue,
        [string]$Description
    )

    $fullPath = Resolve-RequiredFile -PathValue $PathValue -Description $Description
    $raw = Get-Content -LiteralPath $fullPath -Raw -Encoding UTF8
    if ($raw.Contains([char]0xFFFD)) {
        throw "$Description contains invalid UTF-8 replacement character."
    }

    try {
        return [ordered]@{
            path = $fullPath
            json = $raw | ConvertFrom-Json
        }
    }
    catch {
        throw "$Description is invalid JSON: $($_.Exception.Message)"
    }
}

if ([string]::IsNullOrWhiteSpace($PackageDirectory) -or -not (Test-Path -LiteralPath $PackageDirectory -PathType Container)) {
    throw "Production evidence handoff package directory was not found: $PackageDirectory"
}

$packageFullPath = (Resolve-Path -LiteralPath $PackageDirectory).Path
$items = @(Get-ChildItem -LiteralPath $packageFullPath -Force)
$directories = @($items | Where-Object { $_.PSIsContainer })
if ($directories.Count -gt 0) {
    throw "Production evidence handoff package must not contain nested directories: $($directories[0].Name)"
}

$requiredFiles = @(
    "production-evidence.zip",
    "production-evidence-handoff-receipt.json",
    "production-evidence-handoff-receipt.md",
    "production-evidence-handoff-checklist.json",
    "production-evidence-handoff-checklist.md",
    "production-evidence-handoff-package-index.json",
    "production-evidence-handoff-package-index.md",
    "SHA256SUMS.txt"
)

foreach ($requiredFile in $requiredFiles) {
    if (-not (Test-Path -LiteralPath (Join-Path $packageFullPath $requiredFile) -PathType Leaf)) {
        throw "Production evidence handoff package is missing required file: $requiredFile"
    }
}

$allowed = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
foreach ($fileName in $requiredFiles) {
    [void]$allowed.Add($fileName)
}

foreach ($item in @($items | Where-Object { -not $_.PSIsContainer })) {
    if (-not $allowed.Contains($item.Name)) {
        throw "Production evidence handoff package contains unexpected file: $($item.Name)"
    }
}

$indexPath = Join-Path $packageFullPath "production-evidence-handoff-package-index.json"
$indexFile = Read-JsonFile -PathValue $indexPath -Description "Production evidence handoff package index"
$index = $indexFile.json

foreach ($fieldName in @("schemaVersion", "packageId", "generatedAt", "status", "releaseId", "archiveSha256", "manifestSha256", "productionReady", "requireProductionReady", "files")) {
    if (-not $index.PSObject.Properties.Name.Contains($fieldName)) {
        throw "Production evidence handoff package index is missing required field: $fieldName"
    }
}

if ([int]$index.schemaVersion -ne 1) {
    throw "Production evidence handoff package index schemaVersion is unsupported: $($index.schemaVersion)"
}

foreach ($fieldName in @("packageId", "status", "releaseId", "archiveSha256", "manifestSha256")) {
    Assert-StringField -Object $index -PropertyName $fieldName -Context "Production evidence handoff package index"
}

if (@("blocked", "production-ready-handoff") -notcontains [string]$index.status) {
    throw "Production evidence handoff package index status is invalid: $($index.status)"
}

if (-not ([string]$index.archiveSha256 -match "^[0-9a-f]{64}$")) {
    throw "Production evidence handoff package index archiveSha256 is invalid."
}

if (-not ([string]$index.manifestSha256 -match "^[0-9a-f]{64}$")) {
    throw "Production evidence handoff package index manifestSha256 is invalid."
}

if (-not [string]::IsNullOrWhiteSpace($ExpectedArchiveSha256) -and $ExpectedArchiveSha256.ToLowerInvariant() -ne [string]$index.archiveSha256) {
    throw "Production evidence handoff package archiveSha256 does not match expected archive hash."
}

$generatedAt = [DateTimeOffset]::MinValue
if (-not [DateTimeOffset]::TryParse([string]$index.generatedAt, [ref]$generatedAt)) {
    throw "Production evidence handoff package index generatedAt is not a valid DateTimeOffset."
}

if ($RequireProductionReady) {
    $latestReleaseId = Get-LatestActiveReleaseId
    if (-not [string]::Equals([string]$index.releaseId, $latestReleaseId, [System.StringComparison]::Ordinal)) {
        throw "Production evidence handoff package releaseId '$($index.releaseId)' must match latest active release '$latestReleaseId' when -RequireProductionReady is used."
    }
}

$indexFiles = @($index.files)
if ($indexFiles.Count -ne 5) {
    throw "Production evidence handoff package index must list exactly 5 artifact files."
}

foreach ($file in $indexFiles) {
    foreach ($fieldName in @("fileName", "lengthBytes", "sha256")) {
        Assert-StringField -Object $file -PropertyName $fieldName -Context "Production evidence handoff package index file"
    }

    if (-not $allowed.Contains([string]$file.fileName)) {
        throw "Production evidence handoff package index contains unexpected artifact file: $($file.fileName)"
    }

    $artifactPath = Resolve-RequiredFile -PathValue (Join-Path $packageFullPath ([string]$file.fileName)) -Description "Production evidence handoff package artifact"
    $artifact = Get-Item -LiteralPath $artifactPath
    if ([int64]$artifact.Length -ne [int64]$file.lengthBytes) {
        throw "Production evidence handoff package length mismatch: $($file.fileName)"
    }

    if ((Get-FileSha256 -PathValue $artifactPath) -ne [string]$file.sha256) {
        throw "Production evidence handoff package sha256 mismatch: $($file.fileName)"
    }
}

$archivePath = Join-Path $packageFullPath "production-evidence.zip"
if ((Get-FileSha256 -PathValue $archivePath) -ne [string]$index.archiveSha256) {
    throw "Production evidence handoff package archiveSha256 does not match archive file."
}

$shaSumsPath = Join-Path $packageFullPath "SHA256SUMS.txt"
$shaLines = @(Get-Content -LiteralPath $shaSumsPath -Encoding UTF8 | Where-Object { -not [string]::IsNullOrWhiteSpace($_) })
if ($shaLines.Count -ne 6) {
    throw "Production evidence handoff package SHA256SUMS.txt must contain exactly 6 lines."
}

foreach ($line in $shaLines) {
    if ($line -notmatch "^([0-9a-f]{64})  (.+)$") {
        throw "Production evidence handoff package SHA256SUMS.txt contains invalid line: $line"
    }

    $hash = $Matches[1]
    $fileName = $Matches[2]
    if (-not $allowed.Contains($fileName)) {
        throw "Production evidence handoff package SHA256SUMS.txt contains unexpected file: $fileName"
    }

    $filePath = Resolve-RequiredFile -PathValue (Join-Path $packageFullPath $fileName) -Description "Production evidence handoff package checksum target"
    if ((Get-FileSha256 -PathValue $filePath) -ne $hash) {
        throw "Production evidence handoff package SHA256SUMS mismatch: $fileName"
    }
}

$checklistPath = Join-Path $packageFullPath "production-evidence-handoff-checklist.json"
$checklistArgs = @{
    ChecklistPath = $checklistPath
    ReceiptPath = (Join-Path $packageFullPath "production-evidence-handoff-receipt.json")
    ArchivePath = $archivePath
    ExpectedArchiveSha256 = [string]$index.archiveSha256
    WriteJson = $true
}

if ($RequireProductionReady) {
    $checklistArgs.RequireProductionReady = $true
}

$checklistValidationJson = & (Resolve-RepoPath "scripts/validate-production-evidence-handoff-checklist.ps1") @checklistArgs
$checklistValidation = $checklistValidationJson | ConvertFrom-Json

if ([string]$checklistValidation.releaseId -ne [string]$index.releaseId) {
    throw "Production evidence handoff package releaseId does not match checklist."
}

if ([string]$checklistValidation.archiveSha256 -ne [string]$index.archiveSha256) {
    throw "Production evidence handoff package archiveSha256 does not match checklist."
}

if ([string]$checklistValidation.manifestSha256 -ne [string]$index.manifestSha256) {
    throw "Production evidence handoff package manifestSha256 does not match checklist."
}

if ($RequireProductionReady) {
    if (-not [bool]$index.productionReady) {
        throw "Production evidence handoff package is not production-ready."
    }

    if ([string]$index.status -ne "production-ready-handoff") {
        throw "Production evidence handoff package status must be production-ready-handoff."
    }
}

$markdownPath = Join-Path $packageFullPath "production-evidence-handoff-package-index.md"
$markdown = Get-Content -LiteralPath $markdownPath -Raw -Encoding UTF8
if ($markdown.Contains([char]0xFFFD)) {
    throw "Production evidence handoff package markdown contains invalid UTF-8 replacement character."
}

foreach ($requiredText in @([string]$index.releaseId, [string]$index.archiveSha256, [string]$index.manifestSha256, "Production evidence handoff package", "SHA256")) {
    if (-not $markdown.Contains($requiredText)) {
        throw "Production evidence handoff package markdown is missing: $requiredText"
    }
}

$result = [ordered]@{
    status = "valid"
    packageStatus = [string]$index.status
    packageDirectory = $packageFullPath
    releaseId = [string]$index.releaseId
    archiveSha256 = [string]$index.archiveSha256
    manifestSha256 = [string]$index.manifestSha256
    productionReady = [bool]$index.productionReady
    files = $requiredFiles
}

if ($WriteJson) {
    Write-Output ($result | ConvertTo-Json -Depth 8)
}
else {
    Write-Host "production evidence handoff package valid $($result | ConvertTo-Json -Depth 8 -Compress)"
}
