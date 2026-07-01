param(
    [Parameter(Mandatory = $true)]
    [string]$ArchivePath,

    [string]$ExpectedArchiveSha256 = "",
    [switch]$RequireProductionReady,
    [switch]$WriteJson
)

$ErrorActionPreference = "Stop"

Add-Type -AssemblyName System.IO.Compression

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

function Get-SafeEntryName {
    param([string]$EntryName)

    if ([string]::IsNullOrWhiteSpace($EntryName) -or [System.IO.Path]::IsPathRooted($EntryName)) {
        throw "Production evidence handoff package archive contains unsafe entry: $EntryName"
    }

    if ($EntryName.Contains("/") -or $EntryName.Contains("\") -or $EntryName -eq "." -or $EntryName -eq "..") {
        throw "Production evidence handoff package archive entry must be a file name only: $EntryName"
    }

    return $EntryName
}

function Copy-ZipEntry {
    param(
        [System.IO.Compression.ZipArchiveEntry]$Entry,
        [string]$DestinationPath
    )

    $destinationStream = [System.IO.File]::Open($DestinationPath, [System.IO.FileMode]::CreateNew)
    $entryStream = $null
    try {
        $entryStream = $Entry.Open()
        $entryStream.CopyTo($destinationStream)
    }
    finally {
        if ($entryStream -ne $null) {
            $entryStream.Dispose()
        }

        $destinationStream.Dispose()
    }
}

function Assert-PackageIndexLatestReleaseId {
    param(
        [string]$PackageDirectory,
        [string]$LatestReleaseId
    )

    $indexPath = Join-Path $PackageDirectory "production-evidence-handoff-package-index.json"
    try {
        $index = Get-Content -LiteralPath $indexPath -Raw -Encoding UTF8 | ConvertFrom-Json
    }
    catch {
        throw "Production evidence handoff package archive index JSON is invalid: $($_.Exception.Message)"
    }

    $releaseId = [string]$index.releaseId
    if ([string]::IsNullOrWhiteSpace($releaseId)) {
        throw "Production evidence handoff package archive releaseId is required."
    }

    if (-not [string]::Equals($releaseId, $LatestReleaseId, [System.StringComparison]::Ordinal)) {
        throw "Production evidence handoff package archive releaseId '$releaseId' must match latest active release '$LatestReleaseId' when -RequireProductionReady is used."
    }
}

if ([string]::IsNullOrWhiteSpace($ArchivePath) -or -not (Test-Path -LiteralPath $ArchivePath -PathType Leaf)) {
    throw "Production evidence handoff package archive was not found: $ArchivePath"
}

$archiveFullPath = (Resolve-Path -LiteralPath $ArchivePath).Path
$archiveSha256 = Get-FileSha256 -PathValue $archiveFullPath
if (-not [string]::IsNullOrWhiteSpace($ExpectedArchiveSha256) -and $ExpectedArchiveSha256.ToLowerInvariant() -ne $archiveSha256) {
    throw "Production evidence handoff package archive SHA256 does not match expected archive hash."
}

$requiredEntries = @(
    "production-evidence.zip",
    "production-evidence-handoff-receipt.json",
    "production-evidence-handoff-receipt.md",
    "production-evidence-handoff-checklist.json",
    "production-evidence-handoff-checklist.md",
    "production-evidence-handoff-package-index.json",
    "production-evidence-handoff-package-index.md",
    "SHA256SUMS.txt"
)

$allowed = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
foreach ($entryName in $requiredEntries) {
    [void]$allowed.Add($entryName)
}

$tempRoot = Join-Path ([System.IO.Path]::GetTempPath()) ("vpn-platform-handoff-package-archive-" + [Guid]::NewGuid().ToString("N"))
New-Item -ItemType Directory -Path $tempRoot -Force | Out-Null

$archiveStream = $null
$archive = $null
try {
    try {
        $archiveStream = [System.IO.File]::OpenRead($archiveFullPath)
        $archive = [System.IO.Compression.ZipArchive]::new($archiveStream, [System.IO.Compression.ZipArchiveMode]::Read)

        $seen = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
        foreach ($entry in $archive.Entries) {
            if ([string]::IsNullOrWhiteSpace($entry.Name) -or $entry.FullName -ne $entry.Name) {
                throw "Production evidence handoff package archive contains unexpected entry: $($entry.FullName)"
            }

            $entryName = Get-SafeEntryName -EntryName $entry.FullName
            if (-not $allowed.Contains($entryName)) {
                throw "Production evidence handoff package archive contains unexpected entry: $entryName"
            }

            if (-not $seen.Add($entryName)) {
                throw "Production evidence handoff package archive contains duplicated entry: $entryName"
            }

            Copy-ZipEntry -Entry $entry -DestinationPath (Join-Path $tempRoot $entryName)
        }

        foreach ($requiredEntry in $requiredEntries) {
            if (-not $seen.Contains($requiredEntry)) {
                throw "Production evidence handoff package archive is missing required entry: $requiredEntry"
            }
        }
    }
    finally {
        if ($archive -ne $null) {
            $archive.Dispose()
        }

        if ($archiveStream -ne $null) {
            $archiveStream.Dispose()
        }
    }

    if ($RequireProductionReady) {
        Assert-PackageIndexLatestReleaseId -PackageDirectory $tempRoot -LatestReleaseId (Get-LatestActiveReleaseId)
    }

    $validatorArgs = @{
        PackageDirectory = $tempRoot
        WriteJson = $true
    }

    if ($RequireProductionReady) {
        $validatorArgs.RequireProductionReady = $true
    }

    $packageValidationJson = & (Resolve-RepoPath "scripts/validate-production-evidence-handoff-package.ps1") @validatorArgs
    $packageValidation = $packageValidationJson | ConvertFrom-Json

    $result = [ordered]@{
        status = "valid"
        archivePath = $archiveFullPath
        archiveSha256 = $archiveSha256
        archiveBytes = [int64](Get-Item -LiteralPath $archiveFullPath).Length
        packageStatus = [string]$packageValidation.packageStatus
        releaseId = [string]$packageValidation.releaseId
        productionReady = [bool]$packageValidation.productionReady
        packageArchiveSourceSha256 = [string]$packageValidation.archiveSha256
        manifestSha256 = [string]$packageValidation.manifestSha256
        entries = $requiredEntries
    }

    if ($WriteJson) {
        Write-Output ($result | ConvertTo-Json -Depth 8)
    }
    else {
        Write-Host "production evidence handoff package archive valid $($result | ConvertTo-Json -Depth 8 -Compress)"
    }
}
finally {
    if (Test-Path -LiteralPath $tempRoot) {
        Remove-Item -LiteralPath $tempRoot -Recurse -Force
    }
}
