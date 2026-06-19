param(
    [string]$OutputDirectory = "tmp/admin-vps-bootstrap-smoke-readiness-regression-test"
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$outputPath = if ([System.IO.Path]::IsPathRooted($OutputDirectory)) {
    [System.IO.Path]::GetFullPath($OutputDirectory)
}
else {
    [System.IO.Path]::GetFullPath((Join-Path $repoRoot $OutputDirectory))
}

function Assert-InWorkspace {
    param([Parameter(Mandatory = $true)][string]$Path)

    $rootFullPath = [System.IO.Path]::GetFullPath($repoRoot)
    $targetFullPath = [System.IO.Path]::GetFullPath($Path)
    if (-not $targetFullPath.StartsWith($rootFullPath, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "Refusing to write outside workspace: $targetFullPath"
    }
}

function Set-ScopedEnv {
    param(
        [Parameter(Mandatory = $true)][hashtable]$Previous,
        [Parameter(Mandatory = $true)][string]$Name,
        [AllowNull()][string]$Value
    )

    if (-not $Previous.ContainsKey($Name)) {
        $Previous[$Name] = [Environment]::GetEnvironmentVariable($Name, "Process")
    }

    [Environment]::SetEnvironmentVariable($Name, $Value, "Process")
    if ($null -eq $Value) {
        Remove-Item -LiteralPath "Env:\$Name" -ErrorAction SilentlyContinue
    }
    else {
        Set-Item -LiteralPath "Env:\$Name" -Value $Value
    }
}

function Invoke-ReadinessScenario {
    param(
        [Parameter(Mandatory = $true)][string]$Name,
        [Parameter(Mandatory = $true)][int]$ExpectedExitCode,
        [Parameter(Mandatory = $true)][string]$ExpectedMessage,
        [bool]$SetPassword = $true,
        [bool]$LocalSqlite = $false,
        [bool]$ConfirmBootstrapReset = $false,
        [bool]$SetConnectionString = $true
    )

    $scenarioPath = Join-Path $outputPath $Name
    New-Item -ItemType Directory -Path $scenarioPath -Force | Out-Null

    $reportPath = Join-Path $scenarioPath "admin-vps-bootstrap-smoke-readiness-report.json"
    $stdoutPath = Join-Path $scenarioPath "stdout.txt"
    $stderrPath = Join-Path $scenarioPath "stderr.txt"
    $password = "ReadinessPassword123!"
    $envName = "ADMIN_VPS_BOOTSTRAP_SMOKE_ADMIN_PASSWORD"

    $previousEnv = @{}
    try {
        if ($SetPassword) {
            Set-ScopedEnv -Previous $previousEnv -Name $envName -Value $password
        }
        else {
            Set-ScopedEnv -Previous $previousEnv -Name $envName -Value $null
        }

        $args = @(
            "-ExecutionPolicy", "Bypass",
            "-File", (Join-Path $repoRoot "scripts/admin-vps-bootstrap-smoke-readiness.ps1"),
            "-ApiBaseUrl", "https://api.example.test",
            "-AdminWebUrl", "https://admin.example.test",
            "-AdminEmail", "admin@example.test",
            "-AdminPasswordEnvName", $envName,
            "-ReadinessReportPath", $reportPath,
            "-SmokeReportPath", (Join-Path $scenarioPath "admin-vps-smoke-report.json"),
            "-PreflightReportPath", (Join-Path $scenarioPath "admin-vps-smoke-preflight-report.json"),
            "-BootstrapSmokeReportPath", (Join-Path $scenarioPath "admin-vps-bootstrap-smoke-report.json"),
            "-EnvironmentName", "Regression",
            "-Operator", "admin-vps-bootstrap-smoke-readiness-regression",
            "-ReleaseId", "readiness-regression",
            "-RequireReady"
        )

        if ($LocalSqlite) {
            $args += "-LocalSqlite"
        }
        elseif ($SetConnectionString) {
            $args += @("-ConnectionString", "Host=127.0.0.1;Database=vpnplatform;Username=admin;")
        }

        if ($ConfirmBootstrapReset) {
            $args += "-ConfirmBootstrapReset"
        }

        $process = Start-Process -FilePath "powershell" `
            -ArgumentList $args `
            -NoNewWindow `
            -Wait `
            -PassThru `
            -RedirectStandardOutput $stdoutPath `
            -RedirectStandardError $stderrPath

        $stdout = if (Test-Path -LiteralPath $stdoutPath -PathType Leaf) { Get-Content -LiteralPath $stdoutPath -Raw -Encoding UTF8 } else { "" }
        $stderr = if (Test-Path -LiteralPath $stderrPath -PathType Leaf) { Get-Content -LiteralPath $stderrPath -Raw -Encoding UTF8 } else { "" }
        $combined = "$stdout`n$stderr"

        if ($process.ExitCode -ne $ExpectedExitCode) {
            throw "Scenario $Name exit code $($process.ExitCode), expected $ExpectedExitCode. Output: $combined"
        }

        if (-not $combined.Contains($ExpectedMessage)) {
            throw "Scenario $Name did not include expected message '$ExpectedMessage'. Output: $combined"
        }

        if ($combined.Contains($password)) {
            throw "Scenario $Name leaked password to process output."
        }

        if (-not (Test-Path -LiteralPath $reportPath -PathType Leaf)) {
            throw "Scenario $Name did not create readiness report."
        }

        $reportRaw = Get-Content -LiteralPath $reportPath -Raw -Encoding UTF8
        if ($reportRaw.Contains($password)) {
            throw "Scenario $Name leaked password to readiness report."
        }

        if ($reportRaw.Contains("Host=127.0.0.1")) {
            throw "Scenario $Name leaked connection string to readiness report."
        }

        $bytes = [System.IO.File]::ReadAllBytes($reportPath)
        if ($bytes.Length -ge 3 -and $bytes[0] -eq 0xEF -and $bytes[1] -eq 0xBB -and $bytes[2] -eq 0xBF) {
            throw "Scenario $Name readiness report has UTF-8 BOM."
        }

        return [ordered]@{
            name = $Name
            exitCode = $process.ExitCode
            expectedMessage = $ExpectedMessage
            reportCreated = $true
        }
    }
    finally {
        foreach ($entry in $previousEnv.GetEnumerator()) {
            [Environment]::SetEnvironmentVariable($entry.Key, $entry.Value, "Process")
            if ($null -eq $entry.Value) {
                Remove-Item -LiteralPath "Env:\$($entry.Key)" -ErrorAction SilentlyContinue
            }
            else {
                Set-Item -LiteralPath "Env:\$($entry.Key)" -Value $entry.Value
            }
        }
    }
}

Assert-InWorkspace $outputPath
if (Test-Path -LiteralPath $outputPath -PathType Container) {
    Remove-Item -LiteralPath $outputPath -Recurse -Force
}
New-Item -ItemType Directory -Path $outputPath -Force | Out-Null

$results = @()
$results += Invoke-ReadinessScenario `
    -Name "local-ready" `
    -ExpectedExitCode 0 `
    -ExpectedMessage "admin vps bootstrap smoke readiness report valid" `
    -LocalSqlite $true

$results += Invoke-ReadinessScenario `
    -Name "missing-password" `
    -ExpectedExitCode 1 `
    -ExpectedMessage "passwordEnvPresent must be true" `
    -SetPassword $false `
    -LocalSqlite $true

$results += Invoke-ReadinessScenario `
    -Name "missing-confirm-bootstrap-reset" `
    -ExpectedExitCode 1 `
    -ExpectedMessage "confirmBootstrapReset must be true" `
    -ConfirmBootstrapReset $false `
    -SetConnectionString $true

$results += Invoke-ReadinessScenario `
    -Name "missing-connection-string" `
    -ExpectedExitCode 1 `
    -ExpectedMessage "connectionStringPresent must be true" `
    -ConfirmBootstrapReset $true `
    -SetConnectionString $false

Write-Host "admin vps bootstrap smoke readiness regression passed $($results | ConvertTo-Json -Compress)"
