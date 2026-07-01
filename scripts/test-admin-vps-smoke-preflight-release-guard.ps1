param()

$ErrorActionPreference = "Stop"

function Resolve-RepoPath {
    param([string]$RelativePath)

    $root = Split-Path -Parent $PSScriptRoot
    return Join-Path $root $RelativePath
}

$preflightPath = Resolve-RepoPath "scripts/admin-vps-smoke-preflight.ps1"
$tmpDirectory = Resolve-RepoPath "tmp"
New-Item -ItemType Directory -Force -Path $tmpDirectory | Out-Null

$preflightReportRelativePath = "tmp/admin-vps-smoke-preflight-unknown-release-id.json"
$preflightReportPath = Join-Path $tmpDirectory "admin-vps-smoke-preflight-unknown-release-id.json"
$smokeReportRelativePath = "tmp/admin-vps-smoke-report-for-preflight-unknown-release-id.json"
$smokeReportPath = Join-Path $tmpDirectory "admin-vps-smoke-report-for-preflight-unknown-release-id.json"

try {
    foreach ($path in @($preflightReportPath, $smokeReportPath)) {
        if (Test-Path -LiteralPath $path) {
            Remove-Item -LiteralPath $path -Force
        }
    }

    $previousPassword = $env:ADMIN_VPS_SMOKE_ADMIN_PASSWORD
    $env:ADMIN_VPS_SMOKE_ADMIN_PASSWORD = "preflight-release-guard-regression-password"

    $previousErrorActionPreference = $ErrorActionPreference
    $ErrorActionPreference = "Continue"
    $output = & powershell -NoProfile -ExecutionPolicy Bypass -File $preflightPath `
        -ApiBaseUrl "https://api.example.test" `
        -AdminWebUrl "https://admin.example.test" `
        -AdminEmail "admin@example.test" `
        -SmokeReportPath $smokeReportRelativePath `
        -PreflightReportPath $preflightReportRelativePath `
        -EnvironmentName "staging" `
        -Operator "admin-vps-smoke-preflight-release-guard" `
        -ReleaseId "missing-release-id-for-regression" 2>&1
    $preflightExitCode = $LASTEXITCODE
    $ErrorActionPreference = $previousErrorActionPreference

    if ($preflightExitCode -eq 0) {
        throw "Preflight accepted unknown ReleaseId."
    }

    if (Test-Path -LiteralPath $preflightReportPath) {
        throw "Preflight created report artifact after unknown ReleaseId failure."
    }

    if (Test-Path -LiteralPath $smokeReportPath) {
        throw "Preflight created smoke report artifact after unknown ReleaseId failure."
    }

    $text = [string]::Join("`n", @($output | ForEach-Object { [string]$_ }))
    if ($text.IndexOf("ReleaseId must exist in backend/src/VpnPlatform.Api/AppReleases/releases.json", [System.StringComparison]::OrdinalIgnoreCase) -lt 0) {
        throw "Preflight failed for an unexpected reason: $text"
    }

    if ($text.IndexOf("preflight-release-guard-regression-password", [System.StringComparison]::OrdinalIgnoreCase) -ge 0) {
        throw "Preflight leaked password in regression output."
    }

    Write-Output "admin vps smoke preflight release guard valid"
}
finally {
    if ($null -eq $previousPassword) {
        Remove-Item Env:\ADMIN_VPS_SMOKE_ADMIN_PASSWORD -ErrorAction SilentlyContinue
    } else {
        $env:ADMIN_VPS_SMOKE_ADMIN_PASSWORD = $previousPassword
    }

    foreach ($path in @($preflightReportPath, $smokeReportPath)) {
        if (Test-Path -LiteralPath $path) {
            Remove-Item -LiteralPath $path -Force
        }
    }
}
