param()

$ErrorActionPreference = "Stop"

function Resolve-RepoPath {
    param([string]$RelativePath)

    $root = Split-Path -Parent $PSScriptRoot
    return Join-Path $root $RelativePath
}

$browserSmokePath = Resolve-RepoPath "scripts/admin-vps-browser-smoke.ps1"
$tmpDirectory = Resolve-RepoPath "tmp"
New-Item -ItemType Directory -Force -Path $tmpDirectory | Out-Null

$reportRelativePath = "tmp/admin-vps-browser-smoke-direct-unknown-release-id.json"
$reportPath = Join-Path $tmpDirectory "admin-vps-browser-smoke-direct-unknown-release-id.json"

try {
    if (Test-Path -LiteralPath $reportPath) {
        Remove-Item -LiteralPath $reportPath -Force
    }

    $previousPassword = $env:ADMIN_VPS_SMOKE_ADMIN_PASSWORD
    $env:ADMIN_VPS_SMOKE_ADMIN_PASSWORD = "release-guard-regression-password"

    $previousErrorActionPreference = $ErrorActionPreference
    $ErrorActionPreference = "Continue"
    $output = & powershell -NoProfile -ExecutionPolicy Bypass -File $browserSmokePath `
        -ApiBaseUrl "https://api.example.test" `
        -AdminWebUrl "https://admin.example.test" `
        -AdminEmail "admin@example.test" `
        -OutputPath $reportRelativePath `
        -EnvironmentName "staging" `
        -Operator "admin-vps-browser-smoke-direct-release-guard" `
        -ReleaseId "missing-release-id-for-regression" 2>&1
    $browserSmokeExitCode = $LASTEXITCODE
    $ErrorActionPreference = $previousErrorActionPreference

    if ($browserSmokeExitCode -eq 0) {
        throw "Browser smoke accepted unknown ReleaseId."
    }

    if (Test-Path -LiteralPath $reportPath) {
        throw "Browser smoke created report artifact after unknown ReleaseId failure."
    }

    $text = [string]::Join("`n", @($output | ForEach-Object { [string]$_ }))
    if ($text.IndexOf("ReleaseId must exist in backend/src/VpnPlatform.Api/AppReleases/releases.json", [System.StringComparison]::OrdinalIgnoreCase) -lt 0) {
        throw "Browser smoke failed for an unexpected reason: $text"
    }

    if ($text.IndexOf("release-guard-regression-password", [System.StringComparison]::OrdinalIgnoreCase) -ge 0) {
        throw "Browser smoke leaked password in regression output."
    }

    Write-Output "admin vps browser smoke direct release guard valid"
}
finally {
    if ($null -eq $previousPassword) {
        Remove-Item Env:\ADMIN_VPS_SMOKE_ADMIN_PASSWORD -ErrorAction SilentlyContinue
    } else {
        $env:ADMIN_VPS_SMOKE_ADMIN_PASSWORD = $previousPassword
    }

    if (Test-Path -LiteralPath $reportPath) {
        Remove-Item -LiteralPath $reportPath -Force
    }

    if ((Test-Path -LiteralPath $tmpDirectory) -and -not (Get-ChildItem -LiteralPath $tmpDirectory -Force)) {
        Remove-Item -LiteralPath $tmpDirectory -Force
    }
}
