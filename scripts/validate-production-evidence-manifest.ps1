param(
    [Parameter(Mandatory = $true)]
    [string]$ManifestPath,

    [switch]$RequireAllFiles,
    [switch]$WriteJson
)

$ErrorActionPreference = "Stop"

if ((Get-Command ConvertFrom-Json).Parameters.ContainsKey("DateKind")) {
    $PSDefaultParameterValues["ConvertFrom-Json:DateKind"] = "String"
}

function Resolve-RequiredPath {
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

$manifestFullPath = Resolve-RequiredPath -PathValue $ManifestPath
$raw = Get-Content -LiteralPath $manifestFullPath -Raw -Encoding UTF8
if ($raw.Contains([char]0xFFFD)) {
    throw "Production evidence manifest contains invalid UTF-8 replacement character."
}

try {
    $manifest = $raw | ConvertFrom-Json
}
catch {
    throw "Production evidence manifest is invalid JSON: $($_.Exception.Message)"
}

foreach ($fieldName in @("schemaVersion", "manifestId", "releaseId", "generatedAt", "bundleDirectory", "totalFiles", "totalBytes", "files")) {
    if (-not $manifest.PSObject.Properties.Name.Contains($fieldName)) {
        throw "Production evidence manifest is missing required field: $fieldName"
    }
}

if ([int]$manifest.schemaVersion -ne 1) {
    throw "Production evidence manifest schemaVersion is unsupported: $($manifest.schemaVersion)"
}

foreach ($fieldName in @("manifestId", "releaseId", "bundleDirectory")) {
    Assert-StringField -Object $manifest -PropertyName $fieldName -Context "Production evidence manifest"
}

$generatedAt = [DateTimeOffset]::MinValue
if (-not [DateTimeOffset]::TryParse([string]$manifest.generatedAt, [ref]$generatedAt)) {
    throw "Production evidence manifest generatedAt is not a valid DateTimeOffset."
}

$bundleDirectory = [string]$manifest.bundleDirectory
if (-not (Test-Path -LiteralPath $bundleDirectory -PathType Container)) {
    throw "Production evidence manifest bundleDirectory was not found: $bundleDirectory"
}

$files = @($manifest.files)
if ($files.Count -eq 0) {
    throw "Production evidence manifest must contain files."
}

if ([int]$manifest.totalFiles -ne $files.Count) {
    throw "Production evidence manifest totalFiles does not match files count."
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
        throw "Production evidence manifest is missing file: $requiredName"
    }
}

$duplicates = $names | Group-Object | Where-Object { $_.Count -gt 1 }
if ($duplicates) {
    throw "Production evidence manifest contains duplicated file: $($duplicates[0].Name)"
}

$totalBytes = 0L
$verified = @()
foreach ($file in $files) {
    foreach ($fieldName in @("name", "relativePath", "sha256", "lengthBytes", "lastWriteTimeUtc")) {
        Assert-StringField -Object $file -PropertyName $fieldName -Context "Production evidence manifest file"
    }

    $name = [string]$file.name
    $relativePath = [string]$file.relativePath
    if ([System.IO.Path]::IsPathRooted($relativePath)) {
        throw "Production evidence manifest file $name relativePath must not be rooted."
    }

    if (-not ([string]$file.sha256 -match "^[0-9a-f]{64}$")) {
        throw "Production evidence manifest file $name has invalid sha256."
    }

    $filePath = Join-Path $bundleDirectory $relativePath
    if (-not (Test-Path -LiteralPath $filePath -PathType Leaf)) {
        throw "Production evidence manifest referenced file was not found: $filePath"
    }

    $item = Get-Item -LiteralPath $filePath
    $bundleRoot = (Resolve-Path -LiteralPath $bundleDirectory).Path.TrimEnd([System.IO.Path]::DirectorySeparatorChar, [System.IO.Path]::AltDirectorySeparatorChar)
    if (-not $item.FullName.StartsWith($bundleRoot + [System.IO.Path]::DirectorySeparatorChar, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "Production evidence manifest file $name relativePath must stay inside bundle directory."
    }

    $expectedLength = [int64]$file.lengthBytes
    if ($item.Length -ne $expectedLength) {
        throw "Production evidence manifest file $name length mismatch. Expected $expectedLength, actual $($item.Length)."
    }

    $actualHash = Get-FileSha256 -Path $item.FullName
    if ($actualHash -ne [string]$file.sha256) {
        throw "Production evidence manifest file $name sha256 mismatch."
    }

    $lastWrite = [DateTimeOffset]::MinValue
    if (-not [DateTimeOffset]::TryParse([string]$file.lastWriteTimeUtc, [ref]$lastWrite)) {
        throw "Production evidence manifest file $name lastWriteTimeUtc is not a valid DateTimeOffset."
    }

    $totalBytes += $item.Length
    $verified += [ordered]@{
        name = $name
        relativePath = $relativePath
        lengthBytes = $item.Length
        sha256 = $actualHash
    }
}

if ([int64]$manifest.totalBytes -ne $totalBytes) {
    throw "Production evidence manifest totalBytes does not match files sum."
}

$result = [ordered]@{
    status = "valid"
    manifestPath = $manifestFullPath
    releaseId = [string]$manifest.releaseId
    totalFiles = $files.Count
    totalBytes = $totalBytes
    verifiedFiles = $verified
}

if ($WriteJson) {
    Write-Output ($result | ConvertTo-Json -Depth 8)
}
else {
    Write-Host "production evidence manifest valid $($result | ConvertTo-Json -Depth 8 -Compress)"
}
