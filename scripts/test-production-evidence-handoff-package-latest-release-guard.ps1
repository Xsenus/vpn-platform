param()

$ErrorActionPreference = "Stop"

function Resolve-RepoPath {
    param([string]$RelativePath)

    $root = Split-Path -Parent $PSScriptRoot
    return Join-Path $root $RelativePath
}

$validatorPath = Resolve-RepoPath "scripts/validate-production-evidence-handoff-package.ps1"
$tmpRoot = Resolve-RepoPath "tmp"
$packageDirectory = Join-Path $tmpRoot "production-evidence-handoff-package-stale-release-guard"
$fakeHash = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa"

try {
    New-Item -ItemType Directory -Force -Path $packageDirectory | Out-Null

    foreach ($fileName in @(
        "production-evidence.zip",
        "production-evidence-handoff-receipt.json",
        "production-evidence-handoff-receipt.md",
        "production-evidence-handoff-checklist.json",
        "production-evidence-handoff-checklist.md",
        "production-evidence-handoff-package-index.md",
        "SHA256SUMS.txt")) {
        "sanitized placeholder" | Set-Content -LiteralPath (Join-Path $packageDirectory $fileName) -Encoding UTF8
    }

    $index = [ordered]@{
        schemaVersion = 1
        packageId = "production-evidence-handoff-package-stale-release-guard"
        generatedAt = "2026-07-01T17:00:00+07:00"
        status = "production-ready-handoff"
        releaseId = "stale-release-id"
        archiveSha256 = $fakeHash
        manifestSha256 = $fakeHash
        productionReady = $true
        requireProductionReady = $true
        files = @()
    }

    $index | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $packageDirectory "production-evidence-handoff-package-index.json") -Encoding UTF8

    $previousErrorActionPreference = $ErrorActionPreference
    $ErrorActionPreference = "Continue"
    $output = & powershell -NoProfile -ExecutionPolicy Bypass -File $validatorPath -PackageDirectory $packageDirectory -RequireProductionReady 2>&1
    $validatorExitCode = $LASTEXITCODE
    $ErrorActionPreference = $previousErrorActionPreference

    if ($validatorExitCode -eq 0) {
        throw "Validator accepted stale releaseId in -RequireProductionReady mode."
    }

    $text = [string]::Join("`n", @($output | ForEach-Object { [string]$_ }))
    if ($text.IndexOf("must match latest active release", [System.StringComparison]::OrdinalIgnoreCase) -lt 0) {
        throw "Validator failed for an unexpected reason: $text"
    }

    Write-Output "production evidence handoff package latest release guard valid"
}
finally {
    if (Test-Path -LiteralPath $packageDirectory) {
        Remove-Item -LiteralPath $packageDirectory -Recurse -Force
    }
}
