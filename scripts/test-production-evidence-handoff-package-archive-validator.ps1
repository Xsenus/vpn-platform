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

function Get-FileSha256 {
    param([string]$PathValue)

    return (Get-FileHash -LiteralPath $PathValue -Algorithm SHA256).Hash.ToLowerInvariant()
}

function Invoke-ArchiveValidator {
    param(
        [string]$PathValue,
        [string]$Sha256 = ""
    )

    $validatorArgs = @{
        ArchivePath = $PathValue
        WriteJson = $true
    }

    if (-not [string]::IsNullOrWhiteSpace($Sha256)) {
        $validatorArgs.ExpectedArchiveSha256 = $Sha256
    }

    if ($RequireProductionReady) {
        $validatorArgs.RequireProductionReady = $true
    }

    return & (Resolve-RepoPath "scripts/validate-production-evidence-handoff-package-archive.ps1") @validatorArgs
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

function Add-UnexpectedEntry {
    param([string]$PathValue)

    $stream = [System.IO.File]::Open($PathValue, [System.IO.FileMode]::Open, [System.IO.FileAccess]::ReadWrite)
    $archive = $null
    try {
        $archive = [System.IO.Compression.ZipArchive]::new($stream, [System.IO.Compression.ZipArchiveMode]::Update)
        $entry = $archive.CreateEntry("unexpected-entry.txt", [System.IO.Compression.CompressionLevel]::Optimal)
        $writer = [System.IO.StreamWriter]::new($entry.Open(), [System.Text.UTF8Encoding]::new($false))
        try {
            $writer.Write("tampered")
        }
        finally {
            $writer.Dispose()
        }
    }
    finally {
        if ($archive -ne $null) {
            $archive.Dispose()
        }

        $stream.Dispose()
    }
}

function New-ArchiveWithoutEntry {
    param(
        [string]$SourcePath,
        [string]$DestinationPath,
        [string]$EntryNameToSkip
    )

    $sourceStream = [System.IO.File]::OpenRead($SourcePath)
    $destinationStream = [System.IO.File]::Open($DestinationPath, [System.IO.FileMode]::CreateNew)
    $sourceArchive = $null
    $destinationArchive = $null
    try {
        $sourceArchive = [System.IO.Compression.ZipArchive]::new($sourceStream, [System.IO.Compression.ZipArchiveMode]::Read)
        $destinationArchive = [System.IO.Compression.ZipArchive]::new($destinationStream, [System.IO.Compression.ZipArchiveMode]::Create)

        foreach ($entry in $sourceArchive.Entries) {
            if ([string]::Equals($entry.FullName, $EntryNameToSkip, [System.StringComparison]::OrdinalIgnoreCase)) {
                continue
            }

            $newEntry = $destinationArchive.CreateEntry($entry.FullName, [System.IO.Compression.CompressionLevel]::Optimal)
            $newEntry.LastWriteTime = $entry.LastWriteTime

            $entryStream = $entry.Open()
            $newEntryStream = $newEntry.Open()
            try {
                $entryStream.CopyTo($newEntryStream)
            }
            finally {
                $newEntryStream.Dispose()
                $entryStream.Dispose()
            }
        }
    }
    finally {
        if ($destinationArchive -ne $null) {
            $destinationArchive.Dispose()
        }

        if ($sourceArchive -ne $null) {
            $sourceArchive.Dispose()
        }

        $destinationStream.Dispose()
        $sourceStream.Dispose()
    }
}

if ([string]::IsNullOrWhiteSpace($ArchivePath) -or -not (Test-Path -LiteralPath $ArchivePath -PathType Leaf)) {
    throw "Production evidence handoff package archive was not found: $ArchivePath"
}

$archiveFullPath = (Resolve-Path -LiteralPath $ArchivePath).Path
$archiveSha256 = Get-FileSha256 -PathValue $archiveFullPath
if (-not [string]::IsNullOrWhiteSpace($ExpectedArchiveSha256) -and $ExpectedArchiveSha256.ToLowerInvariant() -ne $archiveSha256) {
    throw "Production evidence handoff package archive regression test expected hash does not match input archive."
}

$tempRoot = Join-Path ([System.IO.Path]::GetTempPath()) ("vpn-platform-handoff-archive-validator-test-" + [Guid]::NewGuid().ToString("N"))
New-Item -ItemType Directory -Path $tempRoot -Force | Out-Null

try {
    $validJson = Invoke-ArchiveValidator -PathValue $archiveFullPath -Sha256 $archiveSha256
    $valid = $validJson | ConvertFrom-Json

    $wrongHash = "0" * 64
    if ($wrongHash -eq $archiveSha256) {
        $wrongHash = "1" * 64
    }

    $wrongHashMessage = Assert-FailsWith -ExpectedMessage "does not match expected archive hash" -Action {
        Invoke-ArchiveValidator -PathValue $archiveFullPath -Sha256 $wrongHash
    }

    $unexpectedEntryArchive = Join-Path $tempRoot "unexpected-entry.zip"
    Copy-Item -LiteralPath $archiveFullPath -Destination $unexpectedEntryArchive -Force
    Add-UnexpectedEntry -PathValue $unexpectedEntryArchive
    $unexpectedEntryMessage = Assert-FailsWith -ExpectedMessage "unexpected entry" -Action {
        Invoke-ArchiveValidator -PathValue $unexpectedEntryArchive
    }

    $missingEntryArchive = Join-Path $tempRoot "missing-sha256sums.zip"
    New-ArchiveWithoutEntry -SourcePath $archiveFullPath -DestinationPath $missingEntryArchive -EntryNameToSkip "SHA256SUMS.txt"
    $missingEntryMessage = Assert-FailsWith -ExpectedMessage "missing required entry" -Action {
        Invoke-ArchiveValidator -PathValue $missingEntryArchive
    }

    $result = [ordered]@{
        status = "passed"
        archivePath = $archiveFullPath
        archiveSha256 = $archiveSha256
        releaseId = [string]$valid.releaseId
        packageStatus = [string]$valid.packageStatus
        testedFailures = @(
            [ordered]@{ name = "wrong-expected-sha256"; message = $wrongHashMessage },
            [ordered]@{ name = "unexpected-entry"; message = $unexpectedEntryMessage },
            [ordered]@{ name = "missing-required-entry"; message = $missingEntryMessage }
        )
    }

    if ($WriteJson) {
        Write-Output ($result | ConvertTo-Json -Depth 8)
    }
    else {
        Write-Host "production evidence handoff package archive validator regression passed $($result | ConvertTo-Json -Depth 8 -Compress)"
    }
}
finally {
    if (Test-Path -LiteralPath $tempRoot) {
        Remove-Item -LiteralPath $tempRoot -Recurse -Force
    }
}
