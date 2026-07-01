param()

$ErrorActionPreference = "Stop"

function Resolve-RepoPath {
    param([string]$RelativePath)

    $root = Split-Path -Parent $PSScriptRoot
    return Join-Path $root $RelativePath
}

$bundleGeneratorPath = Resolve-RepoPath "scripts/new-production-evidence-bundle.ps1"
$manifestGeneratorPath = Resolve-RepoPath "scripts/new-production-evidence-manifest.ps1"
$archiveGeneratorPath = Resolve-RepoPath "scripts/new-production-evidence-archive.ps1"
$tmpDirectory = Resolve-RepoPath "tmp"
New-Item -ItemType Directory -Force -Path $tmpDirectory | Out-Null

$bundleDirectory = Join-Path $tmpDirectory "production-evidence-archive-unknown-release-id"
$manifestPath = Join-Path $bundleDirectory "production-evidence-manifest.json"
$archivePath = Join-Path $bundleDirectory "production-evidence-unknown-release.zip"

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
        -Operator "production-evidence-archive-release-guard" | Out-Host

    & powershell -NoProfile -ExecutionPolicy Bypass -File $manifestGeneratorPath `
        -BundleDirectory $bundleDirectory `
        -OutputPath $manifestPath | Out-Host

    $manifest = Get-Content -LiteralPath $manifestPath -Raw -Encoding UTF8 | ConvertFrom-Json
    $manifest.releaseId = "missing-release-id-for-archive-regression"
    [System.IO.File]::WriteAllText(
        $manifestPath,
        ($manifest | ConvertTo-Json -Depth 10),
        [System.Text.UTF8Encoding]::new($false))

    $previousErrorActionPreference = $ErrorActionPreference
    $ErrorActionPreference = "Continue"
    $output = & powershell -NoProfile -ExecutionPolicy Bypass -File $archiveGeneratorPath `
        -ManifestPath $manifestPath `
        -OutputPath $archivePath 2>&1
    $archiveExitCode = $LASTEXITCODE
    $ErrorActionPreference = $previousErrorActionPreference

    if ($archiveExitCode -eq 0) {
        throw "Production evidence archive generator accepted unknown releaseId."
    }

    if (Test-Path -LiteralPath $archivePath) {
        throw "Production evidence archive generator created archive after unknown releaseId failure."
    }

    $text = [string]::Join("`n", @($output | ForEach-Object { [string]$_ }))
    if ($text.IndexOf("ReleaseId must exist in backend/src/VpnPlatform.Api/AppReleases/releases.json", [System.StringComparison]::OrdinalIgnoreCase) -lt 0) {
        throw "Production evidence archive generator failed for an unexpected reason: $text"
    }

    Write-Output "production evidence archive release guard valid"
}
finally {
    if (Test-Path -LiteralPath $bundleDirectory) {
        Remove-Item -LiteralPath $bundleDirectory -Recurse -Force
    }
    if ((Test-Path -LiteralPath $tmpDirectory) -and -not (Get-ChildItem -LiteralPath $tmpDirectory -Force)) {
        Remove-Item -LiteralPath $tmpDirectory -Force
    }
}
