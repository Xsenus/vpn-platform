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

function Set-ScopedEnv {
    param(
        [hashtable]$Previous,
        [string]$Name,
        [AllowNull()][string]$Value
    )

    if (-not $Previous.ContainsKey($Name)) {
        $Previous[$Name] = [Environment]::GetEnvironmentVariable($Name, "Process")
    }

    [Environment]::SetEnvironmentVariable($Name, $Value, "Process")
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
        [AllowNull()][string]$EnvMaxEvidenceChainMinutes,
        [string[]]$AdditionalArguments = @(),
        [bool]$ExpectPreflightReport = $true,
        [switch]$UseSameReportPath,
        [string]$Operator = "admin-vps-smoke-flow-wrapper-regression",
        [switch]$OmitOperator,
        [string]$ExpectedPreflightOperator = "",
        [string]$EnvironmentName = "Local",
        [switch]$OmitEnvironmentName,
        [AllowNull()][string]$EnvEnvironmentName,
        [string]$ExpectedPreflightEnvironmentName = "",
        [string]$ExpectedPreflightApiBaseUrl = "",
        [string]$ExpectedPreflightAdminWebUrl = "",
        [string]$ExpectedPreflightAdminEmail = ""
    )

    $scenarioPath = Join-Path $outputFullPath $Name
    New-Item -ItemType Directory -Path $scenarioPath -Force | Out-Null

    $stdoutPath = Join-Path $scenarioPath "stdout.log"
    $stderrPath = Join-Path $scenarioPath "stderr.log"
    $smokeReportPath = Join-Path $scenarioPath "admin-vps-smoke-report.json"
    $preflightReportPath = Join-Path $scenarioPath "admin-vps-smoke-preflight-report.json"
    if ($UseSameReportPath) {
        $preflightReportPath = $smokeReportPath
    }
    $wrapperPath = Join-Path $repoRoot "scripts/admin-vps-smoke.ps1"
    $previous = @{}

    try {
        Set-ScopedEnv -Previous $previous -Name "ADMIN_VPS_SMOKE_ADMIN_PASSWORD" -Value $Password
        Set-ScopedEnv -Previous $previous -Name "ADMIN_VPS_SMOKE_MAX_EVIDENCE_CHAIN_MINUTES" -Value $EnvMaxEvidenceChainMinutes
        Set-ScopedEnv -Previous $previous -Name "ADMIN_VPS_SMOKE_ENVIRONMENT" -Value $EnvEnvironmentName

        $arguments = @(
                "-NoProfile",
                "-ExecutionPolicy", "Bypass",
                "-File", $wrapperPath,
                "-ApiBaseUrl", $ApiBaseUrl,
                "-AdminWebUrl", $AdminWebUrl,
                "-AdminEmail", $AdminEmail,
                "-SmokeReportPath", $smokeReportPath,
                "-PreflightReportPath", $preflightReportPath,
                "-FrontendPath", $FrontendPath,
                "-AccountBootstrapChecked"
            )

        if (-not $OmitEnvironmentName) {
            $arguments += @("-EnvironmentName", $EnvironmentName)
        }

        if (-not $OmitOperator) {
            $arguments += @("-Operator", $Operator)
        }

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
            if ($output.IndexOf("Admin VPS smoke flow is ready to run.", [System.StringComparison]::OrdinalIgnoreCase) -ge 0) {
                throw "Admin VPS smoke flow should not be ready after parameter binding scenario '$Name'."
            }

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

        if (-not [string]::IsNullOrWhiteSpace($ExpectedPreflightOperator) -and [string]$preflightReport.operator -ne $ExpectedPreflightOperator) {
            throw "Preflight report operator should be '$ExpectedPreflightOperator' for scenario '$Name', got '$($preflightReport.operator)'."
        }

        if (-not [string]::IsNullOrWhiteSpace($ExpectedPreflightEnvironmentName) -and [string]$preflightReport.environmentName -ne $ExpectedPreflightEnvironmentName) {
            throw "Preflight report environmentName should be '$ExpectedPreflightEnvironmentName' for scenario '$Name', got '$($preflightReport.environmentName)'."
        }

        if (-not [string]::IsNullOrWhiteSpace($ExpectedPreflightApiBaseUrl) -and [string]$preflightReport.apiBaseUrl -ne $ExpectedPreflightApiBaseUrl) {
            throw "Preflight report apiBaseUrl should be '$ExpectedPreflightApiBaseUrl' for scenario '$Name', got '$($preflightReport.apiBaseUrl)'."
        }

        if (-not [string]::IsNullOrWhiteSpace($ExpectedPreflightAdminWebUrl) -and [string]$preflightReport.adminWebUrl -ne $ExpectedPreflightAdminWebUrl) {
            throw "Preflight report adminWebUrl should be '$ExpectedPreflightAdminWebUrl' for scenario '$Name', got '$($preflightReport.adminWebUrl)'."
        }

        if (-not [string]::IsNullOrWhiteSpace($ExpectedPreflightAdminEmail) -and [string]$preflightReport.adminEmail -ne $ExpectedPreflightAdminEmail) {
            throw "Preflight report adminEmail should be '$ExpectedPreflightAdminEmail' for scenario '$Name', got '$($preflightReport.adminEmail)'."
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
        -Name "format-max-evidence-chain-minutes" `
        -ApiBaseUrl "http://127.0.0.1:18201" `
        -AdminWebUrl "http://127.0.0.1:18205/admin/" `
        -AdminEmail "fresh-admin@example.test" `
        -FrontendPath "frontend" `
        -Password "LocalAdminPassword123!" `
        -ExpectedMessage "MaxEvidenceChainMinutes must be an integer" `
        -AdditionalArguments @("-MaxEvidenceChainMinutes", "not-a-number") `
        -ExpectPreflightReport $false

    $testedFailures += Invoke-WrapperFailure `
        -Name "format-env-max-evidence-chain-minutes" `
        -ApiBaseUrl "http://127.0.0.1:18201" `
        -AdminWebUrl "http://127.0.0.1:18205/admin/" `
        -AdminEmail "fresh-admin@example.test" `
        -FrontendPath "frontend" `
        -Password "LocalAdminPassword123!" `
        -ExpectedMessage "MaxEvidenceChainMinutes must be an integer" `
        -EnvMaxEvidenceChainMinutes "not-a-number" `
        -ExpectPreflightReport $false

    $testedFailures += Invoke-WrapperFailure `
        -Name "bad-max-evidence-chain-minutes" `
        -ApiBaseUrl "http://127.0.0.1:18201" `
        -AdminWebUrl "http://127.0.0.1:18205/admin/" `
        -AdminEmail "fresh-admin@example.test" `
        -FrontendPath "frontend" `
        -Password "LocalAdminPassword123!" `
        -ExpectedMessage "MaxEvidenceChainMinutes must be greater than 0" `
        -AdditionalArguments @("-MaxEvidenceChainMinutes", "0") `
        -ExpectPreflightReport $false

    $testedFailures += Invoke-WrapperFailure `
        -Name "bad-env-max-evidence-chain-minutes" `
        -ApiBaseUrl "http://127.0.0.1:18201" `
        -AdminWebUrl "http://127.0.0.1:18205/admin/" `
        -AdminEmail "fresh-admin@example.test" `
        -FrontendPath "frontend" `
        -Password "LocalAdminPassword123!" `
        -ExpectedMessage "MaxEvidenceChainMinutes must be greater than 0" `
        -EnvMaxEvidenceChainMinutes "0" `
        -ExpectPreflightReport $false

    $testedFailures += Invoke-WrapperFailure `
        -Name "too-high-max-evidence-chain-minutes" `
        -ApiBaseUrl "http://127.0.0.1:18201" `
        -AdminWebUrl "http://127.0.0.1:18205/admin/" `
        -AdminEmail "fresh-admin@example.test" `
        -FrontendPath "frontend" `
        -Password "LocalAdminPassword123!" `
        -ExpectedMessage "MaxEvidenceChainMinutes must be less than or equal to 1440" `
        -AdditionalArguments @("-MaxEvidenceChainMinutes", "1441") `
        -ExpectPreflightReport $false

    $testedFailures += Invoke-WrapperFailure `
        -Name "too-high-env-max-evidence-chain-minutes" `
        -ApiBaseUrl "http://127.0.0.1:18201" `
        -AdminWebUrl "http://127.0.0.1:18205/admin/" `
        -AdminEmail "fresh-admin@example.test" `
        -FrontendPath "frontend" `
        -Password "LocalAdminPassword123!" `
        -ExpectedMessage "MaxEvidenceChainMinutes must be less than or equal to 1440" `
        -EnvMaxEvidenceChainMinutes "1441" `
        -ExpectPreflightReport $false

    $testedFailures += Invoke-WrapperFailure `
        -Name "unknown-release-id" `
        -ApiBaseUrl "http://127.0.0.1:18201" `
        -AdminWebUrl "http://127.0.0.1:18205/admin/" `
        -AdminEmail "fresh-admin@example.test" `
        -FrontendPath "frontend" `
        -Password "LocalAdminPassword123!" `
        -ExpectedMessage "ReleaseId must exist in backend/src/VpnPlatform.Api/AppReleases/releases.json" `
        -AdditionalArguments @("-ReleaseId", "missing-release-id-for-regression") `
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
        -Name "default-operator-missing-password" `
        -ApiBaseUrl "http://127.0.0.1:18201" `
        -AdminWebUrl "http://127.0.0.1:18205/admin/" `
        -AdminEmail "fresh-admin@example.test" `
        -FrontendPath "frontend" `
        -Password $null `
        -ExpectedMessage "passwordEnvPresent must be true" `
        -ExpectedFailedCheck "password-env-present" `
        -OmitOperator `
        -ExpectedPreflightOperator "manual-operator"

    $testedFailures += Invoke-WrapperFailure `
        -Name "default-environment-missing-password" `
        -ApiBaseUrl "http://127.0.0.1:18201" `
        -AdminWebUrl "http://127.0.0.1:18205/admin/" `
        -AdminEmail "fresh-admin@example.test" `
        -FrontendPath "frontend" `
        -Password $null `
        -ExpectedMessage "passwordEnvPresent must be true" `
        -ExpectedFailedCheck "password-env-present" `
        -OmitEnvironmentName `
        -EnvEnvironmentName "   " `
        -ExpectedPreflightEnvironmentName "staging"

    $testedFailures += Invoke-WrapperFailure `
        -Name "preflight-identity-values-normalized" `
        -ApiBaseUrl " http://127.0.0.1:18201 " `
        -AdminWebUrl " http://127.0.0.1:18205/admin/ " `
        -AdminEmail " fresh-admin@example.test " `
        -FrontendPath "frontend" `
        -Password $null `
        -ExpectedMessage "passwordEnvPresent must be true" `
        -ExpectedFailedCheck "password-env-present" `
        -ExpectedPreflightApiBaseUrl "http://127.0.0.1:18201" `
        -ExpectedPreflightAdminWebUrl "http://127.0.0.1:18205/admin/" `
        -ExpectedPreflightAdminEmail "fresh-admin@example.test"

    $testedFailures += Invoke-WrapperFailure `
        -Name "bad-api-url" `
        -ApiBaseUrl "not-a-url" `
        -AdminWebUrl "http://127.0.0.1:18205/admin/" `
        -AdminEmail "fresh-admin@example.test" `
        -FrontendPath "frontend" `
        -Password "LocalAdminPassword123!" `
        -ExpectedMessage "ApiBaseUrl must be an absolute http or https URL" `
        -ExpectPreflightReport $false

    $testedFailures += Invoke-WrapperFailure `
        -Name "bad-admin-web-url" `
        -ApiBaseUrl "http://127.0.0.1:18201" `
        -AdminWebUrl "not-a-url" `
        -AdminEmail "fresh-admin@example.test" `
        -FrontendPath "frontend" `
        -Password "LocalAdminPassword123!" `
        -ExpectedMessage "AdminWebUrl must be an absolute http or https URL" `
        -ExpectPreflightReport $false

    $testedFailures += Invoke-WrapperFailure `
        -Name "bad-admin-email" `
        -ApiBaseUrl "http://127.0.0.1:18201" `
        -AdminWebUrl "http://127.0.0.1:18205/admin/" `
        -AdminEmail "not-an-email" `
        -FrontendPath "frontend" `
        -Password "LocalAdminPassword123!" `
        -ExpectedMessage "AdminEmail must contain an email address" `
        -ExpectPreflightReport $false

    $testedFailures += Invoke-WrapperFailure `
        -Name "same-report-paths" `
        -ApiBaseUrl "http://127.0.0.1:18201" `
        -AdminWebUrl "http://127.0.0.1:18205/admin/" `
        -AdminEmail "fresh-admin@example.test" `
        -FrontendPath "frontend" `
        -Password "LocalAdminPassword123!" `
        -ExpectedMessage "PreflightReportPath must be different from SmokeReportPath" `
        -ExpectPreflightReport $false `
        -UseSameReportPath

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
