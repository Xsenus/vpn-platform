param()

$ErrorActionPreference = "Stop"

function Resolve-RepoPath {
    param([string]$RelativePath)

    $root = Split-Path -Parent $PSScriptRoot
    return Join-Path $root $RelativePath
}

$bundleGeneratorPath = Resolve-RepoPath "scripts/new-production-evidence-bundle.ps1"
$manifestGeneratorPath = Resolve-RepoPath "scripts/new-production-evidence-manifest.ps1"
$tmpDirectory = Resolve-RepoPath "tmp"
New-Item -ItemType Directory -Force -Path $tmpDirectory | Out-Null

$bundleDirectory = Join-Path $tmpDirectory "production-evidence-manifest-unknown-release-id"
$manifestPath = Join-Path $bundleDirectory "production-evidence-manifest.json"

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
        -Operator "production-evidence-manifest-release-guard" | Out-Host

    $stagingReportPath = Join-Path $bundleDirectory "staging-smoke-report.json"
    $stagingReport = Get-Content -LiteralPath $stagingReportPath -Raw -Encoding UTF8 | ConvertFrom-Json
    $stagingReport.releaseId = "missing-release-id-for-regression"
    [System.IO.File]::WriteAllText(
        $stagingReportPath,
        ($stagingReport | ConvertTo-Json -Depth 10),
        [System.Text.UTF8Encoding]::new($false))

    $previousErrorActionPreference = $ErrorActionPreference
    $ErrorActionPreference = "Continue"
    $output = & powershell -NoProfile -ExecutionPolicy Bypass -File $manifestGeneratorPath `
        -BundleDirectory $bundleDirectory `
        -OutputPath $manifestPath 2>&1
    $manifestExitCode = $LASTEXITCODE
    $ErrorActionPreference = $previousErrorActionPreference

    if ($manifestExitCode -eq 0) {
        throw "Production evidence manifest generator accepted unknown releaseId."
    }

    if (Test-Path -LiteralPath $manifestPath) {
        throw "Production evidence manifest generator created manifest after unknown releaseId failure."
    }

    $text = [string]::Join("`n", @($output | ForEach-Object { [string]$_ }))
    if ($text.IndexOf("ReleaseId must exist in backend/src/VpnPlatform.Api/AppReleases/releases.json", [System.StringComparison]::OrdinalIgnoreCase) -lt 0) {
        throw "Production evidence manifest generator failed for an unexpected reason: $text"
    }

    Write-Output "production evidence manifest release guard valid"
}
finally {
    if (Test-Path -LiteralPath $bundleDirectory) {
        Remove-Item -LiteralPath $bundleDirectory -Recurse -Force
    }
}
