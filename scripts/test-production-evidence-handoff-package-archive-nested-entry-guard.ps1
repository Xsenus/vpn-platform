param()

$ErrorActionPreference = "Stop"

Add-Type -AssemblyName System.IO.Compression

function Resolve-RepoPath {
    param([string]$RelativePath)

    $root = Split-Path -Parent $PSScriptRoot
    return Join-Path $root $RelativePath
}

function Remove-EmptyDirectory {
    param([string]$DirectoryPath)

    if (-not (Test-Path -LiteralPath $DirectoryPath -PathType Container)) {
        return
    }

    $remaining = Get-ChildItem -LiteralPath $DirectoryPath -Force -ErrorAction SilentlyContinue | Select-Object -First 1
    if ($null -eq $remaining) {
        Remove-Item -LiteralPath $DirectoryPath -Force
    }
}

function Add-ZipTextEntry {
    param(
        [System.IO.Compression.ZipArchive]$Archive,
        [string]$EntryName,
        [string]$Content
    )

    $entry = $Archive.CreateEntry($EntryName, [System.IO.Compression.CompressionLevel]::Optimal)
    $writer = [System.IO.StreamWriter]::new($entry.Open(), [System.Text.UTF8Encoding]::new($false))
    try {
        $writer.Write($Content)
    }
    finally {
        $writer.Dispose()
    }
}

function New-NestedEntryArchive {
    param([string]$ArchivePath)

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

    $stream = [System.IO.File]::Open($ArchivePath, [System.IO.FileMode]::CreateNew)
    $archive = $null
    try {
        $archive = [System.IO.Compression.ZipArchive]::new($stream, [System.IO.Compression.ZipArchiveMode]::Create)
        foreach ($entryName in $requiredEntries) {
            Add-ZipTextEntry -Archive $archive -EntryName $entryName -Content "placeholder for $entryName"
        }

        Add-ZipTextEntry -Archive $archive -EntryName "nested/SHA256SUMS.txt" -Content "nested placeholder"
    }
    finally {
        if ($archive -ne $null) {
            $archive.Dispose()
        }

        $stream.Dispose()
    }
}

function Assert-FailsWith {
    param(
        [scriptblock]$Action,
        [string]$ExpectedMessage
    )

    try {
        & $Action | Out-Null
    }
    catch {
        $message = $_.Exception.Message
        if ($message -notlike "*$ExpectedMessage*") {
            throw "Expected failure containing '$ExpectedMessage', actual: $message"
        }

        return $message
    }

    throw "Expected command to fail with '$ExpectedMessage'."
}

$validatorPath = Resolve-RepoPath "scripts/validate-production-evidence-handoff-package-archive.ps1"
$tmpDirectory = Resolve-RepoPath "tmp"
$archivePath = Join-Path $tmpDirectory "production-evidence-handoff-package-archive-nested-entry-guard.zip"

try {
    New-Item -ItemType Directory -Force -Path $tmpDirectory | Out-Null
    if (Test-Path -LiteralPath $archivePath) {
        Remove-Item -LiteralPath $archivePath -Force
    }

    New-NestedEntryArchive -ArchivePath $archivePath

    [void](Assert-FailsWith -ExpectedMessage "unexpected entry" -Action {
            & $validatorPath -ArchivePath $archivePath -WriteJson
        })

    Write-Output "production evidence handoff package archive nested entry guard valid"
}
finally {
    if (Test-Path -LiteralPath $archivePath) {
        Remove-Item -LiteralPath $archivePath -Force
    }

    Remove-EmptyDirectory -DirectoryPath $tmpDirectory
}
