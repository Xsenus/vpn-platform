param(
    [Parameter(Mandatory = $true)]
    [string]$ChecklistPath,

    [string]$ReceiptPath = "",
    [string]$ArchivePath = "",
    [string]$OutputDirectory = "",
    [string]$ExpectedArchiveSha256 = "",
    [switch]$RequireProductionReady,
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

function Get-FileSha256 {
    param([string]$PathValue)

    return (Get-FileHash -LiteralPath $PathValue -Algorithm SHA256).Hash.ToLowerInvariant()
}

function Copy-PackageFile {
    param(
        [string]$SourcePath,
        [string]$DestinationDirectory
    )

    $sourceFullPath = Resolve-RequiredFile -PathValue $SourcePath -Description "Production evidence handoff package source"
    $destinationPath = Join-Path $DestinationDirectory ([System.IO.Path]::GetFileName($sourceFullPath))
    Copy-Item -LiteralPath $sourceFullPath -Destination $destinationPath -Force

    $item = Get-Item -LiteralPath $destinationPath
    return [ordered]@{
        fileName = $item.Name
        sourcePath = $sourceFullPath
        packagePath = $item.FullName
        lengthBytes = [int64]$item.Length
        sha256 = Get-FileSha256 -PathValue $item.FullName
    }
}

$checklistFullPath = Resolve-RequiredFile -PathValue $ChecklistPath -Description "Production evidence handoff checklist"

$validatorArgs = @{
    ChecklistPath = $checklistFullPath
    WriteJson = $true
}

if (-not [string]::IsNullOrWhiteSpace($ReceiptPath)) {
    $validatorArgs.ReceiptPath = $ReceiptPath
}

if (-not [string]::IsNullOrWhiteSpace($ArchivePath)) {
    $validatorArgs.ArchivePath = $ArchivePath
}

if (-not [string]::IsNullOrWhiteSpace($ExpectedArchiveSha256)) {
    $validatorArgs.ExpectedArchiveSha256 = $ExpectedArchiveSha256
}

if ($RequireProductionReady) {
    $validatorArgs.RequireProductionReady = $true
}

$checklistValidationJson = & (Resolve-RepoPath "scripts/validate-production-evidence-handoff-checklist.ps1") @validatorArgs
$checklistValidation = $checklistValidationJson | ConvertFrom-Json

$checklistRaw = Get-Content -LiteralPath $checklistFullPath -Raw -Encoding UTF8
if ($checklistRaw.Contains([char]0xFFFD)) {
    throw "Production evidence handoff checklist contains invalid UTF-8 replacement character."
}

$checklist = $checklistRaw | ConvertFrom-Json
$packageDirectory = if ([string]::IsNullOrWhiteSpace($OutputDirectory)) {
    Join-Path (Split-Path -Parent $checklistFullPath) "production-evidence-handoff-package"
}
else {
    $OutputDirectory
}

if (Test-Path -LiteralPath $packageDirectory) {
    $existingItems = @(Get-ChildItem -LiteralPath $packageDirectory -Force)
    if ($existingItems.Count -gt 0 -and -not $Force) {
        throw "Output directory already contains files: $packageDirectory. Use -Force to overwrite package files."
    }

    $existingDirectories = @($existingItems | Where-Object { $_.PSIsContainer })
    if ($existingDirectories.Count -gt 0) {
        throw "Output directory contains nested directories and cannot be reused safely: $packageDirectory"
    }

    if ($Force) {
        foreach ($item in $existingItems) {
            Remove-Item -LiteralPath $item.FullName -Force
        }
    }
}
else {
    New-Item -ItemType Directory -Path $packageDirectory -Force | Out-Null
}

$packageDirectory = (Resolve-Path -LiteralPath $packageDirectory).Path
$archiveSourcePath = if ([string]::IsNullOrWhiteSpace($ArchivePath)) { [string]$checklist.archivePath } else { $ArchivePath }
$receiptSourcePath = if ([string]::IsNullOrWhiteSpace($ReceiptPath)) { [string]$checklist.receiptPath } else { $ReceiptPath }
$receiptMarkdownPath = [System.IO.Path]::ChangeExtension((Resolve-RequiredFile -PathValue $receiptSourcePath -Description "Production evidence handoff receipt"), ".md")
$checklistMarkdownPath = [System.IO.Path]::ChangeExtension($checklistFullPath, ".md")

$packageFiles = @(
    (Copy-PackageFile -SourcePath $archiveSourcePath -DestinationDirectory $packageDirectory),
    (Copy-PackageFile -SourcePath $receiptSourcePath -DestinationDirectory $packageDirectory),
    (Copy-PackageFile -SourcePath $receiptMarkdownPath -DestinationDirectory $packageDirectory),
    (Copy-PackageFile -SourcePath $checklistFullPath -DestinationDirectory $packageDirectory),
    (Copy-PackageFile -SourcePath $checklistMarkdownPath -DestinationDirectory $packageDirectory)
)

$index = [ordered]@{
    schemaVersion = 1
    packageId = "production-evidence-handoff-package-$([DateTimeOffset]::UtcNow.ToString("yyyyMMddHHmmss"))"
    generatedAt = [DateTimeOffset]::UtcNow.ToString("O")
    status = [string]$checklistValidation.checklistStatus
    releaseId = [string]$checklistValidation.releaseId
    archiveSha256 = [string]$checklistValidation.archiveSha256
    manifestSha256 = [string]$checklistValidation.manifestSha256
    productionReady = [bool]$checklistValidation.productionReady
    requireProductionReady = [bool]$RequireProductionReady
    files = $packageFiles
}

$indexPath = Join-Path $packageDirectory "production-evidence-handoff-package-index.json"
Set-Content -LiteralPath $indexPath -Value ($index | ConvertTo-Json -Depth 8) -Encoding UTF8

$shaSumsPath = Join-Path $packageDirectory "SHA256SUMS.txt"
$shaLines = @($packageFiles | ForEach-Object { "$($_.sha256)  $($_.fileName)" })
$shaLines += "$(Get-FileSha256 -PathValue $indexPath)  production-evidence-handoff-package-index.json"
Set-Content -LiteralPath $shaSumsPath -Value ($shaLines -join [Environment]::NewLine) -Encoding UTF8

$markdownPath = Join-Path $packageDirectory "production-evidence-handoff-package-index.md"
$markdown = @(
    "# Production evidence handoff package",
    "",
    "- Status: $($index.status)",
    "- Release: $($index.releaseId)",
    "- Archive SHA256: $($index.archiveSha256)",
    "- Manifest SHA256: $($index.manifestSha256)",
    "- Production ready: $($index.productionReady)",
    "",
    "## Files",
    "",
    "| File | SHA256 | Bytes |",
    "| --- | --- | --- |"
)

foreach ($file in $packageFiles) {
    $markdown += "| $($file.fileName) | $($file.sha256) | $($file.lengthBytes) |"
}

$markdown += @(
    "",
    "## Safety",
    "",
    "- Package contains only archive, receipt, checklist and hash indexes.",
    "- Do not add .env files, cookies, private headers, provider secrets or API keys."
)

Set-Content -LiteralPath $markdownPath -Value ($markdown -join [Environment]::NewLine) -Encoding UTF8

$result = [ordered]@{
    status = "created"
    packageStatus = [string]$index.status
    packageDirectory = $packageDirectory
    indexPath = $indexPath
    markdownPath = $markdownPath
    sha256SumsPath = $shaSumsPath
    releaseId = [string]$index.releaseId
    archiveSha256 = [string]$index.archiveSha256
    manifestSha256 = [string]$index.manifestSha256
    productionReady = [bool]$index.productionReady
    files = @($packageFiles | ForEach-Object { $_.fileName })
}

if ($WriteJson) {
    Write-Output ($result | ConvertTo-Json -Depth 8)
}
else {
    Write-Host "production evidence handoff package created $($result | ConvertTo-Json -Depth 8 -Compress)"
}
