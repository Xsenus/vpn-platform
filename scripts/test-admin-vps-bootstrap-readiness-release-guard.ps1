param()

$ErrorActionPreference = "Stop"

function Resolve-RepoPath {
    param([string]$RelativePath)

    $root = Split-Path -Parent $PSScriptRoot
    return Join-Path $root $RelativePath
}

$readinessPath = Resolve-RepoPath "scripts/admin-vps-bootstrap-smoke-readiness.ps1"
$tmpDirectory = Resolve-RepoPath "tmp"
New-Item -ItemType Directory -Force -Path $tmpDirectory | Out-Null

$readinessReportRelativePath = "tmp/admin-vps-bootstrap-readiness-unknown-release-id.json"
$readinessReportPath = Join-Path $tmpDirectory "admin-vps-bootstrap-readiness-unknown-release-id.json"
$bootstrapSmokeReportPath = Join-Path $tmpDirectory "admin-vps-bootstrap-smoke-for-readiness-unknown-release-id.json"

try {
    foreach ($path in @($readinessReportPath, $bootstrapSmokeReportPath)) {
        if (Test-Path -LiteralPath $path) {
            Remove-Item -LiteralPath $path -Force
        }
    }

    $passwordEnvName = "ADMIN_VPS_BOOTSTRAP_READINESS_RELEASE_GUARD_PASSWORD"
    $previousPassword = [Environment]::GetEnvironmentVariable($passwordEnvName, "Process")
    [Environment]::SetEnvironmentVariable($passwordEnvName, "bootstrap-readiness-release-guard-password", "Process")

    $previousErrorActionPreference = $ErrorActionPreference
    $ErrorActionPreference = "Continue"
    $output = & powershell -NoProfile -ExecutionPolicy Bypass -File $readinessPath `
        -ApiBaseUrl "https://api.example.test" `
        -AdminWebUrl "https://admin.example.test" `
        -AdminEmail "admin@example.test" `
        -AdminPasswordEnvName $passwordEnvName `
        -Provider "Sqlite" `
        -SmokeReportPath "tmp/admin-vps-smoke-for-readiness-unknown-release-id.json" `
        -PreflightReportPath "tmp/admin-vps-smoke-preflight-for-readiness-unknown-release-id.json" `
        -BootstrapSmokeReportPath "tmp/admin-vps-bootstrap-smoke-for-readiness-unknown-release-id.json" `
        -ReadinessReportPath $readinessReportRelativePath `
        -EnvironmentName "staging" `
        -Operator "admin-vps-bootstrap-readiness-release-guard" `
        -ReleaseId "missing-release-id-for-regression" `
        -LocalSqlite 2>&1
    $readinessExitCode = $LASTEXITCODE
    $ErrorActionPreference = $previousErrorActionPreference

    if ($readinessExitCode -eq 0) {
        throw "Bootstrap readiness accepted unknown ReleaseId."
    }

    if (Test-Path -LiteralPath $readinessReportPath) {
        throw "Bootstrap readiness created report artifact after unknown ReleaseId failure."
    }

    if (Test-Path -LiteralPath $bootstrapSmokeReportPath) {
        throw "Bootstrap readiness created bootstrap smoke artifact after unknown ReleaseId failure."
    }

    $text = [string]::Join("`n", @($output | ForEach-Object { [string]$_ }))
    if ($text.IndexOf("ReleaseId must exist in backend/src/VpnPlatform.Api/AppReleases/releases.json", [System.StringComparison]::OrdinalIgnoreCase) -lt 0) {
        throw "Bootstrap readiness failed for an unexpected reason: $text"
    }

    if ($text.IndexOf("bootstrap-readiness-release-guard-password", [System.StringComparison]::OrdinalIgnoreCase) -ge 0) {
        throw "Bootstrap readiness leaked password in regression output."
    }

    Write-Output "admin vps bootstrap readiness release guard valid"
}
finally {
    if ($null -eq $previousPassword) {
        [Environment]::SetEnvironmentVariable($passwordEnvName, $null, "Process")
    } else {
        [Environment]::SetEnvironmentVariable($passwordEnvName, $previousPassword, "Process")
    }

    foreach ($path in @($readinessReportPath, $bootstrapSmokeReportPath)) {
        if (Test-Path -LiteralPath $path) {
            Remove-Item -LiteralPath $path -Force
        }
    }
}
