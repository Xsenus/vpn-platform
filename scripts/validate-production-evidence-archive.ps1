param(
    [Parameter(Mandatory = $true)]
    [string]$ArchivePath,

    [string]$ExpectedArchiveSha256 = "",
    [switch]$RequireAllFiles,
    [switch]$WriteJson
)

$ErrorActionPreference = "Stop"

if ((Get-Command ConvertFrom-Json).Parameters.ContainsKey("DateKind")) {
    $PSDefaultParameterValues["ConvertFrom-Json:DateKind"] = "String"
}

Add-Type -AssemblyName System.IO.Compression

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

function Get-StreamSha256 {
    param([System.IO.Stream]$Stream)

    $sha256 = [System.Security.Cryptography.SHA256]::Create()
    try {
        $hash = $sha256.ComputeHash($Stream)
        return -join ($hash | ForEach-Object { $_.ToString("x2") })
    }
    finally {
        $sha256.Dispose()
    }
}

function Read-ZipEntryUtf8 {
    param([System.IO.Compression.ZipArchiveEntry]$Entry)

    $stream = $Entry.Open()
    $reader = $null
    try {
        $reader = [System.IO.StreamReader]::new($stream, [System.Text.UTF8Encoding]::new($false, $true))
        return $reader.ReadToEnd()
    }
    finally {
        if ($reader -ne $null) {
            $reader.Dispose()
        }
        else {
            $stream.Dispose()
        }
    }
}

function Get-SafeArchiveEntryName {
    param(
        [string]$RelativePath,
        [string]$DisplayName
    )

    if ([System.IO.Path]::IsPathRooted($RelativePath)) {
        throw "Production evidence archive manifest entry $DisplayName must not be rooted."
    }

    $parts = @($RelativePath -split "[\\/]+" | Where-Object { -not [string]::IsNullOrWhiteSpace($_) })
    if ($parts.Count -eq 0 -or ($parts | Where-Object { $_ -eq "." -or $_ -eq ".." }).Count -gt 0) {
        throw "Production evidence archive manifest entry $DisplayName has unsafe relativePath."
    }

    return ($parts -join "/")
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

$archiveFullPath = Resolve-RequiredFile -PathValue $ArchivePath -Description "Production evidence archive"
$archiveSha256 = Get-FileSha256 -Path $archiveFullPath
if (-not [string]::IsNullOrWhiteSpace($ExpectedArchiveSha256) -and $archiveSha256 -ne $ExpectedArchiveSha256.ToLowerInvariant()) {
    throw "Production evidence archive sha256 mismatch."
}

$archiveStream = [System.IO.File]::OpenRead($archiveFullPath)
$archive = $null
try {
    $archive = [System.IO.Compression.ZipArchive]::new($archiveStream, [System.IO.Compression.ZipArchiveMode]::Read)
    $entries = @($archive.Entries | Where-Object { -not [string]::IsNullOrWhiteSpace($_.FullName) })
    if ($entries.Count -eq 0) {
        throw "Production evidence archive must contain entries."
    }

    $duplicates = @($entries | Group-Object -Property FullName | Where-Object { $_.Count -gt 1 })
    if ($duplicates.Count -gt 0) {
        throw "Production evidence archive contains duplicated entry: $($duplicates[0].Name)"
    }

    $manifestEntry = $archive.GetEntry("production-evidence-manifest.json")
    if ($null -eq $manifestEntry) {
        throw "Production evidence archive is missing production-evidence-manifest.json."
    }

    $manifestText = Read-ZipEntryUtf8 -Entry $manifestEntry
    if ($manifestText.Contains([char]0xFFFD)) {
        throw "Production evidence archive manifest contains invalid UTF-8 replacement character."
    }

    try {
        $manifest = $manifestText | ConvertFrom-Json
    }
    catch {
        throw "Production evidence archive manifest is invalid JSON: $($_.Exception.Message)"
    }

    foreach ($fieldName in @("schemaVersion", "manifestId", "releaseId", "generatedAt", "totalFiles", "totalBytes", "files")) {
        if (-not $manifest.PSObject.Properties.Name.Contains($fieldName)) {
            throw "Production evidence archive manifest is missing required field: $fieldName"
        }
    }

    if ([int]$manifest.schemaVersion -ne 1) {
        throw "Production evidence archive manifest schemaVersion is unsupported: $($manifest.schemaVersion)"
    }

    foreach ($fieldName in @("manifestId", "releaseId")) {
        Assert-StringField -Object $manifest -PropertyName $fieldName -Context "Production evidence archive manifest"
    }

    $generatedAt = [DateTimeOffset]::MinValue
    if (-not [DateTimeOffset]::TryParse([string]$manifest.generatedAt, [ref]$generatedAt)) {
        throw "Production evidence archive manifest generatedAt is not a valid DateTimeOffset."
    }

    $files = @($manifest.files)
    if ($files.Count -eq 0) {
        throw "Production evidence archive manifest must contain files."
    }

    if ([int]$manifest.totalFiles -ne $files.Count) {
        throw "Production evidence archive manifest totalFiles does not match files count."
    }

    $requiredNames = @(
        "staging-smoke-report.json",
        "payment-provider-smoke-report.json",
        "admin-vps-smoke-report.json",
        "vpn-live-smoke-report.json"
    )
    if ($RequireAllFiles) {
        $requiredNames += @(
            "production-readiness-summary.md",
            "production-readiness-summary.json"
        )
    }

    $names = @($files | ForEach-Object { [string]$_.name })
    foreach ($requiredName in $requiredNames) {
        if ($names -notcontains $requiredName) {
            throw "Production evidence archive manifest is missing file: $requiredName"
        }
    }

    $expectedEntries = @("production-evidence-manifest.json")
    $totalBytes = 0L
    $verifiedFiles = @()
    foreach ($file in $files) {
        foreach ($fieldName in @("name", "relativePath", "sha256", "lengthBytes", "lastWriteTimeUtc")) {
            Assert-StringField -Object $file -PropertyName $fieldName -Context "Production evidence archive manifest file"
        }

        $name = [string]$file.name
        $entryName = Get-SafeArchiveEntryName -RelativePath ([string]$file.relativePath) -DisplayName $name
        $expectedEntries += $entryName

        $entry = $archive.GetEntry($entryName)
        if ($null -eq $entry) {
            throw "Production evidence archive is missing entry: $entryName"
        }

        $expectedLength = [int64]$file.lengthBytes
        if ($entry.Length -ne $expectedLength) {
            throw "Production evidence archive entry $entryName length mismatch. Expected $expectedLength, actual $($entry.Length)."
        }

        $entryStream = $entry.Open()
        try {
            $actualHash = Get-StreamSha256 -Stream $entryStream
        }
        finally {
            $entryStream.Dispose()
        }

        if ($actualHash -ne [string]$file.sha256) {
            throw "Production evidence archive entry $entryName sha256 mismatch."
        }

        $lastWrite = [DateTimeOffset]::MinValue
        if (-not [DateTimeOffset]::TryParse([string]$file.lastWriteTimeUtc, [ref]$lastWrite)) {
            throw "Production evidence archive manifest file $name lastWriteTimeUtc is not a valid DateTimeOffset."
        }

        $totalBytes += $entry.Length
        $verifiedFiles += [ordered]@{
            name = $name
            entryName = $entryName
            lengthBytes = $entry.Length
            sha256 = $actualHash
        }
    }

    if ([int64]$manifest.totalBytes -ne $totalBytes) {
        throw "Production evidence archive manifest totalBytes does not match entries sum."
    }

    $unexpectedEntries = @($entries | Where-Object { $expectedEntries -notcontains $_.FullName })
    if ($unexpectedEntries.Count -gt 0) {
        throw "Production evidence archive contains unexpected entry: $($unexpectedEntries[0].FullName)"
    }

    $manifestStream = $manifestEntry.Open()
    try {
        $manifestSha256 = Get-StreamSha256 -Stream $manifestStream
    }
    finally {
        $manifestStream.Dispose()
    }

    $result = [ordered]@{
        status = "valid"
        archivePath = $archiveFullPath
        archiveSha256 = $archiveSha256
        archiveBytes = (Get-Item -LiteralPath $archiveFullPath).Length
        releaseId = [string]$manifest.releaseId
        manifestSha256 = $manifestSha256
        entries = @($entries | ForEach-Object { $_.FullName })
        verifiedFiles = $verifiedFiles
    }

    if ($WriteJson) {
        Write-Output ($result | ConvertTo-Json -Depth 8)
    }
    else {
        Write-Host "production evidence archive valid $($result | ConvertTo-Json -Depth 8 -Compress)"
    }
}
finally {
    if ($archive -ne $null) {
        $archive.Dispose()
    }

    $archiveStream.Dispose()
}
