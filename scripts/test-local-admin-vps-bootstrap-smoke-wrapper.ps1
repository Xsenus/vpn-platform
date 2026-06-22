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

New-Item -ItemType Directory -Path $outputFullPath -Force | Out-Null

try {
    $stdoutPath = Join-Path $outputFullPath "stdout.log"
    $stderrPath = Join-Path $outputFullPath "stderr.log"
    $wrapperPath = Join-Path $repoRoot "scripts/local-admin-vps-bootstrap-smoke.ps1"

    $process = Start-Process -FilePath "powershell" `
        -ArgumentList @(
            "-NoProfile",
            "-ExecutionPolicy", "Bypass",
            "-File", $wrapperPath,
            "-ApiPort", "18231",
            "-AdminPort", "18235",
            "-MaxEvidenceChainMinutes", "0",
            "-KeepArtifacts"
        ) `
        -WorkingDirectory $repoRoot `
        -RedirectStandardOutput $stdoutPath `
        -RedirectStandardError $stderrPath `
        -PassThru `
        -Wait `
        -WindowStyle Hidden

    $output = ((Get-Content -LiteralPath $stdoutPath -Raw -ErrorAction SilentlyContinue) + "`n" + (Get-Content -LiteralPath $stderrPath -Raw -ErrorAction SilentlyContinue))

    if ($process.ExitCode -eq 0) {
        throw "Expected local admin VPS bootstrap smoke wrapper to fail for bad MaxEvidenceChainMinutes."
    }

    if ($output.IndexOf("MaxEvidenceChainMinutes must be greater than 0", [System.StringComparison]::OrdinalIgnoreCase) -lt 0) {
        throw "Expected bad MaxEvidenceChainMinutes output. Actual output: $output"
    }

    foreach ($forbiddenOutput in @("Admin bootstrap/reset is ready to run.", "Admin VPS bootstrap+smoke flow is ready to run.", "Admin VPS browser smoke is ready to run.", "e2e:admin-vps-smoke", "local admin vps bootstrap smoke ok")) {
        if ($output.IndexOf($forbiddenOutput, [System.StringComparison]::OrdinalIgnoreCase) -ge 0) {
            throw "Local admin smoke appears to have started before MaxEvidenceChainMinutes validation."
        }
    }

    if (Test-Path -LiteralPath $localSmokeTmp) {
        throw "Local admin smoke artifacts should not exist after MaxEvidenceChainMinutes validation failure: $localSmokeTmp"
    }

    $result = [ordered]@{
        status = "passed"
        testedFailures = @(
            [ordered]@{
                name = "bad-max-evidence-chain-minutes"
                exitCode = $process.ExitCode
                expectedMessage = "MaxEvidenceChainMinutes must be greater than 0"
                localSmokeArtifactsCreated = $false
            }
        )
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
