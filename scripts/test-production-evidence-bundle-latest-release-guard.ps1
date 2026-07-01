param()

$ErrorActionPreference = "Stop"

function Resolve-RepoPath {
    param([string]$RelativePath)

    $root = Split-Path -Parent $PSScriptRoot
    return Join-Path $root $RelativePath
}

$validatorPath = Resolve-RepoPath "scripts/validate-production-evidence-bundle.ps1"
$tmpDirectory = Resolve-RepoPath "tmp"
$bundleDirectory = Join-Path $tmpDirectory "production-evidence-bundle-stale-release-guard"

try {
    New-Item -ItemType Directory -Force -Path $bundleDirectory | Out-Null

    foreach ($fileName in @(
            "staging-smoke-report.json",
            "payment-provider-smoke-report.json",
            "admin-vps-smoke-report.json",
            "vpn-live-smoke-report.json"
        )) {
        [ordered]@{
            reportId = [System.IO.Path]::GetFileNameWithoutExtension($fileName)
            releaseId = "stale-release-id"
        } | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath (Join-Path $bundleDirectory $fileName) -Encoding UTF8
    }

    $previousErrorActionPreference = $ErrorActionPreference
    $ErrorActionPreference = "Continue"
    $output = & powershell -NoProfile -ExecutionPolicy Bypass -File $validatorPath -BundleDirectory $bundleDirectory -RequireProductionReady 2>&1
    $validatorExitCode = $LASTEXITCODE
    $ErrorActionPreference = $previousErrorActionPreference

    if ($validatorExitCode -eq 0) {
        throw "Validator accepted stale releaseId in -RequireProductionReady mode."
    }

    $text = [string]::Join("`n", @($output | ForEach-Object { [string]$_ }))
    if ($text.IndexOf("must match latest active release", [System.StringComparison]::OrdinalIgnoreCase) -lt 0) {
        throw "Validator failed for an unexpected reason: $text"
    }

    Write-Output "production evidence bundle latest release guard valid"
}
finally {
    if (Test-Path -LiteralPath $bundleDirectory) {
        Remove-Item -LiteralPath $bundleDirectory -Recurse -Force
    }
    if ((Test-Path -LiteralPath $tmpDirectory) -and -not (Get-ChildItem -LiteralPath $tmpDirectory -Force)) {
        Remove-Item -LiteralPath $tmpDirectory -Force
    }
}
