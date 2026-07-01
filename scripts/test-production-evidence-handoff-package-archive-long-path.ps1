param(
    [string]$OutputDirectory = "",
    [switch]$Force,
    [switch]$WriteJson
)

$ErrorActionPreference = "Stop"

function Resolve-RepoPath {
    param([string]$RelativePath)

    $repoRoot = Split-Path -Parent $PSScriptRoot
    return Join-Path $repoRoot $RelativePath
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

$repoRoot = Split-Path -Parent $PSScriptRoot
$usingDefaultOutputDirectory = [string]::IsNullOrWhiteSpace($OutputDirectory)
if ($usingDefaultOutputDirectory) {
    $OutputDirectory = Join-Path (Resolve-RepoPath "tmp") "production-evidence-handoff-package-archive-long-release-id-path-regression-test"
}
$shouldCleanupGeneratedOutput = $usingDefaultOutputDirectory -and -not $WriteJson

$flowArgs = @{
    OutputDirectory = $OutputDirectory
    Force = $true
    WriteJson = $true
}

if ($Force) {
    $flowArgs.Force = $true
}

$flowJson = & (Resolve-RepoPath "scripts/test-production-evidence-handoff-package-archive-flow.ps1") @flowArgs
$flow = $flowJson | ConvertFrom-Json

if ([string]$flow.status -ne "passed") {
    throw "Production evidence handoff package archive long path regression expected flow status passed."
}

$archivePath = [string]$flow.handoffPackageArchivePath
if ([string]::IsNullOrWhiteSpace($archivePath) -or -not (Test-Path -LiteralPath $archivePath -PathType Leaf)) {
    throw "Production evidence handoff package archive long path regression archive was not found: $archivePath"
}

$archiveName = [System.IO.Path]::GetFileName($archivePath)
$releaseId = [string]$flow.releaseId
$expectedHash = (Get-TextSha256 -Value $releaseId).Substring(0, 12)

if ($archiveName.Contains($releaseId)) {
    throw "Production evidence handoff package archive long path regression archive name still contains full release id."
}

if ($archiveName -notlike "production-evidence-handoff-package-$expectedHash-*.zip") {
    throw "Production evidence handoff package archive long path regression archive name does not contain expected release hash: $archiveName"
}

if ($archiveName.Length -gt 72) {
    throw "Production evidence handoff package archive long path regression archive name is too long: $archiveName"
}

$resultJsonPath = [string]$flow.resultJsonPath
$result = Get-Content -LiteralPath $resultJsonPath -Raw -Encoding UTF8 | ConvertFrom-Json
if ([string]$result.releaseId -ne $releaseId) {
    throw "Production evidence handoff package archive long path regression result JSON lost full release id."
}

$validation = [ordered]@{
    status = "passed"
    releaseId = $releaseId
    archivePath = $archivePath
    archiveName = $archiveName
    archiveNameLength = $archiveName.Length
    releaseHash = $expectedHash
    resultJsonPath = $resultJsonPath
}

if ($WriteJson) {
    Write-Output ($validation | ConvertTo-Json -Depth 8)
}
else {
    Write-Host "production evidence handoff package archive long path regression passed $($validation | ConvertTo-Json -Depth 8 -Compress)"
}

if ($shouldCleanupGeneratedOutput -and (Test-Path -LiteralPath $OutputDirectory)) {
    Remove-Item -LiteralPath $OutputDirectory -Recurse -Force
    $tmpDirectory = Join-Path $repoRoot "tmp"
    if ((Test-Path -LiteralPath $tmpDirectory) -and -not (Get-ChildItem -LiteralPath $tmpDirectory -Force)) {
        Remove-Item -LiteralPath $tmpDirectory -Force
    }
}
