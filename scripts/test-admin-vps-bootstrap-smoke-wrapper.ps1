param(
    [string]$OutputDirectory = "tmp/admin-vps-bootstrap-smoke-wrapper-regression-test",
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

function Invoke-BootstrapSmokeScenario {
    param(
        [Parameter(Mandatory = $true)][string]$Name,
        [AllowNull()][string]$Password,
        [AllowNull()][string]$ConnectionString,
        [switch]$ConfirmBootstrapReset,
        [switch]$LocalSqlite,
        [switch]$DryRun,
        [Parameter(Mandatory = $true)][int]$ExpectedExitCode,
        [Parameter(Mandatory = $true)][string]$ExpectedMessage,
        [AllowNull()][string]$EnvMaxEvidenceChainMinutes,
        [string[]]$AdditionalArguments = @(),
        [string]$ApiBaseUrl = "http://127.0.0.1:18211",
        [string]$AdminWebUrl = "http://127.0.0.1:18215/admin/",
        [string]$AdminEmail = "fresh-bootstrap-admin@example.test",
        [switch]$UseSameReportPath,
        [string]$Operator = "admin-vps-bootstrap-smoke-wrapper-regression",
        [switch]$OmitOperator,
        [string]$ExpectedReadinessOperator = "",
        [string]$EnvironmentName = "Local",
        [switch]$OmitEnvironmentName,
        [AllowNull()][string]$EnvEnvironmentName,
        [string]$ExpectedReadinessEnvironmentName = ""
    )

    $scenarioPath = Join-Path $outputFullPath $Name
    New-Item -ItemType Directory -Path $scenarioPath -Force | Out-Null

    $stdoutPath = Join-Path $scenarioPath "stdout.log"
    $stderrPath = Join-Path $scenarioPath "stderr.log"
    $smokeReportPath = Join-Path $scenarioPath "admin-vps-smoke-report.json"
    $preflightReportPath = Join-Path $scenarioPath "admin-vps-smoke-preflight-report.json"
    $bootstrapSmokeReportPath = Join-Path $scenarioPath "admin-vps-bootstrap-smoke-report.json"
    $readinessReportPath = Join-Path $scenarioPath "admin-vps-bootstrap-smoke-readiness-report.json"
    if ($UseSameReportPath) {
        $preflightReportPath = $smokeReportPath
    }
    $wrapperPath = Join-Path $repoRoot "scripts/admin-vps-bootstrap-smoke.ps1"
    $previous = @{}

    try {
        Set-ScopedEnv -Previous $previous -Name "ADMIN_VPS_BOOTSTRAP_SMOKE_ADMIN_PASSWORD" -Value $Password
        Set-ScopedEnv -Previous $previous -Name "ADMIN_VPS_SMOKE_ADMIN_PASSWORD" -Value $null
        Set-ScopedEnv -Previous $previous -Name "ADMIN_VPS_SMOKE_MAX_EVIDENCE_CHAIN_MINUTES" -Value $EnvMaxEvidenceChainMinutes
        Set-ScopedEnv -Previous $previous -Name "ADMIN_VPS_SMOKE_ENVIRONMENT" -Value $EnvEnvironmentName
        Set-ScopedEnv -Previous $previous -Name "ConnectionStrings__DefaultConnection" -Value $ConnectionString

        $arguments = @(
            "-NoProfile",
            "-ExecutionPolicy", "Bypass",
            "-File", $wrapperPath,
            "-ApiBaseUrl", $ApiBaseUrl,
            "-AdminWebUrl", $AdminWebUrl,
            "-AdminEmail", $AdminEmail,
            "-SmokeReportPath", $smokeReportPath,
            "-PreflightReportPath", $preflightReportPath,
            "-BootstrapSmokeReportPath", $bootstrapSmokeReportPath,
            "-ReadinessReportPath", $readinessReportPath,
            "-FrontendPath", "frontend"
        )

        if (-not $OmitEnvironmentName) {
            $arguments += @("-EnvironmentName", $EnvironmentName)
        }

        if (-not $OmitOperator) {
            $arguments += @("-Operator", $Operator)
        }

        if ($ConfirmBootstrapReset) {
            $arguments += "-ConfirmBootstrapReset"
        }

        if ($LocalSqlite) {
            $arguments += "-LocalSqlite"
        }

        if ($DryRun) {
            $arguments += "-DryRun"
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

        if ($process.ExitCode -ne $ExpectedExitCode) {
            throw "Expected scenario '$Name' exit code $ExpectedExitCode, got $($process.ExitCode). Output: $output"
        }

        if ($output.IndexOf($ExpectedMessage, [System.StringComparison]::OrdinalIgnoreCase) -lt 0) {
            throw "Expected scenario '$Name' output to contain '$ExpectedMessage'. Actual output: $output"
        }

        if (-not [string]::IsNullOrEmpty($Password) -and $output.Contains($Password)) {
            throw "Admin VPS bootstrap smoke wrapper leaked password in scenario '$Name'."
        }

        $forbiddenOutputs = @("Admin VPS smoke flow is ready to run.", "Admin VPS browser smoke is ready to run.", "e2e:admin-vps-smoke", "Admin VPS bootstrap+smoke flow completed.")
        if (-not $DryRun) {
            $forbiddenOutputs += "Admin VPS bootstrap+smoke flow is ready to run."
        }

        foreach ($forbiddenOutput in $forbiddenOutputs) {
            if ($output.IndexOf($forbiddenOutput, [System.StringComparison]::OrdinalIgnoreCase) -ge 0) {
                throw "Admin smoke appears to have started in scenario '$Name'."
            }
        }

        $forbiddenArtifacts = @($smokeReportPath, $preflightReportPath, $bootstrapSmokeReportPath)
        if (-not $DryRun) {
            $forbiddenArtifacts += $readinessReportPath
        }

        foreach ($forbiddenArtifact in $forbiddenArtifacts) {
            if (Test-Path -LiteralPath $forbiddenArtifact -PathType Leaf) {
                throw "Smoke artifact should not exist after scenario '$Name': $forbiddenArtifact"
            }
        }

        $readinessReleaseId = ""
        if ($DryRun) {
            if (-not (Test-Path -LiteralPath $readinessReportPath -PathType Leaf)) {
                throw "Readiness report should exist for dry-run scenario '$Name'."
            }

            $readinessReport = Get-Content -LiteralPath $readinessReportPath -Raw -Encoding UTF8 | ConvertFrom-Json
            $readinessReleaseId = [string]$readinessReport.releaseId
            if ([string]::IsNullOrWhiteSpace($readinessReleaseId)) {
                throw "Readiness report releaseId should be resolved before dry-run smoke stop in scenario '$Name'."
            }

            if (-not [string]::IsNullOrWhiteSpace($ExpectedReadinessOperator) -and [string]$readinessReport.operator -ne $ExpectedReadinessOperator) {
                throw "Readiness report operator should be '$ExpectedReadinessOperator' for scenario '$Name', got '$($readinessReport.operator)'."
            }

            if (-not [string]::IsNullOrWhiteSpace($ExpectedReadinessEnvironmentName) -and [string]$readinessReport.environmentName -ne $ExpectedReadinessEnvironmentName) {
                throw "Readiness report environmentName should be '$ExpectedReadinessEnvironmentName' for scenario '$Name', got '$($readinessReport.environmentName)'."
            }
        }

        return [ordered]@{
            name = $Name
            exitCode = $process.ExitCode
            expectedMessage = $ExpectedMessage
            smokeArtifactsCreated = $false
            readinessReleaseId = $readinessReleaseId
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
    $testedScenarios = @()
    $testedScenarios += Invoke-BootstrapSmokeScenario `
        -Name "format-max-evidence-chain-minutes" `
        -Password "LocalBootstrapSmokePassword12345" `
        -ConnectionString "Data Source=tmp/admin-vps-bootstrap-smoke-wrapper-regression-test/format-max-evidence-chain-minutes/local.db" `
        -LocalSqlite `
        -ExpectedExitCode 1 `
        -ExpectedMessage "MaxEvidenceChainMinutes must be an integer" `
        -AdditionalArguments @("-MaxEvidenceChainMinutes", "not-a-number")

    $testedScenarios += Invoke-BootstrapSmokeScenario `
        -Name "format-env-max-evidence-chain-minutes" `
        -Password "LocalBootstrapSmokePassword12345" `
        -ConnectionString "Data Source=tmp/admin-vps-bootstrap-smoke-wrapper-regression-test/format-env-max-evidence-chain-minutes/local.db" `
        -LocalSqlite `
        -ExpectedExitCode 1 `
        -ExpectedMessage "MaxEvidenceChainMinutes must be an integer" `
        -EnvMaxEvidenceChainMinutes "not-a-number"

    $testedScenarios += Invoke-BootstrapSmokeScenario `
        -Name "bad-max-evidence-chain-minutes" `
        -Password "LocalBootstrapSmokePassword12345" `
        -ConnectionString "Data Source=tmp/admin-vps-bootstrap-smoke-wrapper-regression-test/bad-max-evidence-chain-minutes/local.db" `
        -LocalSqlite `
        -ExpectedExitCode 1 `
        -ExpectedMessage "MaxEvidenceChainMinutes must be greater than 0" `
        -AdditionalArguments @("-MaxEvidenceChainMinutes", "0")

    $testedScenarios += Invoke-BootstrapSmokeScenario `
        -Name "bad-env-max-evidence-chain-minutes" `
        -Password "LocalBootstrapSmokePassword12345" `
        -ConnectionString "Data Source=tmp/admin-vps-bootstrap-smoke-wrapper-regression-test/bad-env-max-evidence-chain-minutes/local.db" `
        -LocalSqlite `
        -ExpectedExitCode 1 `
        -ExpectedMessage "MaxEvidenceChainMinutes must be greater than 0" `
        -EnvMaxEvidenceChainMinutes "0"

    $testedScenarios += Invoke-BootstrapSmokeScenario `
        -Name "too-high-max-evidence-chain-minutes" `
        -Password "LocalBootstrapSmokePassword12345" `
        -ConnectionString "Data Source=tmp/admin-vps-bootstrap-smoke-wrapper-regression-test/too-high-max-evidence-chain-minutes/local.db" `
        -LocalSqlite `
        -ExpectedExitCode 1 `
        -ExpectedMessage "MaxEvidenceChainMinutes must be less than or equal to 1440" `
        -AdditionalArguments @("-MaxEvidenceChainMinutes", "1441")

    $testedScenarios += Invoke-BootstrapSmokeScenario `
        -Name "too-high-env-max-evidence-chain-minutes" `
        -Password "LocalBootstrapSmokePassword12345" `
        -ConnectionString "Data Source=tmp/admin-vps-bootstrap-smoke-wrapper-regression-test/too-high-env-max-evidence-chain-minutes/local.db" `
        -LocalSqlite `
        -ExpectedExitCode 1 `
        -ExpectedMessage "MaxEvidenceChainMinutes must be less than or equal to 1440" `
        -EnvMaxEvidenceChainMinutes "1441"

    $testedScenarios += Invoke-BootstrapSmokeScenario `
        -Name "unknown-release-id" `
        -Password "LocalBootstrapSmokePassword12345" `
        -ConnectionString "Data Source=tmp/admin-vps-bootstrap-smoke-wrapper-regression-test/unknown-release-id/local.db" `
        -LocalSqlite `
        -ExpectedExitCode 1 `
        -ExpectedMessage "ReleaseId must exist in backend/src/VpnPlatform.Api/AppReleases/releases.json" `
        -AdditionalArguments @("-ReleaseId", "missing-release-id-for-regression")

    $testedScenarios += Invoke-BootstrapSmokeScenario `
        -Name "bad-api-url" `
        -Password "LocalBootstrapSmokePassword12345" `
        -ConnectionString "Data Source=tmp/admin-vps-bootstrap-smoke-wrapper-regression-test/bad-api-url/local.db" `
        -LocalSqlite `
        -ExpectedExitCode 1 `
        -ExpectedMessage "ApiBaseUrl must be an absolute http or https URL" `
        -ApiBaseUrl "not-a-url"

    $testedScenarios += Invoke-BootstrapSmokeScenario `
        -Name "bad-admin-web-url" `
        -Password "LocalBootstrapSmokePassword12345" `
        -ConnectionString "Data Source=tmp/admin-vps-bootstrap-smoke-wrapper-regression-test/bad-admin-web-url/local.db" `
        -LocalSqlite `
        -ExpectedExitCode 1 `
        -ExpectedMessage "AdminWebUrl must be an absolute http or https URL" `
        -AdminWebUrl "not-a-url"

    $testedScenarios += Invoke-BootstrapSmokeScenario `
        -Name "bad-admin-email" `
        -Password "LocalBootstrapSmokePassword12345" `
        -ConnectionString "Data Source=tmp/admin-vps-bootstrap-smoke-wrapper-regression-test/bad-admin-email/local.db" `
        -LocalSqlite `
        -ExpectedExitCode 1 `
        -ExpectedMessage "AdminEmail must contain an email address" `
        -AdminEmail "not-an-email"

    $testedScenarios += Invoke-BootstrapSmokeScenario `
        -Name "same-report-paths" `
        -Password "LocalBootstrapSmokePassword12345" `
        -ConnectionString "Data Source=tmp/admin-vps-bootstrap-smoke-wrapper-regression-test/same-report-paths/local.db" `
        -LocalSqlite `
        -ExpectedExitCode 1 `
        -ExpectedMessage "PreflightReportPath must be different from SmokeReportPath" `
        -UseSameReportPath

    $testedScenarios += Invoke-BootstrapSmokeScenario `
        -Name "missing-password" `
        -Password $null `
        -ConnectionString "Host=127.0.0.1;Database=vpnplatform;Username=vpnplatform;Password=local-only" `
        -ConfirmBootstrapReset `
        -ExpectedExitCode 1 `
        -ExpectedMessage "Admin password env 'ADMIN_VPS_BOOTSTRAP_SMOKE_ADMIN_PASSWORD' is required"

    $testedScenarios += Invoke-BootstrapSmokeScenario `
        -Name "missing-confirm-bootstrap-reset" `
        -Password "LocalBootstrapSmokePassword12345" `
        -ConnectionString "Host=127.0.0.1;Database=vpnplatform;Username=vpnplatform;Password=local-only" `
        -ExpectedExitCode 1 `
        -ExpectedMessage "Pass -ConfirmBootstrapReset"

    $testedScenarios += Invoke-BootstrapSmokeScenario `
        -Name "missing-connection-string" `
        -Password "LocalBootstrapSmokePassword12345" `
        -ConnectionString $null `
        -ConfirmBootstrapReset `
        -ExpectedExitCode 1 `
        -ExpectedMessage "Connection string is required for non-local admin bootstrap/reset"

    $testedScenarios += Invoke-BootstrapSmokeScenario `
        -Name "dry-run-no-smoke" `
        -Password "LocalBootstrapSmokePassword12345" `
        -ConnectionString "Data Source=tmp/admin-vps-bootstrap-smoke-wrapper-regression-test/dry-run-no-smoke/local.db" `
        -LocalSqlite `
        -DryRun `
        -ExpectedExitCode 0 `
        -ExpectedMessage "Dry-run mode: admin VPS smoke was not started"

    $testedScenarios += Invoke-BootstrapSmokeScenario `
        -Name "dry-run-default-operator" `
        -Password "LocalBootstrapSmokePassword12345" `
        -ConnectionString "Data Source=tmp/admin-vps-bootstrap-smoke-wrapper-regression-test/dry-run-default-operator/local.db" `
        -LocalSqlite `
        -DryRun `
        -ExpectedExitCode 0 `
        -ExpectedMessage "Dry-run mode: admin VPS smoke was not started" `
        -OmitOperator `
        -ExpectedReadinessOperator "manual-operator"

    $testedScenarios += Invoke-BootstrapSmokeScenario `
        -Name "dry-run-default-environment" `
        -Password "LocalBootstrapSmokePassword12345" `
        -ConnectionString "Data Source=tmp/admin-vps-bootstrap-smoke-wrapper-regression-test/dry-run-default-environment/local.db" `
        -LocalSqlite `
        -DryRun `
        -ExpectedExitCode 0 `
        -ExpectedMessage "Dry-run mode: admin VPS smoke was not started" `
        -OmitEnvironmentName `
        -EnvEnvironmentName "   " `
        -ExpectedReadinessEnvironmentName "Production"

    $result = [ordered]@{
        status = "passed"
        testedScenarios = @($testedScenarios)
    }

    if ($WriteJson) {
        Write-Output ($result | ConvertTo-Json -Depth 8)
    }
    else {
        Write-Host "admin vps bootstrap smoke wrapper regression passed $($result | ConvertTo-Json -Depth 8 -Compress)"
    }
}
finally {
    if (-not $KeepArtifacts -and (Test-Path -LiteralPath $outputFullPath)) {
        Remove-Item -LiteralPath $outputFullPath -Recurse -Force
    }
}
