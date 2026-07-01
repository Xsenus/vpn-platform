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

function Update-ReceiptForArchive {
    param(
        [Parameter(Mandatory = $true)][string]$ReceiptPath,
        [Parameter(Mandatory = $true)][object]$ArchiveValidation
    )

    $receipt = Get-Content -LiteralPath $ReceiptPath -Raw -Encoding UTF8 | ConvertFrom-Json
    $receipt.releaseId = [string]$ArchiveValidation.releaseId
    $receipt.archiveSha256 = [string]$ArchiveValidation.archiveSha256
    $receipt.archiveBytes = [int64]$ArchiveValidation.archiveBytes
    $receipt.manifestSha256 = [string]$ArchiveValidation.manifestSha256
    $receipt.entries = @($ArchiveValidation.entries | ForEach-Object { [string]$_ })
    $receipt.verifiedFiles = @($ArchiveValidation.verifiedFiles)

    [System.IO.File]::WriteAllText(
        $ReceiptPath,
        ($receipt | ConvertTo-Json -Depth 10),
        [System.Text.UTF8Encoding]::new($false))

    $markdown = @(
        "# Production evidence handoff receipt",
        "",
        "- Status: ``ready-for-handoff``",
        "- Release: ``$($receipt.releaseId)``",
        "- Archive: ``$($receipt.archiveName)``",
        "- Archive SHA256: ``$($receipt.archiveSha256)``",
        "- Archive bytes: ``$($receipt.archiveBytes)``",
        "- Manifest SHA256: ``$($receipt.manifestSha256)``",
        "",
        "## Verified files",
        "",
        "| Name | Entry | Bytes | SHA256 |",
        "| --- | --- | ---: | --- |"
    )

    foreach ($file in @($receipt.verifiedFiles)) {
        $markdown += ('| ' + $file.name + ' | `' + $file.entryName + '` | ' + $file.lengthBytes + ' | `' + $file.sha256 + '` |')
    }

    [System.IO.File]::WriteAllText(
        [System.IO.Path]::ChangeExtension($ReceiptPath, ".md"),
        ($markdown -join [Environment]::NewLine),
        [System.Text.UTF8Encoding]::new($false))
}

$bundleGeneratorPath = Resolve-RepoPath "scripts/new-production-evidence-bundle.ps1"
$manifestGeneratorPath = Resolve-RepoPath "scripts/new-production-evidence-manifest.ps1"
$archiveGeneratorPath = Resolve-RepoPath "scripts/new-production-evidence-archive.ps1"
$receiptGeneratorPath = Resolve-RepoPath "scripts/new-production-evidence-handoff-receipt.ps1"
$archiveValidatorPath = Resolve-RepoPath "scripts/validate-production-evidence-archive.ps1"
$checklistGeneratorPath = Resolve-RepoPath "scripts/new-production-evidence-handoff-checklist.ps1"
$tmpDirectory = Resolve-RepoPath "tmp"
New-Item -ItemType Directory -Force -Path $tmpDirectory | Out-Null

$bundleDirectory = Join-Path $tmpDirectory "production-evidence-handoff-checklist-unknown-release-id"
$manifestPath = Join-Path $bundleDirectory "production-evidence-manifest.json"
$archivePath = Join-Path $bundleDirectory "production-evidence.zip"
$receiptPath = Join-Path $bundleDirectory "production-evidence-handoff-receipt.json"
$checklistPath = Join-Path $bundleDirectory "production-evidence-handoff-checklist.json"

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
        -Operator "production-evidence-handoff-checklist-release-guard" | Out-Host

    & powershell -NoProfile -ExecutionPolicy Bypass -File $manifestGeneratorPath `
        -BundleDirectory $bundleDirectory `
        -OutputPath $manifestPath | Out-Host

    & powershell -NoProfile -ExecutionPolicy Bypass -File $archiveGeneratorPath `
        -ManifestPath $manifestPath `
        -OutputPath $archivePath | Out-Host

    & powershell -NoProfile -ExecutionPolicy Bypass -File $receiptGeneratorPath `
        -ArchivePath $archivePath `
        -OutputPath $receiptPath | Out-Host

    Update-ArchiveManifestReleaseId `
        -ArchivePath $archivePath `
        -ReleaseId "missing-release-id-for-checklist-regression"

    $archiveValidation = (& powershell -NoProfile -ExecutionPolicy Bypass -File $archiveValidatorPath `
        -ArchivePath $archivePath `
        -WriteJson) | ConvertFrom-Json
    Update-ReceiptForArchive -ReceiptPath $receiptPath -ArchiveValidation $archiveValidation

    $previousErrorActionPreference = $ErrorActionPreference
    $ErrorActionPreference = "Continue"
    $output = & powershell -NoProfile -ExecutionPolicy Bypass -File $checklistGeneratorPath `
        -ReceiptPath $receiptPath `
        -ArchivePath $archivePath `
        -OutputPath $checklistPath 2>&1
    $checklistExitCode = $LASTEXITCODE
    $ErrorActionPreference = $previousErrorActionPreference

    if ($checklistExitCode -eq 0) {
        throw "Production evidence handoff checklist generator accepted unknown releaseId."
    }

    if (Test-Path -LiteralPath $checklistPath) {
        throw "Production evidence handoff checklist generator created checklist after unknown releaseId failure."
    }

    if (Test-Path -LiteralPath ([System.IO.Path]::ChangeExtension($checklistPath, ".md"))) {
        throw "Production evidence handoff checklist generator created markdown after unknown releaseId failure."
    }

    $text = [string]::Join("`n", @($output | ForEach-Object { [string]$_ }))
    if ($text.IndexOf("ReleaseId must exist in backend/src/VpnPlatform.Api/AppReleases/releases.json", [System.StringComparison]::OrdinalIgnoreCase) -lt 0) {
        throw "Production evidence handoff checklist generator failed for an unexpected reason: $text"
    }

    Write-Output "production evidence handoff checklist release guard valid"
}
finally {
    if (Test-Path -LiteralPath $bundleDirectory) {
        Remove-Item -LiteralPath $bundleDirectory -Recurse -Force
    }
    if ((Test-Path -LiteralPath $tmpDirectory) -and -not (Get-ChildItem -LiteralPath $tmpDirectory -Force)) {
        Remove-Item -LiteralPath $tmpDirectory -Force
    }
}
