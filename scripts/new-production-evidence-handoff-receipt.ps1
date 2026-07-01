param(
    [Parameter(Mandatory = $true)]
    [string]$ArchivePath,

    [string]$OutputPath = "",
    [string]$ExpectedArchiveSha256 = "",
    [switch]$RequireAllFiles,
    [switch]$Force,
    [switch]$WriteJson
)

$ErrorActionPreference = "Stop"

function Resolve-RepoPath {
    param([string]$RelativePath)

    $repoRoot = Split-Path -Parent $PSScriptRoot
    return Join-Path $repoRoot $RelativePath
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

function Assert-OutputAvailable {
    param([string]$Path)

    if ((Test-Path -LiteralPath $Path) -and -not $Force) {
        throw "Output file already exists. Pass -Force to overwrite: $Path"
    }
}

function Assert-KnownReleaseId {
    param([Parameter(Mandatory = $true)][string]$Value)

    $releasesPath = Resolve-RepoPath "backend/src/VpnPlatform.Api/AppReleases/releases.json"
    if (-not (Test-Path -LiteralPath $releasesPath -PathType Leaf)) {
        throw "ReleaseId must exist in backend/src/VpnPlatform.Api/AppReleases/releases.json."
    }

    $releases = Get-Content -LiteralPath $releasesPath -Raw -Encoding UTF8 | ConvertFrom-Json
    $matchedRelease = @($releases | Where-Object { [string]$_.releaseId -eq $Value } | Select-Object -First 1)
    if ($matchedRelease.Count -eq 0) {
        throw "ReleaseId must exist in backend/src/VpnPlatform.Api/AppReleases/releases.json."
    }
}

$archiveFullPath = Resolve-RequiredFile -PathValue $ArchivePath -Description "Production evidence archive"
$defaultOutputPath = Join-Path (Split-Path -Parent $archiveFullPath) "production-evidence-handoff-receipt.json"
$receiptJsonPath = if ([string]::IsNullOrWhiteSpace($OutputPath)) {
    $defaultOutputPath
}
else {
    [System.IO.Path]::GetFullPath($OutputPath)
}

if ([System.IO.Path]::GetExtension($receiptJsonPath) -ne ".json") {
    throw "Production evidence handoff receipt OutputPath must point to a .json file."
}

$receiptMarkdownPath = [System.IO.Path]::ChangeExtension($receiptJsonPath, ".md")
Assert-OutputAvailable -Path $receiptJsonPath
Assert-OutputAvailable -Path $receiptMarkdownPath

$validatorParameters = @{
    ArchivePath = $archiveFullPath
    WriteJson = $true
}
if ($RequireAllFiles) {
    $validatorParameters.RequireAllFiles = $true
}
if (-not [string]::IsNullOrWhiteSpace($ExpectedArchiveSha256)) {
    $validatorParameters.ExpectedArchiveSha256 = $ExpectedArchiveSha256
}

$validationJson = & (Resolve-RepoPath "scripts/validate-production-evidence-archive.ps1") @validatorParameters
$validation = $validationJson | ConvertFrom-Json
Assert-KnownReleaseId -Value ([string]$validation.releaseId)

$archiveItem = Get-Item -LiteralPath $archiveFullPath
$receipt = [ordered]@{
    schemaVersion = 1
    receiptId = "production-evidence-handoff-receipt-" + ([DateTimeOffset]::UtcNow.ToString("yyyyMMdd-HHmmss"))
    generatedAt = [DateTimeOffset]::UtcNow.ToString("o")
    status = "ready-for-handoff"
    releaseId = [string]$validation.releaseId
    archiveName = $archiveItem.Name
    archiveSha256 = [string]$validation.archiveSha256
    archiveBytes = [int64]$validation.archiveBytes
    manifestSha256 = [string]$validation.manifestSha256
    requireAllFiles = [bool]$RequireAllFiles
    entries = @($validation.entries | ForEach-Object { [string]$_ })
    verifiedFiles = @($validation.verifiedFiles | ForEach-Object {
        [ordered]@{
            name = [string]$_.name
            entryName = [string]$_.entryName
            lengthBytes = [int64]$_.lengthBytes
            sha256 = [string]$_.sha256
        }
    })
}

$parent = Split-Path -Parent $receiptJsonPath
if (-not [string]::IsNullOrWhiteSpace($parent) -and -not (Test-Path -LiteralPath $parent)) {
    New-Item -ItemType Directory -Path $parent | Out-Null
}

Set-Content -LiteralPath $receiptJsonPath -Value ($receipt | ConvertTo-Json -Depth 8) -Encoding UTF8

$markdown = @(
    "# Production evidence handoff receipt",
    "",
    ('- Status: `' + $receipt.status + '`'),
    ('- Release: `' + $receipt.releaseId + '`'),
    ('- Archive: `' + $receipt.archiveName + '`'),
    ('- Archive SHA256: `' + $receipt.archiveSha256 + '`'),
    ('- Archive bytes: `' + $receipt.archiveBytes + '`'),
    ('- Manifest SHA256: `' + $receipt.manifestSha256 + '`'),
    ('- Generated at: `' + $receipt.generatedAt + '`'),
    ('- Require all files: `' + $receipt.requireAllFiles + '`'),
    "",
    "## Entries",
    ""
)

foreach ($entry in @($receipt.entries)) {
    $markdown += ('- `' + $entry + '`')
}

$markdown += @(
    "",
    "## Verified files",
    "",
    "| Name | Entry | Bytes | SHA256 |",
    "| --- | --- | ---: | --- |"
)

foreach ($file in @($receipt.verifiedFiles)) {
    $markdown += ('| ' + $file.name + ' | `' + $file.entryName + '` | ' + $file.lengthBytes + ' | `' + $file.sha256 + '` |')
}

Set-Content -LiteralPath $receiptMarkdownPath -Value $markdown -Encoding UTF8

$result = [ordered]@{
    status = "created"
    receiptJsonPath = $receiptJsonPath
    receiptMarkdownPath = $receiptMarkdownPath
    releaseId = [string]$receipt.releaseId
    archiveName = [string]$receipt.archiveName
    archiveSha256 = [string]$receipt.archiveSha256
    manifestSha256 = [string]$receipt.manifestSha256
    entries = @($receipt.entries)
}

if ($WriteJson) {
    Write-Output ($result | ConvertTo-Json -Depth 8)
}
else {
    Write-Host "production evidence handoff receipt created $($result | ConvertTo-Json -Depth 8 -Compress)"
}
