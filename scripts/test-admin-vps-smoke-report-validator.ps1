param(
    [string]$OutputDirectory = "tmp/admin-vps-smoke-report-validator-regression-test",
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

function Invoke-SmokeReportValidator {
    param([Parameter(Mandatory = $true)][string]$ReportPath)

    $validatorPath = Join-Path $repoRoot "scripts/validate-admin-vps-smoke-report.ps1"
    return & $validatorPath -ReportPath $ReportPath -RequireAllPassed 2>&1
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

$requiredSections = @(
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
)

$outputFullPath = Resolve-WorkspacePath $OutputDirectory
Assert-InWorkspace $outputFullPath

if (Test-Path -LiteralPath $outputFullPath) {
    Remove-Item -LiteralPath $outputFullPath -Recurse -Force
}

New-Item -ItemType Directory -Path $outputFullPath -Force | Out-Null

try {
    $now = (Get-Date).ToUniversalTime()
    $validReportPath = Join-Path $outputFullPath "admin-vps-smoke-report.json"
    $sections = @(
        foreach ($section in $requiredSections) {
            [ordered]@{
                id = $section
                label = $section
                route = "/admin/$section"
                status = "passed"
                httpStatus = 200
                loaded = $true
                evidence = "Section $section loaded in synthetic validator regression evidence."
            }
        }
    )

    $validReport = [ordered]@{
        reportId = "admin-vps-smoke-validator-regression-" + $now.ToString("yyyyMMdd-HHmmss")
        environmentName = "Local"
        apiBaseUrl = "http://127.0.0.1:18201"
        adminWebUrl = "http://127.0.0.1:18205/admin/"
        startedAt = $now.AddMinutes(-1).ToString("O")
        completedAt = $now.ToString("O")
        releaseId = "manual-admin-vps-smoke-validator-regression"
        operator = "admin-vps-smoke-report-validator-regression"
        notes = "Synthetic sanitized validator regression report without credentials or tokens."
        accountBootstrapChecked = $true
        adminLoginPassed = $true
        noJsErrors = $true
        noUnauthorizedAfterLogin = $true
        sections = $sections
    }

    Copy-ReportJson -Source $validReport -DestinationPath $validReportPath | Out-Null
    $validOutput = Invoke-SmokeReportValidator -ReportPath $validReportPath

    $testedFailures = @()

    $badHttpStatus = Get-Content -LiteralPath $validReportPath -Raw -Encoding UTF8 | ConvertFrom-Json
    $badHttpStatus.sections[0].httpStatus = 500
    $badHttpStatusPath = Copy-ReportJson -Source $badHttpStatus -DestinationPath (Join-Path $outputFullPath "bad-http-status.json")
    $testedFailures += [ordered]@{
        name = "bad-http-status"
        message = Assert-FailsWith -ExpectedMessage "must contain successful httpStatus" -Action {
            Invoke-SmokeReportValidator -ReportPath $badHttpStatusPath
        }
    }

    $placeholderEvidence = Get-Content -LiteralPath $validReportPath -Raw -Encoding UTF8 | ConvertFrom-Json
    $placeholderEvidence.sections[0].evidence = "TODO: safe screenshot name or browser smoke note without secrets"
    $placeholderEvidencePath = Copy-ReportJson -Source $placeholderEvidence -DestinationPath (Join-Path $outputFullPath "placeholder-evidence.json")
    $testedFailures += [ordered]@{
        name = "placeholder-evidence"
        message = Assert-FailsWith -ExpectedMessage "without placeholder markers" -Action {
            Invoke-SmokeReportValidator -ReportPath $placeholderEvidencePath
        }
    }

    $failedStatus = Get-Content -LiteralPath $validReportPath -Raw -Encoding UTF8 | ConvertFrom-Json
    $failedStatus.sections[0].status = "failed"
    $failedStatusPath = Copy-ReportJson -Source $failedStatus -DestinationPath (Join-Path $outputFullPath "failed-status.json")
    $testedFailures += [ordered]@{
        name = "failed-status"
        message = Assert-FailsWith -ExpectedMessage "must be passed when -RequireAllPassed is used" -Action {
            Invoke-SmokeReportValidator -ReportPath $failedStatusPath
        }
    }

    $missingSection = Get-Content -LiteralPath $validReportPath -Raw -Encoding UTF8 | ConvertFrom-Json
    $missingSection.sections = @($missingSection.sections | Where-Object { [string]$_.id -ne "provisioning" })
    $missingSectionPath = Copy-ReportJson -Source $missingSection -DestinationPath (Join-Path $outputFullPath "missing-section.json")
    $testedFailures += [ordered]@{
        name = "missing-section"
        message = Assert-FailsWith -ExpectedMessage "missing admin section: provisioning" -Action {
            Invoke-SmokeReportValidator -ReportPath $missingSectionPath
        }
    }

    $falseGate = Get-Content -LiteralPath $validReportPath -Raw -Encoding UTF8 | ConvertFrom-Json
    $falseGate.noUnauthorizedAfterLogin = $false
    $falseGatePath = Copy-ReportJson -Source $falseGate -DestinationPath (Join-Path $outputFullPath "false-gate.json")
    $testedFailures += [ordered]@{
        name = "false-gate"
        message = Assert-FailsWith -ExpectedMessage "noUnauthorizedAfterLogin must be true" -Action {
            Invoke-SmokeReportValidator -ReportPath $falseGatePath
        }
    }

    $secretMarker = Get-Content -LiteralPath $validReportPath -Raw -Encoding UTF8 | ConvertFrom-Json
    $secretMarker.notes = "bearer should-not-be-accepted"
    $secretMarkerPath = Copy-ReportJson -Source $secretMarker -DestinationPath (Join-Path $outputFullPath "secret-marker.json")
    $testedFailures += [ordered]@{
        name = "secret-marker"
        message = Assert-FailsWith -ExpectedMessage "forbidden secret marker" -Action {
            Invoke-SmokeReportValidator -ReportPath $secretMarkerPath
        }
    }

    $result = [ordered]@{
        status = "passed"
        validReportPath = $validReportPath
        validValidatorOutput = ($validOutput -join "`n")
        testedFailures = @($testedFailures)
    }

    if ($WriteJson) {
        Write-Output ($result | ConvertTo-Json -Depth 8)
    }
    else {
        Write-Host "admin vps smoke report validator regression passed $($result | ConvertTo-Json -Depth 8 -Compress)"
    }
}
finally {
    if (-not $KeepArtifacts -and (Test-Path -LiteralPath $outputFullPath)) {
        Remove-Item -LiteralPath $outputFullPath -Recurse -Force
    }
}
