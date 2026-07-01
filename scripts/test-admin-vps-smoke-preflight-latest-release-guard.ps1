param()

$ErrorActionPreference = "Stop"

function Resolve-RepoPath {
    param([string]$RelativePath)

    $root = Split-Path -Parent $PSScriptRoot
    return Join-Path $root $RelativePath
}

$validatorPath = Resolve-RepoPath "scripts/validate-admin-vps-smoke-preflight-report.ps1"
$tmpDirectory = Resolve-RepoPath "tmp"
New-Item -ItemType Directory -Force -Path $tmpDirectory | Out-Null

$reportPath = Join-Path $tmpDirectory "admin-vps-smoke-preflight-stale-release-guard.json"
$requiredChecks = @(
    "api-base-url",
    "admin-web-url",
    "admin-email",
    "password-env-present",
    "frontend-directory",
    "package-command",
    "browser-runner",
    "report-validator",
    "preflight-validator",
    "remote-latest-release"
)

try {
    $report = [ordered]@{
        reportId = "admin-vps-smoke-preflight-stale-release-guard"
        generatedAt = "2026-07-01T17:45:00+07:00"
        environmentName = "staging"
        apiBaseUrl = "https://vpn.example.test"
        adminWebUrl = "https://vpn.example.test/admin/"
        adminEmail = "admin@example.test"
        operator = "local-regression"
        smokeReportPath = "tmp/admin-vps-smoke-report.json"
        preflightReportPath = $reportPath
        releaseId = "stale-release-id"
        remoteReleaseId = ""
        remoteReleaseCheckRequired = $false
        remoteReleaseMatched = $true
        remoteReleaseStatus = "not-required"
        remoteReleaseMessage = "Remote release check is disabled for this local regression."
        passwordEnvPresent = $true
        readyForLiveSmoke = $true
        checkCount = $requiredChecks.Count
        passedCheckCount = $requiredChecks.Count
        failedCheckCount = 0
        failedChecks = @()
        checks = @($requiredChecks | ForEach-Object {
            [ordered]@{
                name = $_
                passed = $true
                message = "passed"
            }
        })
    }

    $report | ConvertTo-Json -Depth 10 | Set-Content -LiteralPath $reportPath -Encoding UTF8

    $previousErrorActionPreference = $ErrorActionPreference
    $ErrorActionPreference = "Continue"
    $output = & powershell -NoProfile -ExecutionPolicy Bypass -File $validatorPath -ReportPath $reportPath -RequireReady 2>&1
    $validatorExitCode = $LASTEXITCODE
    $ErrorActionPreference = $previousErrorActionPreference

    if ($validatorExitCode -eq 0) {
        throw "Validator accepted stale releaseId in -RequireReady mode."
    }

    $text = [string]::Join("`n", @($output | ForEach-Object { [string]$_ }))
    if ($text.IndexOf("must match latest active release", [System.StringComparison]::OrdinalIgnoreCase) -lt 0) {
        throw "Validator failed for an unexpected reason: $text"
    }

    Write-Output "admin vps smoke preflight latest release guard valid"
}
finally {
    if (Test-Path -LiteralPath $reportPath) {
        Remove-Item -LiteralPath $reportPath -Force
    }

    if ((Test-Path -LiteralPath $tmpDirectory) -and -not (Get-ChildItem -LiteralPath $tmpDirectory -Force)) {
        Remove-Item -LiteralPath $tmpDirectory -Force
    }
}
