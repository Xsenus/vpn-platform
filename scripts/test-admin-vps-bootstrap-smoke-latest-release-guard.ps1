param()

$ErrorActionPreference = "Stop"

function Resolve-RepoPath {
    param([string]$RelativePath)

    $root = Split-Path -Parent $PSScriptRoot
    return Join-Path $root $RelativePath
}

function Invoke-ExpectedStaleReleaseFailure {
    param(
        [Parameter(Mandatory = $true)][string]$ValidatorPath,
        [Parameter(Mandatory = $true)][string]$ReportPath,
        [Parameter(Mandatory = $true)][string]$ModeSwitch
    )

    $previousErrorActionPreference = $ErrorActionPreference
    $ErrorActionPreference = "Continue"
    $output = & powershell -NoProfile -ExecutionPolicy Bypass -File $ValidatorPath -ReportPath $ReportPath $ModeSwitch 2>&1
    $validatorExitCode = $LASTEXITCODE
    $ErrorActionPreference = $previousErrorActionPreference

    if ($validatorExitCode -eq 0) {
        throw "Validator accepted stale releaseId in $ModeSwitch mode."
    }

    $text = [string]::Join("`n", @($output | ForEach-Object { [string]$_ }))
    if ($text.IndexOf("must match latest active release", [System.StringComparison]::OrdinalIgnoreCase) -lt 0) {
        throw "Validator failed for an unexpected reason: $text"
    }
}

$readinessValidatorPath = Resolve-RepoPath "scripts/validate-admin-vps-bootstrap-smoke-readiness-report.ps1"
$bootstrapValidatorPath = Resolve-RepoPath "scripts/validate-admin-vps-bootstrap-smoke-report.ps1"
$tmpDirectory = Resolve-RepoPath "tmp"
New-Item -ItemType Directory -Force -Path $tmpDirectory | Out-Null

$readinessReportPath = Join-Path $tmpDirectory "admin-vps-bootstrap-readiness-stale-release-guard.json"
$bootstrapReportPath = Join-Path $tmpDirectory "admin-vps-bootstrap-smoke-stale-release-guard.json"

try {
    $checks = @(
        "api-base-url",
        "admin-web-url",
        "admin-email",
        "password-env-name",
        "password-env-name-safe",
        "password-env-present",
        "password-length",
        "provider-supported",
        "local-or-confirm-reset",
        "connection-string",
        "project-file",
        "frontend-directory",
        "package-command",
        "bootstrap-script",
        "smoke-wrapper",
        "readiness-validator",
        "bootstrap-report-validator"
    ) | ForEach-Object {
        [ordered]@{
            name = $_
            passed = $true
            message = "sanitized passed check $_"
        }
    }

    $readinessReport = [ordered]@{
        reportId = "admin-vps-bootstrap-readiness-stale-release-guard"
        generatedAt = "2026-07-01T15:00:00+07:00"
        environmentName = "staging"
        operator = "admin-vps-bootstrap-latest-release-guard"
        releaseId = "stale-release-id"
        apiBaseUrl = "https://api.example.test"
        adminWebUrl = "https://admin.example.test"
        adminEmail = "admin@example.test"
        provider = "Sqlite"
        passwordEnvName = "ADMIN_VPS_BOOTSTRAP_PASSWORD"
        smokeReportPath = "tmp/admin-vps-smoke-stale-release-guard.json"
        preflightReportPath = "tmp/admin-vps-smoke-preflight-stale-release-guard.json"
        bootstrapSmokeReportPath = "tmp/admin-vps-bootstrap-smoke-stale-release-guard.json"
        readinessReportPath = "tmp/admin-vps-bootstrap-readiness-stale-release-guard.json"
        localSqlite = $true
        applyMigrations = $true
        confirmBootstrapReset = $false
        connectionStringPresent = $false
        passwordEnvPresent = $true
        passwordLengthOk = $true
        readyForBootstrapSmoke = $true
        checks = @($checks)
    }

    $bootstrapReport = [ordered]@{
        reportId = "admin-vps-bootstrap-smoke-stale-release-guard"
        environmentName = "staging"
        apiBaseUrl = "https://api.example.test"
        adminWebUrl = "https://admin.example.test"
        adminEmail = "admin@example.test"
        provider = "Sqlite"
        passwordEnvName = "ADMIN_VPS_BOOTSTRAP_PASSWORD"
        smokeReportPath = "tmp/admin-vps-smoke-stale-release-guard.json"
        preflightReportPath = "tmp/admin-vps-smoke-preflight-stale-release-guard.json"
        readinessReportPath = "tmp/admin-vps-bootstrap-readiness-stale-release-guard.json"
        bootstrapSmokeReportPath = "tmp/admin-vps-bootstrap-smoke-stale-release-guard.json"
        generatedAt = "2026-07-01T15:30:00+07:00"
        completedAt = "2026-07-01T15:30:00+07:00"
        releaseId = "stale-release-id"
        operator = "admin-vps-bootstrap-latest-release-guard"
        status = "passed"
        bootstrapResetConfirmed = $false
        localSqlite = $true
        dryRun = $false
        accountBootstrapChecked = $true
        passwordEnvPresent = $true
    }

    $readinessReport | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $readinessReportPath -Encoding UTF8
    $bootstrapReport | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $bootstrapReportPath -Encoding UTF8

    Invoke-ExpectedStaleReleaseFailure -ValidatorPath $readinessValidatorPath -ReportPath $readinessReportPath -ModeSwitch "-RequireReady"
    Invoke-ExpectedStaleReleaseFailure -ValidatorPath $bootstrapValidatorPath -ReportPath $bootstrapReportPath -ModeSwitch "-RequirePassed"

    Write-Output "admin vps bootstrap smoke latest release guard valid"
}
finally {
    foreach ($path in @($readinessReportPath, $bootstrapReportPath)) {
        if (Test-Path -LiteralPath $path) {
            Remove-Item -LiteralPath $path -Force
        }
    }
}
