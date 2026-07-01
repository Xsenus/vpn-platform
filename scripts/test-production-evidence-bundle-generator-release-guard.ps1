param()

$ErrorActionPreference = "Stop"

function Resolve-RepoPath {
    param([string]$RelativePath)

    $root = Split-Path -Parent $PSScriptRoot
    return Join-Path $root $RelativePath
}

$generatorPath = Resolve-RepoPath "scripts/new-production-evidence-bundle.ps1"
$tmpDirectory = Resolve-RepoPath "tmp"
New-Item -ItemType Directory -Force -Path $tmpDirectory | Out-Null

$bundleDirectory = Join-Path $tmpDirectory "production-evidence-bundle-unknown-release-id"

try {
    if (Test-Path -LiteralPath $bundleDirectory) {
        Remove-Item -LiteralPath $bundleDirectory -Recurse -Force
    }

    $previousErrorActionPreference = $ErrorActionPreference
    $ErrorActionPreference = "Continue"
    $output = & powershell -NoProfile -ExecutionPolicy Bypass -File $generatorPath `
        -OutputDirectory $bundleDirectory `
        -ApiBaseUrl "https://api.example.test" `
        -AdminWebUrl "https://admin.example.test" `
        -X3uiPanelUrl "https://x3ui.example.test" `
        -PublicWebUrl "https://public.example.test" `
        -CabinetWebUrl "https://cabinet.example.test" `
        -EnvironmentName "staging" `
        -Operator "production-evidence-bundle-generator-release-guard" `
        -ReleaseId "missing-release-id-for-regression" 2>&1
    $generatorExitCode = $LASTEXITCODE
    $ErrorActionPreference = $previousErrorActionPreference

    if ($generatorExitCode -eq 0) {
        throw "Production evidence bundle generator accepted unknown ReleaseId."
    }

    if (Test-Path -LiteralPath $bundleDirectory) {
        throw "Production evidence bundle generator created output directory after unknown ReleaseId failure."
    }

    $text = [string]::Join("`n", @($output | ForEach-Object { [string]$_ }))
    if ($text.IndexOf("ReleaseId must exist in backend/src/VpnPlatform.Api/AppReleases/releases.json", [System.StringComparison]::OrdinalIgnoreCase) -lt 0) {
        throw "Production evidence bundle generator failed for an unexpected reason: $text"
    }

    Write-Output "production evidence bundle generator release guard valid"
}
finally {
    if (Test-Path -LiteralPath $bundleDirectory) {
        Remove-Item -LiteralPath $bundleDirectory -Recurse -Force
    }
}
