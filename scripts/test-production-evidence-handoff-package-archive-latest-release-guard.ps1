param()

$ErrorActionPreference = "Stop"

function Resolve-RepoPath {
    param([string]$RelativePath)

    $root = Split-Path -Parent $PSScriptRoot
    return Join-Path $root $RelativePath
}

$validatorPath = Resolve-RepoPath "scripts/validate-production-evidence-handoff-package-archive.ps1"
$tmpDirectory = Resolve-RepoPath "tmp"
$packageDirectory = Join-Path $tmpDirectory "production-evidence-handoff-package-archive-stale-release-guard"
$archivePath = Join-Path $tmpDirectory "production-evidence-handoff-package-archive-stale-release-guard.zip"

try {
    New-Item -ItemType Directory -Force -Path $packageDirectory | Out-Null

    foreach ($fileName in @(
            "production-evidence-handoff-receipt.json",
            "production-evidence-handoff-checklist.json"
        )) {
        [ordered]@{
            releaseId = "stale-release-id"
        } | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath (Join-Path $packageDirectory $fileName) -Encoding UTF8
    }

    [ordered]@{
        releaseId = "stale-release-id"
        status = "production-ready-handoff"
    } | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath (Join-Path $packageDirectory "production-evidence-handoff-package-index.json") -Encoding UTF8

    foreach ($fileName in @(
            "production-evidence-handoff-receipt.md",
            "production-evidence-handoff-checklist.md",
            "production-evidence-handoff-package-index.md",
            "SHA256SUMS.txt"
        )) {
        "stale-release-id" | Set-Content -LiteralPath (Join-Path $packageDirectory $fileName) -Encoding UTF8
    }

    "placeholder archive" | Set-Content -LiteralPath (Join-Path $packageDirectory "production-evidence.zip") -Encoding UTF8

    Compress-Archive -Path (Join-Path $packageDirectory "*") -DestinationPath $archivePath -Force

    $previousErrorActionPreference = $ErrorActionPreference
    $ErrorActionPreference = "Continue"
    $output = & powershell -NoProfile -ExecutionPolicy Bypass -File $validatorPath -ArchivePath $archivePath -RequireProductionReady 2>&1
    $validatorExitCode = $LASTEXITCODE
    $ErrorActionPreference = $previousErrorActionPreference

    if ($validatorExitCode -eq 0) {
        throw "Validator accepted stale releaseId in -RequireProductionReady mode."
    }

    $text = [string]::Join("`n", @($output | ForEach-Object { [string]$_ }))
    if ($text.IndexOf("must match latest active release", [System.StringComparison]::OrdinalIgnoreCase) -lt 0) {
        throw "Validator failed for an unexpected reason: $text"
    }

    Write-Output "production evidence handoff package archive latest release guard valid"
}
finally {
    foreach ($path in @($archivePath, $packageDirectory)) {
        if (Test-Path -LiteralPath $path) {
            Remove-Item -LiteralPath $path -Recurse -Force
        }
    }
}
