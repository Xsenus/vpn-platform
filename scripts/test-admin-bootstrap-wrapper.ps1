param(
    [string]$OutputDirectory = "tmp/admin-bootstrap-wrapper-regression-test",
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

function Invoke-AdminBootstrapScenario {
    param(
        [Parameter(Mandatory = $true)][string]$Name,
        [string[]]$AdditionalArguments = @(),
        [Parameter(Mandatory = $true)][int]$ExpectedExitCode,
        [Parameter(Mandatory = $true)][string]$ExpectedMessage,
        [string]$ExpectedOutputContains = "",
        [string]$ExpectedOutputNotContains = ""
    )

    $scenarioPath = Join-Path $outputFullPath $Name
    New-Item -ItemType Directory -Path $scenarioPath -Force | Out-Null

    $stdoutPath = Join-Path $scenarioPath "stdout.log"
    $stderrPath = Join-Path $scenarioPath "stderr.log"
    $wrapperPath = Join-Path $repoRoot "scripts/admin-bootstrap.ps1"
    $password = "AdminBootstrapWrapperPassword12345"

    $arguments = @(
        "-NoProfile",
        "-ExecutionPolicy", "Bypass",
        "-File", $wrapperPath,
        "-EnvironmentName", "Local",
        "-Email", "direct-bootstrap-admin@example.test",
        "-Password", $password,
        "-ProjectPath", "backend/src/VpnPlatform.Api/VpnPlatform.Api.csproj",
        "-DryRun"
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

    if ($process.ExitCode -ne $ExpectedExitCode) {
        throw "Expected scenario '$Name' exit code $ExpectedExitCode, got $($process.ExitCode). Output: $output"
    }

    if ($output.IndexOf($ExpectedMessage, [System.StringComparison]::OrdinalIgnoreCase) -lt 0) {
        throw "Expected scenario '$Name' output to contain '$ExpectedMessage'. Actual output: $output"
    }

    if (-not [string]::IsNullOrWhiteSpace($ExpectedOutputContains) -and $output.IndexOf($ExpectedOutputContains, [System.StringComparison]::OrdinalIgnoreCase) -lt 0) {
        throw "Expected scenario '$Name' output to contain '$ExpectedOutputContains'. Actual output: $output"
    }

    if (-not [string]::IsNullOrWhiteSpace($ExpectedOutputNotContains) -and $output.IndexOf($ExpectedOutputNotContains, [System.StringComparison]::OrdinalIgnoreCase) -ge 0) {
        throw "Expected scenario '$Name' output not to contain '$ExpectedOutputNotContains'. Actual output: $output"
    }

    if ($output.Contains($password)) {
        throw "Admin bootstrap wrapper leaked password in scenario '$Name'."
    }

    return [ordered]@{
        name = $Name
        exitCode = $process.ExitCode
        expectedMessage = $ExpectedMessage
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
    $testedScenarios += Invoke-AdminBootstrapScenario `
        -Name "provider-case-normalized" `
        -AdditionalArguments @("-Provider", "postgres") `
        -ExpectedExitCode 0 `
        -ExpectedMessage "Dry-run mode: database was not changed" `
        -ExpectedOutputContains "Provider: Postgres"

    $testedScenarios += Invoke-AdminBootstrapScenario `
        -Name "local-sqlite-overrides-provider" `
        -AdditionalArguments @("-LocalSqlite", "-Provider", "Mongo") `
        -ExpectedExitCode 0 `
        -ExpectedMessage "Dry-run mode: database was not changed" `
        -ExpectedOutputContains "Provider: Sqlite"

    $testedScenarios += Invoke-AdminBootstrapScenario `
        -Name "bad-provider" `
        -AdditionalArguments @("-Provider", "Mongo") `
        -ExpectedExitCode 1 `
        -ExpectedMessage "Provider must be Postgres or Sqlite" `
        -ExpectedOutputNotContains "Admin bootstrap/reset is ready to run."

    $result = [ordered]@{
        status = "passed"
        testedScenarios = @($testedScenarios)
    }

    if ($WriteJson) {
        Write-Output ($result | ConvertTo-Json -Depth 8)
    }
    else {
        Write-Host "admin bootstrap wrapper regression passed $($result | ConvertTo-Json -Depth 8 -Compress)"
    }
}
finally {
    if (-not $KeepArtifacts -and (Test-Path -LiteralPath $outputFullPath)) {
        Remove-Item -LiteralPath $outputFullPath -Recurse -Force
    }
}
