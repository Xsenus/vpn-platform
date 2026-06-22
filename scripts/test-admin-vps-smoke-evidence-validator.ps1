param(
    [string]$OutputDirectory = "tmp/admin-vps-smoke-evidence-validator-regression-test",
    [switch]$KeepArtifacts,
    [switch]$WriteJson
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot

function Resolve-WorkspacePath {
    param([Parameter(Mandatory = $true)][string]$Path)

    if ([System.IO.Path]::IsPathRooted($Path)) {
        return [System.IO.Path]::GetFullPath($Path)
    }

    return [System.IO.Path]::GetFullPath((Join-Path $repoRoot $Path))
}

function Assert-InWorkspace {
    param([Parameter(Mandatory = $true)][string]$Path)

    $fullPath = [System.IO.Path]::GetFullPath($Path)
    $fullRoot = [System.IO.Path]::GetFullPath($repoRoot)
    if (-not $fullPath.StartsWith($fullRoot, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "Path must stay inside repository workspace: $fullPath"
    }
}

function Write-Utf8NoBomJson {
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [Parameter(Mandatory = $true)][object]$Value
    )

    [System.IO.File]::WriteAllText($Path, ($Value | ConvertTo-Json -Depth 12), [System.Text.UTF8Encoding]::new($false))
}

function Get-FileSha256 {
    param([Parameter(Mandatory = $true)][string]$Path)

    $sha256 = [System.Security.Cryptography.SHA256]::Create()
    try {
        $bytes = [System.IO.File]::ReadAllBytes($Path)
        $hash = $sha256.ComputeHash($bytes)
        return [System.BitConverter]::ToString($hash).Replace("-", "").ToLowerInvariant()
    }
    finally {
        $sha256.Dispose()
    }
}

function Format-ReportTimestamp {
    param([DateTimeOffset]$Value)

    $Value.ToUniversalTime().ToString("yyyyMMdd-HHmmss")
}

function Invoke-EvidenceValidator {
    param(
        [Parameter(Mandatory = $true)][string]$PreflightPath,
        [Parameter(Mandatory = $true)][string]$SmokePath,
        [string]$ExpectedPreflightReportSha256 = "",
        [string]$ExpectedSmokeReportSha256 = ""
    )

    $validator = Join-Path $repoRoot "scripts/validate-admin-vps-smoke-evidence.ps1"
    $validatorArgs = @{
        PreflightReportPath = $PreflightPath
        SmokeReportPath = $SmokePath
    }

    if (-not [string]::IsNullOrWhiteSpace($ExpectedPreflightReportSha256)) {
        $validatorArgs.ExpectedPreflightReportSha256 = $ExpectedPreflightReportSha256
    }

    if (-not [string]::IsNullOrWhiteSpace($ExpectedSmokeReportSha256)) {
        $validatorArgs.ExpectedSmokeReportSha256 = $ExpectedSmokeReportSha256
    }

    return & $validator @validatorArgs 6>&1 2>&1
}

function Assert-FailsWith {
    param(
        [Parameter(Mandatory = $true)][scriptblock]$Action,
        [Parameter(Mandatory = $true)][string]$ExpectedMessage
    )

    try {
        & $Action | Out-Null
    }
    catch {
        $message = $_.Exception.Message
        if ($message.IndexOf($ExpectedMessage, [System.StringComparison]::OrdinalIgnoreCase) -lt 0) {
            throw "Expected failure containing '$ExpectedMessage', actual: $message"
        }

        return $message
    }

    throw "Expected command to fail with '$ExpectedMessage'."
}

function New-Section {
    param([Parameter(Mandatory = $true)][string]$Id)

    return [ordered]@{
        id = $Id
        route = "/admin/#$Id"
        status = "passed"
        httpStatus = 200
        loaded = $true
        evidence = "Section $Id loaded on VPS admin; no failed API responses observed."
    }
}

function New-EvidencePair {
    param(
        [Parameter(Mandatory = $true)][string]$PreflightPath,
        [Parameter(Mandatory = $true)][string]$SmokePath
    )

    $generatedAt = [DateTimeOffset]::Parse("2026-06-19T00:00:00+07:00")
    $startedAt = $generatedAt.AddMinutes(1)
    $completedAt = $generatedAt.AddMinutes(2)
    $apiBaseUrl = "https://api.example.test"
    $adminWebUrl = "https://admin.example.test/admin"
    $releaseId = "2026-06-19-admin-vps-smoke-evidence-validator"
    $operator = "evidence-validator-regression"
    $sections = @(
        "dashboard",
        "users",
        "payments",
        "tariffs",
        "subscriptions",
        "vpn",
        "nodes",
        "panels",
        "support",
        "audit",
        "bot",
        "releases",
        "faq",
        "content",
        "scenarios",
        "provisioning"
    ) | ForEach-Object { New-Section -Id $_ }

    $preflight = [ordered]@{
        reportId = "admin-vps-smoke-preflight-" + (Format-ReportTimestamp $generatedAt)
        generatedAt = $generatedAt.ToString("O")
        environmentName = "staging"
        operator = $operator
        releaseId = $releaseId
        apiBaseUrl = $apiBaseUrl
        adminWebUrl = $adminWebUrl
        adminEmail = "owner@example.test"
        smokeReportPath = $SmokePath
        preflightReportPath = $PreflightPath
        passwordEnvPresent = $true
        readyForLiveSmoke = $true
        checks = @(
            [ordered]@{ name = "api-base-url"; passed = $true; message = "ok" },
            [ordered]@{ name = "admin-web-url"; passed = $true; message = "ok" },
            [ordered]@{ name = "admin-email"; passed = $true; message = "ok" },
            [ordered]@{ name = "password-env-present"; passed = $true; message = "present [hidden]" },
            [ordered]@{ name = "frontend-directory"; passed = $true; message = "ok" },
            [ordered]@{ name = "package-command"; passed = $true; message = "ok" },
            [ordered]@{ name = "browser-runner"; passed = $true; message = "ok" },
            [ordered]@{ name = "report-validator"; passed = $true; message = "ok" },
            [ordered]@{ name = "preflight-validator"; passed = $true; message = "ok" }
        )
    }

    $smoke = [ordered]@{
        reportId = "admin-vps-smoke-" + (Format-ReportTimestamp $startedAt)
        environmentName = "staging"
        apiBaseUrl = $apiBaseUrl
        adminWebUrl = $adminWebUrl
        adminEmail = "owner@example.test"
        smokeReportPath = $SmokePath
        startedAt = $startedAt.ToString("O")
        completedAt = $completedAt.ToString("O")
        releaseId = $releaseId
        operator = $operator
        notes = "Synthetic sanitized evidence validator regression report."
        accountBootstrapChecked = $true
        adminLoginPassed = $true
        noJsErrors = $true
        noUnauthorizedAfterLogin = $true
        sections = @($sections)
    }

    Write-Utf8NoBomJson -Path $PreflightPath -Value $preflight
    Write-Utf8NoBomJson -Path $SmokePath -Value $smoke
}

$outputFullPath = Resolve-WorkspacePath $OutputDirectory
Assert-InWorkspace $outputFullPath

if (Test-Path -LiteralPath $outputFullPath) {
    Remove-Item -LiteralPath $outputFullPath -Recurse -Force
}

New-Item -ItemType Directory -Path $outputFullPath -Force | Out-Null

try {
    $preflightPath = Join-Path $outputFullPath "admin-vps-smoke-preflight-report.json"
    $smokePath = Join-Path $outputFullPath "admin-vps-smoke-report.json"
    New-EvidencePair -PreflightPath $preflightPath -SmokePath $smokePath

    $validOutput = Invoke-EvidenceValidator -PreflightPath $preflightPath -SmokePath $smokePath
    $validOutputText = ($validOutput -join "`n")
    if ($validOutputText.IndexOf("preflightReportPath", [System.StringComparison]::OrdinalIgnoreCase) -lt 0) {
        throw "Valid smoke evidence output must include preflightReportPath. Output: $validOutputText"
    }

    if ($validOutputText.IndexOf("sectionsContractPath", [System.StringComparison]::OrdinalIgnoreCase) -lt 0) {
        throw "Valid smoke evidence output must include sectionsContractPath. Output: $validOutputText"
    }

    foreach ($expectedIdentityField in @("adminEmail", "owner@example.test", "operator", "evidence-validator-regression")) {
        if ($validOutputText.IndexOf($expectedIdentityField, [System.StringComparison]::OrdinalIgnoreCase) -lt 0) {
            throw "Valid smoke evidence output must include $expectedIdentityField. Output: $validOutputText"
        }
    }

    foreach ($expectedReportIdField in @("preflightReportId", "admin-vps-smoke-preflight-20260618-170000", "smokeReportId", "admin-vps-smoke-20260618-170100")) {
        if ($validOutputText.IndexOf($expectedReportIdField, [System.StringComparison]::OrdinalIgnoreCase) -lt 0) {
            throw "Valid smoke evidence output must include $expectedReportIdField. Output: $validOutputText"
        }
    }

    foreach ($expectedGateField in @("accountBootstrapChecked", "adminLoginPassed", "noJsErrors", "noUnauthorizedAfterLogin", '"accountBootstrapChecked":true', '"adminLoginPassed":true', '"noJsErrors":true', '"noUnauthorizedAfterLogin":true')) {
        if ($validOutputText.IndexOf($expectedGateField, [System.StringComparison]::OrdinalIgnoreCase) -lt 0) {
            throw "Valid smoke evidence output must include $expectedGateField. Output: $validOutputText"
        }
    }

    if ($validOutputText.IndexOf("preflightReportSha256", [System.StringComparison]::OrdinalIgnoreCase) -lt 0) {
        throw "Valid smoke evidence output must include preflightReportSha256. Output: $validOutputText"
    }

    if ($validOutputText.IndexOf("smokeReportSha256", [System.StringComparison]::OrdinalIgnoreCase) -lt 0) {
        throw "Valid smoke evidence output must include smokeReportSha256. Output: $validOutputText"
    }

    foreach ($expectedSummaryField in @("preflightGeneratedAt", "smokeStartedAt", "smokeCompletedAt", "preflightToSmokeSeconds", "smokeDurationSeconds", '"preflightToSmokeSeconds":60', '"smokeDurationSeconds":60', '"sections":16', '"passed":16', '"failed":0', '"blocked":0', '"skipped":0')) {
        if ($validOutputText.IndexOf($expectedSummaryField, [System.StringComparison]::OrdinalIgnoreCase) -lt 0) {
            throw "Valid smoke evidence output must include $expectedSummaryField. Output: $validOutputText"
        }
    }

    $validExpectedSha256Output = Invoke-EvidenceValidator `
        -PreflightPath $preflightPath `
        -SmokePath $smokePath `
        -ExpectedPreflightReportSha256 (Get-FileSha256 $preflightPath) `
        -ExpectedSmokeReportSha256 (Get-FileSha256 $smokePath)
    $validExpectedSha256OutputText = ($validExpectedSha256Output -join "`n")
    if ($validExpectedSha256OutputText.IndexOf("admin vps smoke evidence valid", [System.StringComparison]::OrdinalIgnoreCase) -lt 0) {
        throw "Valid smoke evidence output with expected SHA256 must pass. Output: $validExpectedSha256OutputText"
    }

    $testedFailures = @()

    $testedFailures += [ordered]@{
        name = "mismatched-expected-preflight-sha256"
        message = Assert-FailsWith -ExpectedMessage "preflightReportSha256 does not match expected SHA256" -Action {
            Invoke-EvidenceValidator -PreflightPath $preflightPath -SmokePath $smokePath -ExpectedPreflightReportSha256 ("0" * 64)
        }
    }

    $badApiPreflight = Join-Path $outputFullPath "bad-api-preflight.json"
    $badApi = Get-Content -LiteralPath $preflightPath -Raw -Encoding UTF8 | ConvertFrom-Json
    $badApi.preflightReportPath = $badApiPreflight
    $badApi.apiBaseUrl = "https://different-api.example.test"
    Write-Utf8NoBomJson -Path $badApiPreflight -Value $badApi
    $testedFailures += [ordered]@{
        name = "mismatched-api-url"
        message = Assert-FailsWith -ExpectedMessage "mismatch for apiBaseUrl" -Action {
            Invoke-EvidenceValidator -PreflightPath $badApiPreflight -SmokePath $smokePath
        }
    }

    $badEmailSmokePath = Join-Path $outputFullPath "bad-admin-email-smoke.json"
    $badEmailSmoke = Get-Content -LiteralPath $smokePath -Raw -Encoding UTF8 | ConvertFrom-Json
    $badEmailSmoke.adminEmail = "other-owner@example.test"
    $badEmailSmoke.smokeReportPath = $badEmailSmokePath
    Write-Utf8NoBomJson -Path $badEmailSmokePath -Value $badEmailSmoke
    $testedFailures += [ordered]@{
        name = "mismatched-admin-email"
        message = Assert-FailsWith -ExpectedMessage "mismatch for adminEmail" -Action {
            Invoke-EvidenceValidator -PreflightPath $preflightPath -SmokePath $badEmailSmokePath
        }
    }

    $badPathPreflight = Join-Path $outputFullPath "bad-smoke-path-preflight.json"
    $badPath = Get-Content -LiteralPath $preflightPath -Raw -Encoding UTF8 | ConvertFrom-Json
    $badPath.preflightReportPath = $badPathPreflight
    $badPath.smokeReportPath = (Join-Path $outputFullPath "other-smoke-report.json")
    Write-Utf8NoBomJson -Path $badPathPreflight -Value $badPath
    $testedFailures += [ordered]@{
        name = "mismatched-smoke-report-path"
        message = Assert-FailsWith -ExpectedMessage "mismatch for smokeReportPath" -Action {
            Invoke-EvidenceValidator -PreflightPath $badPathPreflight -SmokePath $smokePath
        }
    }

    $badPreflightPathReport = Join-Path $outputFullPath "bad-preflight-path-preflight.json"
    $badPreflightPath = Get-Content -LiteralPath $preflightPath -Raw -Encoding UTF8 | ConvertFrom-Json
    $badPreflightPath.preflightReportPath = (Join-Path $outputFullPath "other-preflight-report.json")
    Write-Utf8NoBomJson -Path $badPreflightPathReport -Value $badPreflightPath
    $testedFailures += [ordered]@{
        name = "mismatched-preflight-report-path"
        message = Assert-FailsWith -ExpectedMessage "mismatch for preflightReportPath" -Action {
            Invoke-EvidenceValidator -PreflightPath $badPreflightPathReport -SmokePath $smokePath
        }
    }

    $badReleasePreflight = Join-Path $outputFullPath "bad-release-preflight.json"
    $badRelease = Get-Content -LiteralPath $preflightPath -Raw -Encoding UTF8 | ConvertFrom-Json
    $badRelease.preflightReportPath = $badReleasePreflight
    $badRelease.releaseId = "different-release"
    Write-Utf8NoBomJson -Path $badReleasePreflight -Value $badRelease
    $testedFailures += [ordered]@{
        name = "mismatched-release-id"
        message = Assert-FailsWith -ExpectedMessage "mismatch for releaseId" -Action {
            Invoke-EvidenceValidator -PreflightPath $badReleasePreflight -SmokePath $smokePath
        }
    }

    $duplicateReportIdPreflightPath = Join-Path $outputFullPath "duplicate-report-id-preflight.json"
    $duplicateReportIdSmokePath = Join-Path $outputFullPath "duplicate-report-id-smoke.json"
    $duplicateReportIdPreflight = Get-Content -LiteralPath $preflightPath -Raw -Encoding UTF8 | ConvertFrom-Json
    $duplicateReportIdPreflight.preflightReportPath = $duplicateReportIdPreflightPath
    $duplicateReportIdPreflight.smokeReportPath = $duplicateReportIdSmokePath
    Write-Utf8NoBomJson -Path $duplicateReportIdPreflightPath -Value $duplicateReportIdPreflight
    $duplicateReportIdSmoke = Get-Content -LiteralPath $smokePath -Raw -Encoding UTF8 | ConvertFrom-Json
    $duplicateReportIdSmoke.smokeReportPath = $duplicateReportIdSmokePath
    $duplicateReportIdSmoke.reportId = $duplicateReportIdPreflight.reportId
    Write-Utf8NoBomJson -Path $duplicateReportIdSmokePath -Value $duplicateReportIdSmoke
    $testedFailures += [ordered]@{
        name = "duplicate-report-id"
        message = Assert-FailsWith -ExpectedMessage "report ids must be unique" -Action {
            Invoke-EvidenceValidator -PreflightPath $duplicateReportIdPreflightPath -SmokePath $duplicateReportIdSmokePath
        }
    }

    $badPreflightReportIdPath = Join-Path $outputFullPath "bad-preflight-report-id-prefix.json"
    $badPreflightReportId = Get-Content -LiteralPath $preflightPath -Raw -Encoding UTF8 | ConvertFrom-Json
    $badPreflightReportId.preflightReportPath = $badPreflightReportIdPath
    $badPreflightReportId.reportId = "manual-preflight-regression"
    Write-Utf8NoBomJson -Path $badPreflightReportIdPath -Value $badPreflightReportId
    $testedFailures += [ordered]@{
        name = "bad-preflight-report-id-prefix"
        message = Assert-FailsWith -ExpectedMessage "preflight reportId must start with admin-vps-smoke-preflight-" -Action {
            Invoke-EvidenceValidator -PreflightPath $badPreflightReportIdPath -SmokePath $smokePath
        }
    }

    $badSmokeReportIdPreflightPath = Join-Path $outputFullPath "bad-smoke-report-id-prefix-preflight.json"
    $badSmokeReportIdSmokePath = Join-Path $outputFullPath "bad-smoke-report-id-prefix-smoke.json"
    $badSmokeReportIdPreflight = Get-Content -LiteralPath $preflightPath -Raw -Encoding UTF8 | ConvertFrom-Json
    $badSmokeReportIdPreflight.preflightReportPath = $badSmokeReportIdPreflightPath
    $badSmokeReportIdPreflight.smokeReportPath = $badSmokeReportIdSmokePath
    Write-Utf8NoBomJson -Path $badSmokeReportIdPreflightPath -Value $badSmokeReportIdPreflight
    $badSmokeReportIdSmoke = Get-Content -LiteralPath $smokePath -Raw -Encoding UTF8 | ConvertFrom-Json
    $badSmokeReportIdSmoke.smokeReportPath = $badSmokeReportIdSmokePath
    $badSmokeReportIdSmoke.reportId = "admin-vps-smoke-preflight-browser-regression"
    Write-Utf8NoBomJson -Path $badSmokeReportIdSmokePath -Value $badSmokeReportIdSmoke
    $testedFailures += [ordered]@{
        name = "bad-smoke-report-id-prefix"
        message = Assert-FailsWith -ExpectedMessage "smoke reportId must start with admin-vps-smoke-" -Action {
            Invoke-EvidenceValidator -PreflightPath $badSmokeReportIdPreflightPath -SmokePath $badSmokeReportIdSmokePath
        }
    }

    $badPreflightReportIdTimestampPath = Join-Path $outputFullPath "bad-preflight-report-id-timestamp.json"
    $badPreflightReportIdTimestamp = Get-Content -LiteralPath $preflightPath -Raw -Encoding UTF8 | ConvertFrom-Json
    $badPreflightReportIdTimestamp.preflightReportPath = $badPreflightReportIdTimestampPath
    $badPreflightReportIdTimestamp.reportId = "admin-vps-smoke-preflight-manual"
    Write-Utf8NoBomJson -Path $badPreflightReportIdTimestampPath -Value $badPreflightReportIdTimestamp
    $testedFailures += [ordered]@{
        name = "bad-preflight-report-id-timestamp"
        message = Assert-FailsWith -ExpectedMessage "preflight reportId must match admin-vps-smoke-preflight-yyyyMMdd-HHmmss" -Action {
            Invoke-EvidenceValidator -PreflightPath $badPreflightReportIdTimestampPath -SmokePath $smokePath
        }
    }

    $badSmokeReportIdTimestampPreflightPath = Join-Path $outputFullPath "bad-smoke-report-id-timestamp-preflight.json"
    $badSmokeReportIdTimestampSmokePath = Join-Path $outputFullPath "bad-smoke-report-id-timestamp-smoke.json"
    $badSmokeReportIdTimestampPreflight = Get-Content -LiteralPath $preflightPath -Raw -Encoding UTF8 | ConvertFrom-Json
    $badSmokeReportIdTimestampPreflight.preflightReportPath = $badSmokeReportIdTimestampPreflightPath
    $badSmokeReportIdTimestampPreflight.smokeReportPath = $badSmokeReportIdTimestampSmokePath
    Write-Utf8NoBomJson -Path $badSmokeReportIdTimestampPreflightPath -Value $badSmokeReportIdTimestampPreflight
    $badSmokeReportIdTimestampSmoke = Get-Content -LiteralPath $smokePath -Raw -Encoding UTF8 | ConvertFrom-Json
    $badSmokeReportIdTimestampSmoke.smokeReportPath = $badSmokeReportIdTimestampSmokePath
    $badSmokeReportIdTimestampSmoke.reportId = "admin-vps-smoke-manual"
    Write-Utf8NoBomJson -Path $badSmokeReportIdTimestampSmokePath -Value $badSmokeReportIdTimestampSmoke
    $testedFailures += [ordered]@{
        name = "bad-smoke-report-id-timestamp"
        message = Assert-FailsWith -ExpectedMessage "smoke reportId must match admin-vps-smoke-yyyyMMdd-HHmmss" -Action {
            Invoke-EvidenceValidator -PreflightPath $badSmokeReportIdTimestampPreflightPath -SmokePath $badSmokeReportIdTimestampSmokePath
        }
    }

    $emptyReleasePreflight = Join-Path $outputFullPath "empty-release-preflight.json"
    $emptyRelease = Get-Content -LiteralPath $preflightPath -Raw -Encoding UTF8 | ConvertFrom-Json
    $emptyRelease.preflightReportPath = $emptyReleasePreflight
    $emptyRelease.releaseId = ""
    Write-Utf8NoBomJson -Path $emptyReleasePreflight -Value $emptyRelease
    $testedFailures += [ordered]@{
        name = "missing-preflight-release-id"
        message = Assert-FailsWith -ExpectedMessage "field is empty: releaseId" -Action {
            Invoke-EvidenceValidator -PreflightPath $emptyReleasePreflight -SmokePath $smokePath
        }
    }

    $badTimingPreflight = Join-Path $outputFullPath "bad-timing-preflight.json"
    $badTiming = Get-Content -LiteralPath $preflightPath -Raw -Encoding UTF8 | ConvertFrom-Json
    $badTiming.preflightReportPath = $badTimingPreflight
    $badTiming.generatedAt = "2026-06-20T00:00:00+07:00"
    Write-Utf8NoBomJson -Path $badTimingPreflight -Value $badTiming
    $testedFailures += [ordered]@{
        name = "preflight-after-smoke"
        message = Assert-FailsWith -ExpectedMessage "must not be after smoke completedAt" -Action {
            Invoke-EvidenceValidator -PreflightPath $badTimingPreflight -SmokePath $smokePath
        }
    }

    $badSmokeStartPreflightPath = Join-Path $outputFullPath "bad-smoke-start-preflight.json"
    $badSmokeStartPreflight = Get-Content -LiteralPath $preflightPath -Raw -Encoding UTF8 | ConvertFrom-Json
    $badSmokeStartPath = Join-Path $outputFullPath "bad-smoke-start-report.json"
    $badSmokeStartPreflight.preflightReportPath = $badSmokeStartPreflightPath
    $badSmokeStartPreflight.smokeReportPath = $badSmokeStartPath
    Write-Utf8NoBomJson -Path $badSmokeStartPreflightPath -Value $badSmokeStartPreflight
    $badSmokeStart = Get-Content -LiteralPath $smokePath -Raw -Encoding UTF8 | ConvertFrom-Json
    $badSmokeStart.smokeReportPath = $badSmokeStartPath
    $badSmokeStart.startedAt = "2026-06-18T23:59:00+07:00"
    Write-Utf8NoBomJson -Path $badSmokeStartPath -Value $badSmokeStart
    $testedFailures += [ordered]@{
        name = "smoke-started-before-preflight"
        message = Assert-FailsWith -ExpectedMessage "smoke startedAt must not be before preflight generatedAt" -Action {
            Invoke-EvidenceValidator -PreflightPath $badSmokeStartPreflightPath -SmokePath $badSmokeStartPath
        }
    }

    $badSmokeCompletedPath = Join-Path $outputFullPath "bad-smoke-completed-report.json"
    $badSmokeCompleted = Get-Content -LiteralPath $smokePath -Raw -Encoding UTF8 | ConvertFrom-Json
    $badSmokeCompleted.smokeReportPath = $badSmokeCompletedPath
    $badSmokeCompleted.completedAt = "2026-06-19T00:00:30+07:00"
    Write-Utf8NoBomJson -Path $badSmokeCompletedPath -Value $badSmokeCompleted
    $testedFailures += [ordered]@{
        name = "smoke-completed-before-started"
        message = Assert-FailsWith -ExpectedMessage "completedAt must be greater than or equal to startedAt" -Action {
            Invoke-EvidenceValidator -PreflightPath $preflightPath -SmokePath $badSmokeCompletedPath
        }
    }

    $failedSmokePath = Join-Path $outputFullPath "failed-smoke-report.json"
    $failedSmoke = Get-Content -LiteralPath $smokePath -Raw -Encoding UTF8 | ConvertFrom-Json
    $failedSmoke.smokeReportPath = $failedSmokePath
    $failedSmoke.adminLoginPassed = $false
    Write-Utf8NoBomJson -Path $failedSmokePath -Value $failedSmoke
    $testedFailures += [ordered]@{
        name = "failed-smoke-report"
        message = Assert-FailsWith -ExpectedMessage "adminLoginPassed must be true" -Action {
            Invoke-EvidenceValidator -PreflightPath $preflightPath -SmokePath $failedSmokePath
        }
    }

    $result = [ordered]@{
        status = "passed"
        validValidatorOutput = $validOutputText
        validExpectedSha256Output = $validExpectedSha256OutputText
        testedFailures = @($testedFailures)
    }

    if ($WriteJson) {
        Write-Output ($result | ConvertTo-Json -Depth 8)
    }
    else {
        Write-Host "admin vps smoke evidence validator regression passed $($result | ConvertTo-Json -Depth 8 -Compress)"
    }
}
finally {
    if (-not $KeepArtifacts -and (Test-Path -LiteralPath $outputFullPath)) {
        Remove-Item -LiteralPath $outputFullPath -Recurse -Force
    }
}
