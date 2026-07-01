param()

$ErrorActionPreference = "Stop"

function Resolve-RepoPath {
    param([string]$RelativePath)

    $root = Split-Path -Parent $PSScriptRoot
    return Join-Path $root $RelativePath
}

$validatorPath = Resolve-RepoPath "scripts/validate-admin-vps-smoke-evidence.ps1"
$tmpDirectory = Resolve-RepoPath "tmp"
New-Item -ItemType Directory -Force -Path $tmpDirectory | Out-Null

$preflightPath = Join-Path $tmpDirectory "admin-vps-smoke-evidence-stale-release-preflight.json"
$smokePath = Join-Path $tmpDirectory "admin-vps-smoke-evidence-stale-release-smoke.json"
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
    $preflight = [ordered]@{
        reportId = "admin-vps-smoke-preflight-20260701-104500"
        generatedAt = "2026-07-01T17:45:00+07:00"
        environmentName = "staging"
        apiBaseUrl = "https://vpn.example.test"
        adminWebUrl = "https://vpn.example.test/admin/"
        adminEmail = "admin@example.test"
        operator = "admin-vps-smoke-evidence-latest-release-guard"
        smokeReportPath = $smokePath
        preflightReportPath = $preflightPath
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

    $sections = @(
        "dashboard",
        "users",
        "subscriptions",
        "payments",
        "tickets",
        "nodes",
        "plans",
        "promo-codes",
        "content",
        "faq",
        "notifications",
        "releases",
        "analytics",
        "support",
        "audit",
        "settings"
    )

    $smoke = [ordered]@{
        reportId = "admin-vps-smoke-20260701-104600"
        environmentName = "staging"
        apiBaseUrl = "https://vpn.example.test"
        adminWebUrl = "https://vpn.example.test/admin/"
        adminEmail = "admin@example.test"
        smokeReportPath = $smokePath
        startedAt = "2026-07-01T17:46:00+07:00"
        completedAt = "2026-07-01T17:47:00+07:00"
        releaseId = "stale-release-id"
        operator = "admin-vps-smoke-evidence-latest-release-guard"
        notes = "sanitized regression report without secrets"
        accountBootstrapChecked = $true
        adminLoginPassed = $true
        noJsErrors = $true
        noUnauthorizedAfterLogin = $true
        sections = @($sections | ForEach-Object {
            [ordered]@{
                id = $_
                route = "/admin/#$_"
                status = "passed"
                httpStatus = 200
                loaded = $true
                evidence = "sanitized evidence for $_"
            }
        })
    }

    $preflight | ConvertTo-Json -Depth 10 | Set-Content -LiteralPath $preflightPath -Encoding UTF8
    $smoke | ConvertTo-Json -Depth 10 | Set-Content -LiteralPath $smokePath -Encoding UTF8

    $previousErrorActionPreference = $ErrorActionPreference
    $ErrorActionPreference = "Continue"
    $output = & powershell -NoProfile -ExecutionPolicy Bypass -File $validatorPath -PreflightReportPath $preflightPath -SmokeReportPath $smokePath 2>&1
    $validatorExitCode = $LASTEXITCODE
    $ErrorActionPreference = $previousErrorActionPreference

    if ($validatorExitCode -eq 0) {
        throw "Validator accepted stale releaseId in admin VPS smoke evidence chain."
    }

    $text = [string]::Join("`n", @($output | ForEach-Object { [string]$_ }))
    if ($text.IndexOf("must match latest active release", [System.StringComparison]::OrdinalIgnoreCase) -lt 0) {
        throw "Validator failed for an unexpected reason: $text"
    }

    Write-Output "admin vps smoke evidence latest release guard valid"
}
finally {
    foreach ($path in @($preflightPath, $smokePath)) {
        if (Test-Path -LiteralPath $path) {
            Remove-Item -LiteralPath $path -Force
        }
    }
}
