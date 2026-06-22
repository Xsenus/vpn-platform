param(
    [string]$OutputDirectory = "tmp/admin-vps-smoke-flow-wrapper-regression-test",
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

function Set-ScopedPassword {
    param(
        [hashtable]$Previous,
        [AllowNull()][string]$Password
    )

    if (-not $Previous.ContainsKey("ADMIN_VPS_SMOKE_ADMIN_PASSWORD")) {
        $Previous["ADMIN_VPS_SMOKE_ADMIN_PASSWORD"] = [Environment]::GetEnvironmentVariable("ADMIN_VPS_SMOKE_ADMIN_PASSWORD", "Process")
    }

    [Environment]::SetEnvironmentVariable("ADMIN_VPS_SMOKE_ADMIN_PASSWORD", $Password, "Process")
}

function Invoke-WrapperFailure {
    param(
        [Parameter(Mandatory = $true)][string]$Name,
        [Parameter(Mandatory = $true)][string]$ApiBaseUrl,
        [Parameter(Mandatory = $true)][string]$AdminWebUrl,
        [Parameter(Mandatory = $true)][string]$AdminEmail,
        [Parameter(Mandatory = $true)][string]$FrontendPath,
        [AllowNull()][string]$Password,
        [Parameter(Mandatory = $true)][string]$ExpectedMessage,
        [string]$ExpectedFailedCheck = "",
        [string[]]$AdditionalArguments = @(),
        [bool]$ExpectPreflightReport = $true
    )

    $scenarioPath = Join-Path $outputFullPath $Name
    New-Item -ItemType Directory -Path $scenarioPath -Force | Out-Null

    $stdoutPath = Join-Path $scenarioPath "stdout.log"
    $stderrPath = Join-Path $scenarioPath "stderr.log"
    $smokeReportPath = Join-Path $scenarioPath "admin-vps-smoke-report.json"
    $preflightReportPath = Join-Path $scenarioPath "admin-vps-smoke-preflight-report.json"
    $wrapperPath = Join-Path $repoRoot "scripts/admin-vps-smoke.ps1"
    $previous = @{}

    try {
        Set-ScopedPassword -Previous $previous -Password $Password

        $arguments = @(
                "-NoProfile",
                "-ExecutionPolicy", "Bypass",
                "-File", $wrapperPath,
                "-ApiBaseUrl", $ApiBaseUrl,
                "-AdminWebUrl", $AdminWebUrl,
                "-AdminEmail", $AdminEmail,
                "-SmokeReportPath", $smokeReportPath,
                "-PreflightReportPath", $preflightReportPath,
                "-EnvironmentName", "Local",
                "-Operator", "admin-vps-smoke-flow-wrapper-regression",
                "-FrontendPath", $FrontendPath,
                "-AccountBootstrapChecked"
            )

        $arguments += $AdditionalArguments

        $process = Start-Process -FilePath "powershell" `
            -ArgumentList $arguments `
            -WorkingDirectory $repoRoot `
            -RedirectStandardOutput $stdoutPath `
            -RedirectStandardError $stderrPath `
            -PassThru `
            -Wait `
            -WindowStyle Hidden

        $output = ((Get-Content -LiteralPath $stdoutPath -Raw -ErrorAction SilentlyContinue) + "`n" + (Get-Content -LiteralPath $stderrPath -Raw -ErrorAction SilentlyContinue))

        if ($process.ExitCode -eq 0) {
            throw "Expected admin-vps-smoke.ps1 to fail for scenario '$Name'."
        }

        if ($output.IndexOf($ExpectedMessage, [System.StringComparison]::OrdinalIgnoreCase) -lt 0) {
            throw "Expected failure output for '$Name' to contain '$ExpectedMessage'. Actual output: $output"
        }

        if (-not [string]::IsNullOrEmpty($Password) -and $output.Contains($Password)) {
            throw "Admin VPS smoke flow wrapper leaked password in scenario '$Name'."
        }

        foreach ($forbiddenOutput in @("Admin VPS browser smoke is ready to run.", "e2e:admin-vps-smoke", "Admin VPS smoke flow completed.")) {
            if ($output.IndexOf($forbiddenOutput, [System.StringComparison]::OrdinalIgnoreCase) -ge 0) {
                throw "Browser smoke appears to have started before a valid preflight in scenario '$Name'."
            }
        }

        if (Test-Path -LiteralPath $smokeReportPath -PathType Leaf) {
            throw "Smoke report should not exist after failed preflight scenario '$Name'."
        }

        if (-not $ExpectPreflightReport) {
            if (Test-Path -LiteralPath $preflightReportPath -PathType Leaf) {
                throw "Preflight report should not exist after parameter binding scenario '$Name'."
            }

            return [ordered]@{
                name = $Name
                exitCode = $process.ExitCode
                expectedMessage = $ExpectedMessage
                expectedFailedCheck = ""
                preflightReportCreated = $false
                releaseId = ""
            }
        }

        if (-not (Test-Path -LiteralPath $preflightReportPath -PathType Leaf)) {
            throw "Preflight report should exist after failed preflight scenario '$Name'."
        }

        $preflightReport = Get-Content -LiteralPath $preflightReportPath -Raw -Encoding UTF8 | ConvertFrom-Json
        if ([string]::IsNullOrWhiteSpace([string]$preflightReport.releaseId)) {
            throw "Preflight report releaseId should be resolved before failed browser smoke scenario '$Name'."
        }

        $failedCheck = @($preflightReport.checks | Where-Object { [string]$_.name -eq $ExpectedFailedCheck }) | Select-Object -First 1
        if ($null -eq $failedCheck) {
            throw "Expected failed preflight check '$ExpectedFailedCheck' was not found in scenario '$Name'."
        }

        if ($failedCheck.passed -ne $false) {
            throw "Expected preflight check '$ExpectedFailedCheck' to fail in scenario '$Name'."
        }

        return [ordered]@{
            name = $Name
            exitCode = $process.ExitCode
            expectedMessage = $ExpectedMessage
            expectedFailedCheck = $ExpectedFailedCheck
            preflightReportCreated = $true
            releaseId = [string]$preflightReport.releaseId
        }
    }
    finally {
        foreach ($key in $previous.Keys) {
            [Environment]::SetEnvironmentVariable($key, $previous[$key], "Process")
        }
    }
}

$outputFullPath = Resolve-WorkspacePath $OutputDirectory
Assert-InWorkspace $outputFullPath

if (Test-Path -LiteralPath $outputFullPath) {
    Remove-Item -LiteralPath $outputFullPath -Recurse -Force
}

New-Item -ItemType Directory -Path $outputFullPath -Force | Out-Null

try {
    $testedFailures = @()
    $testedFailures += Invoke-WrapperFailure `
        -Name "bad-max-evidence-chain-minutes" `
        -ApiBaseUrl "http://127.0.0.1:18201" `
        -AdminWebUrl "http://127.0.0.1:18205/admin/" `
        -AdminEmail "fresh-admin@example.test" `
        -FrontendPath "frontend" `
        -Password "LocalAdminPassword123!" `
        -ExpectedMessage "ParameterArgumentValidationError" `
        -AdditionalArguments @("-MaxEvidenceChainMinutes", "0") `
        -ExpectPreflightReport $false

    $testedFailures += Invoke-WrapperFailure `
        -Name "missing-password" `
        -ApiBaseUrl "http://127.0.0.1:18201" `
        -AdminWebUrl "http://127.0.0.1:18205/admin/" `
        -AdminEmail "fresh-admin@example.test" `
        -FrontendPath "frontend" `
        -Password $null `
        -ExpectedMessage "passwordEnvPresent must be true" `
        -ExpectedFailedCheck "password-env-present"

    $testedFailures += Invoke-WrapperFailure `
        -Name "bad-api-url" `
        -ApiBaseUrl "not-a-url" `
        -AdminWebUrl "http://127.0.0.1:18205/admin/" `
        -AdminEmail "fresh-admin@example.test" `
        -FrontendPath "frontend" `
        -Password "LocalAdminPassword123!" `
        -ExpectedMessage "apiBaseUrl must be an absolute" `
        -ExpectedFailedCheck "api-base-url"

    $testedFailures += Invoke-WrapperFailure `
        -Name "missing-frontend" `
        -ApiBaseUrl "http://127.0.0.1:18201" `
        -AdminWebUrl "http://127.0.0.1:18205/admin/" `
        -AdminEmail "fresh-admin@example.test" `
        -FrontendPath "tmp/admin-vps-smoke-flow-wrapper-regression-test/missing-frontend/does-not-exist" `
        -Password "LocalAdminPassword123!" `
        -ExpectedMessage "readyForLiveSmoke must be true" `
        -ExpectedFailedCheck "frontend-directory"

    $result = [ordered]@{
        status = "passed"
        testedFailures = @($testedFailures)
    }

    if ($WriteJson) {
        Write-Output ($result | ConvertTo-Json -Depth 8)
    }
    else {
        Write-Host "admin vps smoke flow wrapper regression passed $($result | ConvertTo-Json -Depth 8 -Compress)"
    }
}
finally {
    if (-not $KeepArtifacts -and (Test-Path -LiteralPath $outputFullPath)) {
        Remove-Item -LiteralPath $outputFullPath -Recurse -Force
    }
}
