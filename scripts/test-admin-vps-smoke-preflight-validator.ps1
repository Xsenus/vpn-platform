param(
    [string]$OutputDirectory = "tmp/admin-vps-smoke-preflight-validator-regression-test",
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

function Write-Utf8NoBomFile {
    param(
        [Parameter(Mandatory = $true)][string]$PathValue,
        [Parameter(Mandatory = $true)][string]$Content
    )

    [System.IO.File]::WriteAllText($PathValue, $Content, [System.Text.UTF8Encoding]::new($false))
}

function Invoke-PreflightValidator {
    param(
        [Parameter(Mandatory = $true)][string]$ReportPath,
        [switch]$AllowNotReady
    )

    $validatorPath = Join-Path $repoRoot "scripts/validate-admin-vps-smoke-preflight-report.ps1"
    if ($AllowNotReady) {
        return & $validatorPath -ReportPath $ReportPath 2>&1
    }

    return & $validatorPath -ReportPath $ReportPath -RequireReady 2>&1
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

function Copy-ReportJson {
    param(
        [Parameter(Mandatory = $true)][object]$Source,
        [Parameter(Mandatory = $true)][string]$DestinationPath
    )

    Write-Utf8NoBomFile -PathValue $DestinationPath -Content ($Source | ConvertTo-Json -Depth 12)
    return $DestinationPath
}

$outputFullPath = Resolve-WorkspacePath $OutputDirectory
Assert-InWorkspace $outputFullPath

if (Test-Path -LiteralPath $outputFullPath) {
    Remove-Item -LiteralPath $outputFullPath -Recurse -Force
}

New-Item -ItemType Directory -Path $outputFullPath -Force | Out-Null

$previousPassword = $env:ADMIN_VPS_SMOKE_ADMIN_PASSWORD

try {
    $env:ADMIN_VPS_SMOKE_ADMIN_PASSWORD = "LocalAdminPassword123!"

    $validReportPath = Join-Path $outputFullPath "admin-vps-smoke-preflight-report.json"
    $preflightPath = Join-Path $repoRoot "scripts/admin-vps-smoke-preflight.ps1"
    & $preflightPath `
        -ApiBaseUrl "http://127.0.0.1:18201" `
        -AdminWebUrl "http://127.0.0.1:18205/admin/" `
        -AdminEmail "fresh-admin@example.test" `
        -SmokeReportPath "tmp/admin-vps-smoke-report.json" `
        -PreflightReportPath $validReportPath `
        -EnvironmentName "Local" `
        -Operator "preflight-validator-regression" `
        -RequirePassword | Out-Null

    $validOutput = Invoke-PreflightValidator -ReportPath $validReportPath
    $validReportContent = Get-Content -LiteralPath $validReportPath -Raw -Encoding UTF8
    if ($validReportContent.Contains("LocalAdminPassword123!")) {
        throw "Admin VPS smoke preflight regression report leaked password."
    }

    $validReport = $validReportContent | ConvertFrom-Json
    if ([string]$validReport.remoteReleaseStatus -ne "not-required") {
        throw "Expected standalone preflight report remoteReleaseStatus to be not-required."
    }

    if (-not $validReport.remoteReleaseMatched) {
        throw "Expected standalone preflight report remoteReleaseMatched to be true."
    }

    $testedFailures = @()

    $emptyRelease = $validReportContent | ConvertFrom-Json
    $emptyRelease.releaseId = ""
    $emptyReleasePath = Copy-ReportJson -Source $emptyRelease -DestinationPath (Join-Path $outputFullPath "empty-release-id.json")
    $testedFailures += [ordered]@{
        name = "empty-release-id"
        message = Assert-FailsWith -ExpectedMessage "field is empty: releaseId" -Action {
            Invoke-PreflightValidator -ReportPath $emptyReleasePath
        }
    }

    $badReady = $validReportContent | ConvertFrom-Json
    $badReady.readyForLiveSmoke = $false
    $badReadyPath = Copy-ReportJson -Source $badReady -DestinationPath (Join-Path $outputFullPath "bad-ready-flag.json")
    $testedFailures += [ordered]@{
        name = "bad-ready-flag"
        message = Assert-FailsWith -ExpectedMessage "readyForLiveSmoke must be true" -Action {
            Invoke-PreflightValidator -ReportPath $badReadyPath
        }
    }

    $failedCheck = $validReportContent | ConvertFrom-Json
    $failedCheck.checks[0].passed = $false
    $failedCheckPath = Copy-ReportJson -Source $failedCheck -DestinationPath (Join-Path $outputFullPath "failed-check.json")
    $testedFailures += [ordered]@{
        name = "failed-check"
        message = Assert-FailsWith -ExpectedMessage "must be passed when -RequireReady is used" -Action {
            Invoke-PreflightValidator -ReportPath $failedCheckPath
        }
    }

    $missingCheck = $validReportContent | ConvertFrom-Json
    $missingCheck.checks = @($missingCheck.checks | Where-Object { [string]$_.name -ne "preflight-validator" })
    $missingCheckPath = Copy-ReportJson -Source $missingCheck -DestinationPath (Join-Path $outputFullPath "missing-check.json")
    $testedFailures += [ordered]@{
        name = "missing-check"
        message = Assert-FailsWith -ExpectedMessage "missing check: preflight-validator" -Action {
            Invoke-PreflightValidator -ReportPath $missingCheckPath
        }
    }

    $duplicateCheck = $validReportContent | ConvertFrom-Json
    $duplicateCheck.checks = @($duplicateCheck.checks) + $duplicateCheck.checks[0]
    $duplicateCheckPath = Copy-ReportJson -Source $duplicateCheck -DestinationPath (Join-Path $outputFullPath "duplicate-check.json")
    $testedFailures += [ordered]@{
        name = "duplicate-check"
        message = Assert-FailsWith -ExpectedMessage "duplicated check" -Action {
            Invoke-PreflightValidator -ReportPath $duplicateCheckPath
        }
    }

    $secretMarker = $validReportContent | ConvertFrom-Json
    $secretMarker.operator = "bearer should-not-be-accepted"
    $secretMarkerPath = Copy-ReportJson -Source $secretMarker -DestinationPath (Join-Path $outputFullPath "secret-marker.json")
    $testedFailures += [ordered]@{
        name = "secret-marker"
        message = Assert-FailsWith -ExpectedMessage "forbidden secret marker" -Action {
            Invoke-PreflightValidator -ReportPath $secretMarkerPath
        }
    }

    $validMismatch = $validReportContent | ConvertFrom-Json
    $validMismatch.remoteReleaseCheckRequired = $true
    $validMismatch.remoteReleaseMatched = $false
    $validMismatch.remoteReleaseId = "2026-06-24-older-deploy"
    $validMismatch.remoteReleaseStatus = "mismatch"
    $validMismatch.remoteReleaseMessage = "Remote latest release differs from the local smoke release."
    $validMismatch.readyForLiveSmoke = $false
    foreach ($check in $validMismatch.checks) {
        if ([string]$check.name -eq "remote-latest-release") {
            $check.passed = $false
        }
    }

    $validMismatchPath = Copy-ReportJson -Source $validMismatch -DestinationPath (Join-Path $outputFullPath "valid-remote-release-mismatch.json")
    $validMismatchOutput = Invoke-PreflightValidator -ReportPath $validMismatchPath -AllowNotReady

    $badRemoteStatus = $validReportContent | ConvertFrom-Json
    $badRemoteStatus.remoteReleaseCheckRequired = $true
    $badRemoteStatus.remoteReleaseMatched = $true
    $badRemoteStatus.remoteReleaseId = "2026-06-24-older-deploy"
    $badRemoteStatus.remoteReleaseStatus = "matched"
    $badRemoteStatusPath = Copy-ReportJson -Source $badRemoteStatus -DestinationPath (Join-Path $outputFullPath "bad-remote-release-status.json")
    $testedFailures += [ordered]@{
        name = "bad-remote-release-status"
        message = Assert-FailsWith -ExpectedMessage "remoteReleaseId must equal releaseId" -Action {
            Invoke-PreflightValidator -ReportPath $badRemoteStatusPath
        }
    }

    $result = [ordered]@{
        status = "passed"
        validReportPath = $validReportPath
        validValidatorOutput = ($validOutput -join "`n")
        validMismatchValidatorOutput = ($validMismatchOutput -join "`n")
        testedFailures = @($testedFailures)
    }

    if ($WriteJson) {
        Write-Output ($result | ConvertTo-Json -Depth 8)
    }
    else {
        Write-Host "admin vps smoke preflight validator regression passed $($result | ConvertTo-Json -Depth 8 -Compress)"
    }
}
finally {
    if ($null -eq $previousPassword) {
        Remove-Item Env:\ADMIN_VPS_SMOKE_ADMIN_PASSWORD -ErrorAction SilentlyContinue
    }
    else {
        $env:ADMIN_VPS_SMOKE_ADMIN_PASSWORD = $previousPassword
    }

    if (-not $KeepArtifacts -and (Test-Path -LiteralPath $outputFullPath)) {
        Remove-Item -LiteralPath $outputFullPath -Recurse -Force
    }
}
