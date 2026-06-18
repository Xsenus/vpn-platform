param(
    [Parameter(Mandatory = $true)]
    [string]$PackageDirectory,

    [string]$OutputPath = "",
    [string]$ExpectedArchiveSha256 = "",
    [switch]$RequireProductionReady,
    [switch]$Force,
    [switch]$WriteJson
)

$ErrorActionPreference = "Stop"

Add-Type -AssemblyName System.IO.Compression

function Resolve-RepoPath {
    param([string]$RelativePath)

    $repoRoot = Split-Path -Parent $PSScriptRoot
    return Join-Path $repoRoot $RelativePath
}

function Get-FileSha256 {
    param([string]$PathValue)

    $sha256 = [System.Security.Cryptography.SHA256]::Create()
    $stream = $null
    try {
        $stream = [System.IO.File]::OpenRead($PathValue)
        $hash = $sha256.ComputeHash($stream)
        return -join ($hash | ForEach-Object { $_.ToString("x2") })
    }
    finally {
        if ($stream -ne $null) {
            $stream.Dispose()
        }

        $sha256.Dispose()
    }
}

function Get-TextSha256 {
    param([string]$Value)

    $sha256 = [System.Security.Cryptography.SHA256]::Create()
    try {
        $bytes = [System.Text.Encoding]::UTF8.GetBytes($Value)
        $hash = $sha256.ComputeHash($bytes)
        return -join ($hash | ForEach-Object { $_.ToString("x2") })
    }
    finally {
        $sha256.Dispose()
    }
}

function Get-DefaultArchiveName {
    param([string]$ReleaseId)

    $releaseHash = (Get-TextSha256 -Value $ReleaseId).Substring(0, 12)
    return "production-evidence-handoff-package-$releaseHash-$([DateTimeOffset]::UtcNow.ToString("yyyyMMdd-HHmmss")).zip"
}

function Get-SafeEntryName {
    param([string]$FileName)

    if ([string]::IsNullOrWhiteSpace($FileName) -or [System.IO.Path]::IsPathRooted($FileName)) {
        throw "Production evidence handoff package archive entry is unsafe: $FileName"
    }

    if ($FileName.Contains("/") -or $FileName.Contains("\") -or $FileName -eq "." -or $FileName -eq "..") {
        throw "Production evidence handoff package archive entry must be a file name only: $FileName"
    }

    return $FileName
}

function Add-ZipEntry {
    param(
        [System.IO.Compression.ZipArchive]$Archive,
        [string]$SourcePath,
        [string]$EntryName
    )

    $entry = $Archive.CreateEntry($EntryName, [System.IO.Compression.CompressionLevel]::Optimal)
    $entry.LastWriteTime = [DateTimeOffset](Get-Item -LiteralPath $SourcePath).LastWriteTimeUtc

    $entryStream = $entry.Open()
    $sourceStream = $null
    try {
        $sourceStream = [System.IO.File]::OpenRead($SourcePath)
        $sourceStream.CopyTo($entryStream)
    }
    finally {
        if ($sourceStream -ne $null) {
            $sourceStream.Dispose()
        }

        $entryStream.Dispose()
    }
}

if ([string]::IsNullOrWhiteSpace($PackageDirectory) -or -not (Test-Path -LiteralPath $PackageDirectory -PathType Container)) {
    throw "Production evidence handoff package directory was not found: $PackageDirectory"
}

$packageFullPath = (Resolve-Path -LiteralPath $PackageDirectory).Path
$validatorArgs = @{
    PackageDirectory = $packageFullPath
    WriteJson = $true
}

if (-not [string]::IsNullOrWhiteSpace($ExpectedArchiveSha256)) {
    $validatorArgs.ExpectedArchiveSha256 = $ExpectedArchiveSha256
}

if ($RequireProductionReady) {
    $validatorArgs.RequireProductionReady = $true
}

$packageValidationJson = & (Resolve-RepoPath "scripts/validate-production-evidence-handoff-package.ps1") @validatorArgs
$packageValidation = $packageValidationJson | ConvertFrom-Json

$archiveName = Get-DefaultArchiveName -ReleaseId ([string]$packageValidation.releaseId)
$archiveFullPath = if ([string]::IsNullOrWhiteSpace($OutputPath)) {
    Join-Path (Split-Path -Parent $packageFullPath) $archiveName
}
else {
    [System.IO.Path]::GetFullPath($OutputPath)
}

if ((Test-Path -LiteralPath $archiveFullPath) -and -not $Force) {
    throw "Production evidence handoff package archive already exists. Pass -Force to overwrite: $archiveFullPath"
}

$archiveParent = Split-Path -Parent $archiveFullPath
if (-not [string]::IsNullOrWhiteSpace($archiveParent) -and -not (Test-Path -LiteralPath $archiveParent)) {
    New-Item -ItemType Directory -Path $archiveParent -Force | Out-Null
}

if (Test-Path -LiteralPath $archiveFullPath) {
    Remove-Item -LiteralPath $archiveFullPath -Force
}

$entrySources = @()
foreach ($fileName in @($packageValidation.files | ForEach-Object { [string]$_ })) {
    $entryName = Get-SafeEntryName -FileName $fileName
    $sourcePath = Join-Path $packageFullPath $entryName
    if (-not (Test-Path -LiteralPath $sourcePath -PathType Leaf)) {
        throw "Production evidence handoff package archive source was not found: $entryName"
    }

    $entrySources += [pscustomobject]@{
        sourcePath = (Resolve-Path -LiteralPath $sourcePath).Path
        entryName = $entryName
    }
}

$duplicates = @($entrySources | Group-Object -Property entryName | Where-Object { $_.Count -gt 1 })
if ($duplicates.Count -gt 0) {
    throw "Production evidence handoff package archive contains duplicated entry: $($duplicates[0].Name)"
}

$archiveStream = [System.IO.File]::Open($archiveFullPath, [System.IO.FileMode]::CreateNew)
$archive = $null
try {
    $archive = [System.IO.Compression.ZipArchive]::new($archiveStream, [System.IO.Compression.ZipArchiveMode]::Create)
    foreach ($entrySource in $entrySources) {
        Add-ZipEntry -Archive $archive -SourcePath ([string]$entrySource.sourcePath) -EntryName ([string]$entrySource.entryName)
    }
}
finally {
    if ($archive -ne $null) {
        $archive.Dispose()
    }

    $archiveStream.Dispose()
}

$archiveItem = Get-Item -LiteralPath $archiveFullPath
$result = [ordered]@{
    status = "created"
    archivePath = $archiveItem.FullName
    archiveName = $archiveItem.Name
    archiveSha256 = Get-FileSha256 -PathValue $archiveItem.FullName
    archiveBytes = [int64]$archiveItem.Length
    packageDirectory = $packageFullPath
    packageStatus = [string]$packageValidation.packageStatus
    releaseId = [string]$packageValidation.releaseId
    productionReady = [bool]$packageValidation.productionReady
    packageArchiveSourceSha256 = [string]$packageValidation.archiveSha256
    manifestSha256 = [string]$packageValidation.manifestSha256
    entries = @($entrySources | ForEach-Object { [string]$_.entryName })
}

if ($WriteJson) {
    Write-Output ($result | ConvertTo-Json -Depth 8)
}
else {
    Write-Host "production evidence handoff package archive created $($result | ConvertTo-Json -Depth 8 -Compress)"
}
