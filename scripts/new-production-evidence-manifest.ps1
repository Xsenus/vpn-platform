param(
    [Parameter(Mandatory = $true)]
    [string]$BundleDirectory,

    [string]$OutputPath = "",
    [switch]$RequireSummary,
    [switch]$Force
)

$ErrorActionPreference = "Stop"

function Resolve-RepoPath {
    param([string]$RelativePath)

    $repoRoot = Split-Path -Parent $PSScriptRoot
    return Join-Path $repoRoot $RelativePath
}

function Resolve-BundlePath {
    param([string]$DirectoryPath)

    if ([string]::IsNullOrWhiteSpace($DirectoryPath) -or -not (Test-Path -LiteralPath $DirectoryPath -PathType Container)) {
        throw "Production evidence bundle directory was not found: $DirectoryPath"
    }

    return (Resolve-Path -LiteralPath $DirectoryPath).Path
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

function Get-PortableRelativePath {
    param(
        [string]$BasePath,
        [string]$FullPath
    )

    $separator = [System.IO.Path]::DirectorySeparatorChar
    $baseWithSeparator = if ($BasePath.EndsWith([string]$separator, [System.StringComparison]::Ordinal)) {
        $BasePath
    } else {
        $BasePath + $separator
    }

    $baseUri = New-Object System.Uri($baseWithSeparator)
    $fileUri = New-Object System.Uri($FullPath)
    return [System.Uri]::UnescapeDataString($baseUri.MakeRelativeUri($fileUri).ToString()).Replace("/", [string]$separator)
}

function Read-ReleaseId {
    param(
        [string]$BundlePath,
        [string]$StagingReportPath
    )

    $summaryJsonPath = Join-Path $BundlePath "production-readiness-summary.json"
    if (Test-Path -LiteralPath $summaryJsonPath -PathType Leaf) {
        $summary = Get-Content -LiteralPath $summaryJsonPath -Raw -Encoding UTF8 | ConvertFrom-Json
        if (-not [string]::IsNullOrWhiteSpace([string]$summary.releaseId)) {
            return [string]$summary.releaseId
        }
    }

    $staging = Get-Content -LiteralPath $StagingReportPath -Raw -Encoding UTF8 | ConvertFrom-Json
    if (-not [string]::IsNullOrWhiteSpace([string]$staging.releaseId)) {
        return [string]$staging.releaseId
    }

    return "unknown-release"
}

$bundleFullPath = Resolve-BundlePath -DirectoryPath $BundleDirectory
$fullOutputPath = if ([string]::IsNullOrWhiteSpace($OutputPath)) {
    Join-Path $bundleFullPath "production-evidence-manifest.json"
} else {
    [System.IO.Path]::GetFullPath($OutputPath)
}

if ((Test-Path -LiteralPath $fullOutputPath) -and -not $Force) {
    throw "Output file already exists. Pass -Force to overwrite: $fullOutputPath"
}

$validatorParameters = @{
    BundleDirectory = $bundleFullPath
}
if ($RequireSummary) {
    $validatorParameters.RequireSummary = $true
}

& (Resolve-RepoPath "scripts/validate-production-evidence-bundle.ps1") @validatorParameters | Out-Host

$fileNames = @(
    "staging-smoke-report.json",
    "payment-provider-smoke-report.json",
    "admin-vps-smoke-report.json",
    "vpn-live-smoke-report.json"
)

foreach ($optionalFileName in @("production-readiness-summary.md", "production-readiness-summary.json")) {
    $optionalPath = Join-Path $bundleFullPath $optionalFileName
    if (Test-Path -LiteralPath $optionalPath -PathType Leaf) {
        $fileNames += $optionalFileName
    }
}

$files = @()
foreach ($fileName in $fileNames) {
    $path = Join-Path $bundleFullPath $fileName
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
        throw "Manifest source file was not found after bundle validation: $path"
    }

    $item = Get-Item -LiteralPath $path
    $files += [ordered]@{
        name = $fileName
        relativePath = Get-PortableRelativePath -BasePath $bundleFullPath -FullPath $item.FullName
        sha256 = Get-FileSha256 -Path $item.FullName
        lengthBytes = $item.Length
        lastWriteTimeUtc = $item.LastWriteTimeUtc.ToString("o")
    }
}

$releaseId = Read-ReleaseId -BundlePath $bundleFullPath -StagingReportPath (Join-Path $bundleFullPath "staging-smoke-report.json")
$totalBytes = 0L
foreach ($file in @($files)) {
    $totalBytes += [int64]$file.lengthBytes
}

$manifest = [ordered]@{
    schemaVersion = 1
    manifestId = "production-evidence-manifest-" + ([DateTimeOffset]::UtcNow.ToString("yyyyMMdd-HHmmss"))
    releaseId = $releaseId
    generatedAt = [DateTimeOffset]::UtcNow.ToString("o")
    bundleDirectory = $bundleFullPath
    requireSummary = [bool]$RequireSummary
    totalFiles = @($files).Count
    totalBytes = $totalBytes
    files = $files
}

$parent = Split-Path -Parent $fullOutputPath
if (-not [string]::IsNullOrWhiteSpace($parent) -and -not (Test-Path -LiteralPath $parent)) {
    New-Item -ItemType Directory -Path $parent | Out-Null
}

Set-Content -LiteralPath $fullOutputPath -Value ($manifest | ConvertTo-Json -Depth 8) -Encoding UTF8

Write-Output "production evidence manifest generated $fullOutputPath files=$($manifest.totalFiles) release=$releaseId"
