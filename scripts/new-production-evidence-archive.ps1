param(
    [Parameter(Mandatory = $true)]
    [string]$ManifestPath,

    [string]$OutputPath = "",
    [switch]$RequireAllFiles,
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

function Resolve-RequiredFile {
    param([string]$PathValue)

    if ([string]::IsNullOrWhiteSpace($PathValue) -or -not (Test-Path -LiteralPath $PathValue -PathType Leaf)) {
        throw "Production evidence manifest was not found: $PathValue"
    }

    return (Resolve-Path -LiteralPath $PathValue).Path
}

function Get-FileSha256 {
    param([string]$Path)

    $sha256 = [System.Security.Cryptography.SHA256]::Create()
    $stream = $null
    try {
        $stream = [System.IO.File]::OpenRead($Path)
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

function Get-SafeArchiveEntryName {
    param(
        [string]$RelativePath,
        [string]$DisplayName
    )

    if ([System.IO.Path]::IsPathRooted($RelativePath)) {
        throw "Production evidence archive entry $DisplayName must not be rooted."
    }

    $parts = @($RelativePath -split "[\\/]+" | Where-Object { -not [string]::IsNullOrWhiteSpace($_) })
    if ($parts.Count -eq 0 -or ($parts | Where-Object { $_ -eq "." -or $_ -eq ".." }).Count -gt 0) {
        throw "Production evidence archive entry $DisplayName has unsafe relativePath."
    }

    return ($parts -join "/")
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

$manifestFullPath = Resolve-RequiredFile -PathValue $ManifestPath

$validatorParameters = @{
    ManifestPath = $manifestFullPath
    WriteJson = $true
}
if ($RequireAllFiles) {
    $validatorParameters.RequireAllFiles = $true
}

$validationJson = & (Resolve-RepoPath "scripts/validate-production-evidence-manifest.ps1") @validatorParameters
$validation = $validationJson | ConvertFrom-Json

$manifest = Get-Content -LiteralPath $manifestFullPath -Raw -Encoding UTF8 | ConvertFrom-Json
$bundleDirectory = (Resolve-Path -LiteralPath ([string]$manifest.bundleDirectory)).Path
$bundleRoot = $bundleDirectory.TrimEnd([System.IO.Path]::DirectorySeparatorChar, [System.IO.Path]::AltDirectorySeparatorChar)

$archiveName = "production-evidence-$($validation.releaseId)-$([DateTimeOffset]::UtcNow.ToString("yyyyMMdd-HHmmss")).zip"
$archiveFullPath = if ([string]::IsNullOrWhiteSpace($OutputPath)) {
    Join-Path $bundleDirectory $archiveName
}
else {
    [System.IO.Path]::GetFullPath($OutputPath)
}

if ((Test-Path -LiteralPath $archiveFullPath) -and -not $Force) {
    throw "Production evidence archive already exists. Pass -Force to overwrite: $archiveFullPath"
}

$archiveParent = Split-Path -Parent $archiveFullPath
if (-not [string]::IsNullOrWhiteSpace($archiveParent) -and -not (Test-Path -LiteralPath $archiveParent)) {
    New-Item -ItemType Directory -Path $archiveParent | Out-Null
}

if (Test-Path -LiteralPath $archiveFullPath) {
    Remove-Item -LiteralPath $archiveFullPath -Force
}

$entrySources = @()
$entrySources += [pscustomobject]@{
    sourcePath = $manifestFullPath
    entryName = "production-evidence-manifest.json"
}

foreach ($file in @($manifest.files)) {
    $displayName = [string]$file.name
    $relativePath = [string]$file.relativePath
    $entryName = Get-SafeArchiveEntryName -RelativePath $relativePath -DisplayName $displayName
    $sourcePath = [System.IO.Path]::GetFullPath((Join-Path $bundleDirectory $relativePath))

    if (-not $sourcePath.StartsWith($bundleRoot + [System.IO.Path]::DirectorySeparatorChar, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "Production evidence archive entry $displayName must stay inside bundle directory."
    }

    $entrySources += [pscustomobject]@{
        sourcePath = $sourcePath
        entryName = $entryName
    }
}

$duplicates = @($entrySources | Group-Object -Property entryName | Where-Object { $_.Count -gt 1 })
if ($duplicates.Count -gt 0) {
    throw "Production evidence archive contains duplicated entry: $($duplicates[0].Name)"
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
    archiveSha256 = Get-FileSha256 -Path $archiveItem.FullName
    archiveBytes = $archiveItem.Length
    releaseId = [string]$validation.releaseId
    manifestPath = $manifestFullPath
    manifestSha256 = Get-FileSha256 -Path $manifestFullPath
    entries = @($entrySources | ForEach-Object { $_.entryName })
}

if ($WriteJson) {
    Write-Output ($result | ConvertTo-Json -Depth 8)
}
else {
    Write-Host "production evidence archive created $($result | ConvertTo-Json -Depth 8 -Compress)"
}
