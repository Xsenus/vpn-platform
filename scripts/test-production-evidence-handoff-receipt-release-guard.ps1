param()

$ErrorActionPreference = "Stop"

Add-Type -AssemblyName System.IO.Compression

function Resolve-RepoPath {
    param([string]$RelativePath)

    $root = Split-Path -Parent $PSScriptRoot
    return Join-Path $root $RelativePath
}

function Update-ArchiveManifestReleaseId {
    param(
        [Parameter(Mandatory = $true)][string]$ArchivePath,
        [Parameter(Mandatory = $true)][string]$ReleaseId
    )

    $archiveStream = [System.IO.File]::Open($ArchivePath, [System.IO.FileMode]::Open, [System.IO.FileAccess]::ReadWrite)
    $archive = $null
    try {
        $archive = [System.IO.Compression.ZipArchive]::new($archiveStream, [System.IO.Compression.ZipArchiveMode]::Update)
        $entry = $archive.GetEntry("production-evidence-manifest.json")
        if ($null -eq $entry) {
            throw "production-evidence-manifest.json was not found in archive."
        }

        $reader = [System.IO.StreamReader]::new($entry.Open(), [System.Text.UTF8Encoding]::new($false, $true))
        try {
            $manifest = $reader.ReadToEnd() | ConvertFrom-Json
        }
        finally {
            $reader.Dispose()
        }

        $entry.Delete()
        $manifest.releaseId = $ReleaseId
        $updatedEntry = $archive.CreateEntry("production-evidence-manifest.json", [System.IO.Compression.CompressionLevel]::Optimal)
        $writer = [System.IO.StreamWriter]::new($updatedEntry.Open(), [System.Text.UTF8Encoding]::new($false))
        try {
            $writer.Write(($manifest | ConvertTo-Json -Depth 10))
        }
        finally {
            $writer.Dispose()
        }
    }
    finally {
        if ($archive -ne $null) {
            $archive.Dispose()
        }

        $archiveStream.Dispose()
    }
}

$bundleGeneratorPath = Resolve-RepoPath "scripts/new-production-evidence-bundle.ps1"
$manifestGeneratorPath = Resolve-RepoPath "scripts/new-production-evidence-manifest.ps1"
$archiveGeneratorPath = Resolve-RepoPath "scripts/new-production-evidence-archive.ps1"
$receiptGeneratorPath = Resolve-RepoPath "scripts/new-production-evidence-handoff-receipt.ps1"
$tmpDirectory = Resolve-RepoPath "tmp"
New-Item -ItemType Directory -Force -Path $tmpDirectory | Out-Null

$bundleDirectory = Join-Path $tmpDirectory "production-evidence-handoff-receipt-unknown-release-id"
$manifestPath = Join-Path $bundleDirectory "production-evidence-manifest.json"
$archivePath = Join-Path $bundleDirectory "production-evidence.zip"
$receiptPath = Join-Path $bundleDirectory "production-evidence-handoff-receipt.json"

try {
    if (Test-Path -LiteralPath $bundleDirectory) {
        Remove-Item -LiteralPath $bundleDirectory -Recurse -Force
    }

    & powershell -NoProfile -ExecutionPolicy Bypass -File $bundleGeneratorPath `
        -OutputDirectory $bundleDirectory `
        -ApiBaseUrl "https://api.example.test" `
        -AdminWebUrl "https://admin.example.test" `
        -X3uiPanelUrl "https://x3ui.example.test" `
        -PublicWebUrl "https://public.example.test" `
        -CabinetWebUrl "https://cabinet.example.test" `
        -EnvironmentName "staging" `
        -Operator "production-evidence-handoff-receipt-release-guard" | Out-Host

    & powershell -NoProfile -ExecutionPolicy Bypass -File $manifestGeneratorPath `
        -BundleDirectory $bundleDirectory `
        -OutputPath $manifestPath | Out-Host

    & powershell -NoProfile -ExecutionPolicy Bypass -File $archiveGeneratorPath `
        -ManifestPath $manifestPath `
        -OutputPath $archivePath | Out-Host

    Update-ArchiveManifestReleaseId `
        -ArchivePath $archivePath `
        -ReleaseId "missing-release-id-for-receipt-regression"

    $previousErrorActionPreference = $ErrorActionPreference
    $ErrorActionPreference = "Continue"
    $output = & powershell -NoProfile -ExecutionPolicy Bypass -File $receiptGeneratorPath `
        -ArchivePath $archivePath `
        -OutputPath $receiptPath 2>&1
    $receiptExitCode = $LASTEXITCODE
    $ErrorActionPreference = $previousErrorActionPreference

    if ($receiptExitCode -eq 0) {
        throw "Production evidence handoff receipt generator accepted unknown releaseId."
    }

    if (Test-Path -LiteralPath $receiptPath) {
        throw "Production evidence handoff receipt generator created receipt after unknown releaseId failure."
    }

    if (Test-Path -LiteralPath ([System.IO.Path]::ChangeExtension($receiptPath, ".md"))) {
        throw "Production evidence handoff receipt generator created markdown after unknown releaseId failure."
    }

    $text = [string]::Join("`n", @($output | ForEach-Object { [string]$_ }))
    if ($text.IndexOf("ReleaseId must exist in backend/src/VpnPlatform.Api/AppReleases/releases.json", [System.StringComparison]::OrdinalIgnoreCase) -lt 0) {
        throw "Production evidence handoff receipt generator failed for an unexpected reason: $text"
    }

    Write-Output "production evidence handoff receipt release guard valid"
}
finally {
    if (Test-Path -LiteralPath $bundleDirectory) {
        Remove-Item -LiteralPath $bundleDirectory -Recurse -Force
    }
    if ((Test-Path -LiteralPath $tmpDirectory) -and -not (Get-ChildItem -LiteralPath $tmpDirectory -Force)) {
        Remove-Item -LiteralPath $tmpDirectory -Force
    }
}
