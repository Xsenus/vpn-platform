param()

$ErrorActionPreference = "Stop"

function Resolve-RepoPath {
    param([string]$RelativePath)

    $root = Split-Path -Parent $PSScriptRoot
    return Join-Path $root $RelativePath
}

$generatorPath = Resolve-RepoPath "scripts/new-vpn-live-smoke-report.ps1"
$tmpDirectory = Resolve-RepoPath "tmp"
New-Item -ItemType Directory -Force -Path $tmpDirectory | Out-Null

$reportPath = Join-Path $tmpDirectory "vpn-live-smoke-generator-unknown-release-id.json"

try {
    if (Test-Path -LiteralPath $reportPath) {
        Remove-Item -LiteralPath $reportPath -Force
    }

    $previousErrorActionPreference = $ErrorActionPreference
    $ErrorActionPreference = "Continue"
    $output = & powershell -NoProfile -ExecutionPolicy Bypass -File $generatorPath `
        -OutputPath $reportPath `
        -ApiBaseUrl "https://api.example.test" `
        -AdminWebUrl "https://admin.example.test" `
        -X3uiPanelUrl "https://x3ui.example.test" `
        -EnvironmentName "staging" `
        -Operator "vpn-live-smoke-generator-release-guard" `
        -ReleaseId "missing-release-id-for-regression" 2>&1
    $generatorExitCode = $LASTEXITCODE
    $ErrorActionPreference = $previousErrorActionPreference

    if ($generatorExitCode -eq 0) {
        throw "Generator accepted unknown ReleaseId."
    }

    if (Test-Path -LiteralPath $reportPath) {
        throw "Generator created report artifact after unknown ReleaseId failure."
    }

    $text = [string]::Join("`n", @($output | ForEach-Object { [string]$_ }))
    if ($text.IndexOf("ReleaseId must exist in backend/src/VpnPlatform.Api/AppReleases/releases.json", [System.StringComparison]::OrdinalIgnoreCase) -lt 0) {
        throw "Generator failed for an unexpected reason: $text"
    }

    Write-Output "vpn live smoke generator release guard valid"
}
finally {
    if (Test-Path -LiteralPath $reportPath) {
        Remove-Item -LiteralPath $reportPath -Force
    }

    if ((Test-Path -LiteralPath $tmpDirectory) -and -not (Get-ChildItem -LiteralPath $tmpDirectory -Force)) {
        Remove-Item -LiteralPath $tmpDirectory -Force
    }
}
