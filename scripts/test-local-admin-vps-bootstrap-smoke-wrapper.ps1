param(
    [string]$OutputDirectory = "tmp/local-admin-vps-bootstrap-smoke-wrapper-regression-test",
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

$outputFullPath = Resolve-WorkspacePath $OutputDirectory
$localSmokeTmp = Resolve-WorkspacePath "tmp/local-admin-vps-bootstrap-smoke"
Assert-InWorkspace $outputFullPath
Assert-InWorkspace $localSmokeTmp

if (Test-Path -LiteralPath $outputFullPath) {
    Remove-Item -LiteralPath $outputFullPath -Recurse -Force
}

if (Test-Path -LiteralPath $localSmokeTmp) {
    Remove-Item -LiteralPath $localSmokeTmp -Recurse -Force
}

function Invoke-LocalWrapperFailure {
    param(
        [Parameter(Mandatory = $true)][string]$Name,
        [Parameter(Mandatory = $true)][AllowEmptyCollection()][string[]]$AdditionalArguments,
        [AllowNull()][string]$EnvMaxEvidenceChainMinutes,
        [Parameter(Mandatory = $true)][string]$ExpectedMessage
    )

    $scenarioPath = Join-Path $outputFullPath $Name
    New-Item -ItemType Directory -Path $scenarioPath -Force | Out-Null

    $stdoutPath = Join-Path $scenarioPath "stdout.log"
    $stderrPath = Join-Path $scenarioPath "stderr.log"
    $wrapperPath = Join-Path $repoRoot "scripts/local-admin-vps-bootstrap-smoke.ps1"
    $previousMaxEvidenceChainMinutes = [Environment]::GetEnvironmentVariable("ADMIN_VPS_SMOKE_MAX_EVIDENCE_CHAIN_MINUTES", "Process")

    try {
        [Environment]::SetEnvironmentVariable("ADMIN_VPS_SMOKE_MAX_EVIDENCE_CHAIN_MINUTES", $EnvMaxEvidenceChainMinutes, "Process")

        if (Test-Path -LiteralPath $localSmokeTmp) {
            Remove-Item -LiteralPath $localSmokeTmp -Recurse -Force
        }

        $arguments = @(
            "-NoProfile",
            "-ExecutionPolicy", "Bypass",
            "-File", $wrapperPath,
            "-ApiPort", "18231",
            "-AdminPort", "18235",
            "-KeepArtifacts"
        ) + $AdditionalArguments

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
            throw "Expected local admin VPS bootstrap smoke wrapper to fail for scenario '$Name'."
        }

        if ($output.IndexOf($ExpectedMessage, [System.StringComparison]::OrdinalIgnoreCase) -lt 0) {
            throw "Expected bad MaxEvidenceChainMinutes output for scenario '$Name'. Actual output: $output"
        }

        foreach ($forbiddenOutput in @("Admin bootstrap/reset is ready to run.", "Admin VPS bootstrap+smoke flow is ready to run.", "Admin VPS browser smoke is ready to run.", "e2e:admin-vps-smoke", "local admin vps bootstrap smoke ok")) {
            if ($output.IndexOf($forbiddenOutput, [System.StringComparison]::OrdinalIgnoreCase) -ge 0) {
                throw "Local admin smoke appears to have started before MaxEvidenceChainMinutes validation in scenario '$Name'."
            }
        }

        if (Test-Path -LiteralPath $localSmokeTmp) {
            throw "Local admin smoke artifacts should not exist after MaxEvidenceChainMinutes validation failure in scenario '$Name': $localSmokeTmp"
        }

        return [ordered]@{
            name = $Name
            exitCode = $process.ExitCode
            expectedMessage = $ExpectedMessage
            localSmokeArtifactsCreated = $false
        }
    }
    finally {
        [Environment]::SetEnvironmentVariable("ADMIN_VPS_SMOKE_MAX_EVIDENCE_CHAIN_MINUTES", $previousMaxEvidenceChainMinutes, "Process")
    }
}

New-Item -ItemType Directory -Path $outputFullPath -Force | Out-Null

try {
    $testedFailures = @()
    $testedFailures += Invoke-LocalWrapperFailure `
        -Name "bad-max-evidence-chain-minutes" `
        -AdditionalArguments @("-MaxEvidenceChainMinutes", "0") `
        -EnvMaxEvidenceChainMinutes $null `
        -ExpectedMessage "MaxEvidenceChainMinutes must be greater than 0"

    $testedFailures += Invoke-LocalWrapperFailure `
        -Name "bad-env-max-evidence-chain-minutes" `
        -AdditionalArguments @() `
        -EnvMaxEvidenceChainMinutes "0" `
        -ExpectedMessage "MaxEvidenceChainMinutes must be greater than 0"

    $testedFailures += Invoke-LocalWrapperFailure `
        -Name "too-high-max-evidence-chain-minutes" `
        -AdditionalArguments @("-MaxEvidenceChainMinutes", "1441") `
        -EnvMaxEvidenceChainMinutes $null `
        -ExpectedMessage "MaxEvidenceChainMinutes must be less than or equal to 1440"

    $testedFailures += Invoke-LocalWrapperFailure `
        -Name "too-high-env-max-evidence-chain-minutes" `
        -AdditionalArguments @() `
        -EnvMaxEvidenceChainMinutes "1441" `
        -ExpectedMessage "MaxEvidenceChainMinutes must be less than or equal to 1440"

    $result = [ordered]@{
        status = "passed"
        testedFailures = @($testedFailures)
    }

    if ($WriteJson) {
        Write-Output ($result | ConvertTo-Json -Depth 8)
    }
    else {
        Write-Host "local admin vps bootstrap smoke wrapper regression passed $($result | ConvertTo-Json -Depth 8 -Compress)"
    }
}
finally {
    if (Test-Path -LiteralPath $localSmokeTmp) {
        Remove-Item -LiteralPath $localSmokeTmp -Recurse -Force
    }

    if (-not $KeepArtifacts -and (Test-Path -LiteralPath $outputFullPath)) {
        Remove-Item -LiteralPath $outputFullPath -Recurse -Force
    }
}
