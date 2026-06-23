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
        [bool]$SetConnectionString = $true,
        [string]$Provider,
        [string]$ExpectedProvider,
        [string]$ApiBaseUrl = "https://api.example.test",
        [string]$ExpectedApiBaseUrl,
        [string]$AdminWebUrl = "https://admin.example.test",
        [string]$ExpectedAdminWebUrl,
        [string]$AdminEmail = "admin@example.test",
        [string]$ExpectedAdminEmail,
        [string]$AdminPasswordEnvName = "ADMIN_VPS_BOOTSTRAP_SMOKE_ADMIN_PASSWORD",
        [string]$ExpectedPasswordEnvName,
        [string]$ExpectedFailedCheckName,
        [switch]$PadReportPaths,
        [string]$ExpectedReadinessReportPath,
        [string]$ExpectedSmokeReportPath,
        [string]$ExpectedPreflightReportPath,
        [string]$ExpectedBootstrapSmokeReportPath,
        [string]$EnvironmentName = "Regression",
        [string]$ExpectedEnvironmentName,
        [bool]$UseEnvironmentNameEnv = $false,
        [string]$ProjectPath = "backend/src/VpnPlatform.Api/VpnPlatform.Api.csproj",
        [string]$FrontendPath = "frontend"
    )

    $scenarioPath = Join-Path $outputPath $Name
    New-Item -ItemType Directory -Path $scenarioPath -Force | Out-Null

    $reportPath = Join-Path $scenarioPath "admin-vps-bootstrap-smoke-readiness-report.json"
    $smokeReportPath = Join-Path $scenarioPath "admin-vps-smoke-report.json"
    $preflightReportPath = Join-Path $scenarioPath "admin-vps-smoke-preflight-report.json"
    $bootstrapSmokeReportPath = Join-Path $scenarioPath "admin-vps-bootstrap-smoke-report.json"
    $readinessReportPathArg = if ($PadReportPaths) { " $reportPath " } else { $reportPath }
    $smokeReportPathArg = if ($PadReportPaths) { " $smokeReportPath " } else { $smokeReportPath }
    $preflightReportPathArg = if ($PadReportPaths) { " $preflightReportPath " } else { $preflightReportPath }
    $bootstrapSmokeReportPathArg = if ($PadReportPaths) { " $bootstrapSmokeReportPath " } else { $bootstrapSmokeReportPath }
    $stdoutPath = Join-Path $scenarioPath "stdout.txt"
    $stderrPath = Join-Path $scenarioPath "stderr.txt"
    $password = "ReadinessPassword123!"
    $envName = $AdminPasswordEnvName.Trim()

    $previousEnv = @{}
    try {
        if ($SetPassword) {
            Set-ScopedEnv -Previous $previousEnv -Name $envName -Value $password
        }
        else {
            Set-ScopedEnv -Previous $previousEnv -Name $envName -Value $null
        }

        if ($UseEnvironmentNameEnv) {
            Set-ScopedEnv -Previous $previousEnv -Name "ADMIN_VPS_SMOKE_ENVIRONMENT" -Value $EnvironmentName
        }

        $args = @(
            "-ExecutionPolicy", "Bypass",
            "-File", (Join-Path $repoRoot "scripts/admin-vps-bootstrap-smoke-readiness.ps1"),
            "-ApiBaseUrl", $ApiBaseUrl,
            "-AdminWebUrl", $AdminWebUrl,
            "-AdminEmail", $AdminEmail,
            "-AdminPasswordEnvName", $AdminPasswordEnvName,
            "-ReadinessReportPath", $readinessReportPathArg,
            "-SmokeReportPath", $smokeReportPathArg,
            "-PreflightReportPath", $preflightReportPathArg,
            "-BootstrapSmokeReportPath", $bootstrapSmokeReportPathArg,
            "-ProjectPath", $ProjectPath,
            "-FrontendPath", $FrontendPath,
            "-Operator", "admin-vps-bootstrap-smoke-readiness-regression",
            "-ReleaseId", "readiness-regression",
            "-RequireReady"
        )

        if (-not $UseEnvironmentNameEnv) {
            $args += @("-EnvironmentName", $EnvironmentName)
        }

        if ($LocalSqlite) {
            $args += "-LocalSqlite"
        }
        elseif ($SetConnectionString) {
            $args += @("-ConnectionString", "Host=127.0.0.1;Database=vpnplatform;Username=admin;")
        }

        if (-not [string]::IsNullOrWhiteSpace($Provider)) {
            $args += @("-Provider", $Provider)
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

        if (-not [string]::IsNullOrWhiteSpace($ExpectedProvider)) {
            $report = $reportRaw | ConvertFrom-Json
            if ([string]$report.provider -ne $ExpectedProvider) {
                throw "Scenario $Name provider '$($report.provider)', expected '$ExpectedProvider'."
            }
        }

        if (-not [string]::IsNullOrWhiteSpace($ExpectedApiBaseUrl)) {
            $report = $reportRaw | ConvertFrom-Json
            if ([string]$report.apiBaseUrl -ne $ExpectedApiBaseUrl) {
                throw "Scenario $Name apiBaseUrl '$($report.apiBaseUrl)', expected '$ExpectedApiBaseUrl'."
            }
        }

        if (-not [string]::IsNullOrWhiteSpace($ExpectedAdminWebUrl)) {
            $report = $reportRaw | ConvertFrom-Json
            if ([string]$report.adminWebUrl -ne $ExpectedAdminWebUrl) {
                throw "Scenario $Name adminWebUrl '$($report.adminWebUrl)', expected '$ExpectedAdminWebUrl'."
            }
        }

        if (-not [string]::IsNullOrWhiteSpace($ExpectedAdminEmail)) {
            $report = $reportRaw | ConvertFrom-Json
            if ([string]$report.adminEmail -ne $ExpectedAdminEmail) {
                throw "Scenario $Name adminEmail '$($report.adminEmail)', expected '$ExpectedAdminEmail'."
            }
        }

        if (-not [string]::IsNullOrWhiteSpace($ExpectedEnvironmentName)) {
            $report = $reportRaw | ConvertFrom-Json
            if ([string]$report.environmentName -ne $ExpectedEnvironmentName) {
                throw "Scenario $Name environmentName '$($report.environmentName)', expected '$ExpectedEnvironmentName'."
            }
        }

        if (-not [string]::IsNullOrWhiteSpace($ExpectedPasswordEnvName)) {
            $report = $reportRaw | ConvertFrom-Json
            if ([string]$report.passwordEnvName -ne $ExpectedPasswordEnvName) {
                throw "Scenario $Name passwordEnvName '$($report.passwordEnvName)', expected '$ExpectedPasswordEnvName'."
            }
        }

        if (-not [string]::IsNullOrWhiteSpace($ExpectedFailedCheckName)) {
            $report = $reportRaw | ConvertFrom-Json
            $failedCheck = @($report.checks | Where-Object { [string]$_.name -eq $ExpectedFailedCheckName -and -not $_.passed })
            if ($failedCheck.Count -ne 1) {
                throw "Scenario $Name did not include failed check '$ExpectedFailedCheckName'."
            }
        }

        if (-not [string]::IsNullOrWhiteSpace($ExpectedReadinessReportPath)) {
            $report = $reportRaw | ConvertFrom-Json
            if ([string]$report.readinessReportPath -ne $ExpectedReadinessReportPath) {
                throw "Scenario $Name readinessReportPath '$($report.readinessReportPath)', expected '$ExpectedReadinessReportPath'."
            }
        }

        if (-not [string]::IsNullOrWhiteSpace($ExpectedSmokeReportPath)) {
            $report = $reportRaw | ConvertFrom-Json
            if ([string]$report.smokeReportPath -ne $ExpectedSmokeReportPath) {
                throw "Scenario $Name smokeReportPath '$($report.smokeReportPath)', expected '$ExpectedSmokeReportPath'."
            }
        }

        if (-not [string]::IsNullOrWhiteSpace($ExpectedPreflightReportPath)) {
            $report = $reportRaw | ConvertFrom-Json
            if ([string]$report.preflightReportPath -ne $ExpectedPreflightReportPath) {
                throw "Scenario $Name preflightReportPath '$($report.preflightReportPath)', expected '$ExpectedPreflightReportPath'."
            }
        }

        if (-not [string]::IsNullOrWhiteSpace($ExpectedBootstrapSmokeReportPath)) {
            $report = $reportRaw | ConvertFrom-Json
            if ([string]$report.bootstrapSmokeReportPath -ne $ExpectedBootstrapSmokeReportPath) {
                throw "Scenario $Name bootstrapSmokeReportPath '$($report.bootstrapSmokeReportPath)', expected '$ExpectedBootstrapSmokeReportPath'."
            }
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

function Write-JsonFile {
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [Parameter(Mandatory = $true)]$Value
    )

    [System.IO.File]::WriteAllText(
        $Path,
        ($Value | ConvertTo-Json -Depth 12),
        [System.Text.UTF8Encoding]::new($false))
}

function Invoke-ReadinessValidatorScenario {
    param(
        [Parameter(Mandatory = $true)][string]$Name,
        [Parameter(Mandatory = $true)][string]$SourceReportPath,
        [Parameter(Mandatory = $true)][string]$ExpectedMessage,
        [bool]$RequireReady = $true,
        [scriptblock]$Mutate
    )

    $scenarioPath = Join-Path $outputPath $Name
    New-Item -ItemType Directory -Path $scenarioPath -Force | Out-Null
    $reportPath = Join-Path $scenarioPath "admin-vps-bootstrap-smoke-readiness-report.json"
    $stdoutPath = Join-Path $scenarioPath "stdout.txt"
    $stderrPath = Join-Path $scenarioPath "stderr.txt"

    $report = Get-Content -LiteralPath $SourceReportPath -Raw -Encoding UTF8 | ConvertFrom-Json
    Write-JsonFile -Path $reportPath -Value $report

    if ($null -ne $Mutate) {
        & $Mutate $reportPath
    }

    $args = @(
            "-NoProfile",
            "-ExecutionPolicy", "Bypass",
            "-File", (Join-Path $repoRoot "scripts/validate-admin-vps-bootstrap-smoke-readiness-report.ps1"),
            "-ReportPath", $reportPath
        )
    if ($RequireReady) {
        $args += "-RequireReady"
    }

    $process = Start-Process -FilePath "powershell" `
        -ArgumentList $args `
        -WorkingDirectory $repoRoot `
        -RedirectStandardOutput $stdoutPath `
        -RedirectStandardError $stderrPath `
        -PassThru `
        -Wait `
        -WindowStyle Hidden

    $output = ((Get-Content -LiteralPath $stdoutPath -Raw -ErrorAction SilentlyContinue) + "`n" + (Get-Content -LiteralPath $stderrPath -Raw -ErrorAction SilentlyContinue))
    if ($process.ExitCode -ne 1) {
        throw "Scenario $Name exit code $($process.ExitCode), expected 1. Output: $output"
    }

    if ($output.IndexOf($ExpectedMessage, [System.StringComparison]::OrdinalIgnoreCase) -lt 0) {
        throw "Scenario $Name did not include expected message '$ExpectedMessage'. Output: $output"
    }

    [ordered]@{
        name = $Name
        exitCode = $process.ExitCode
        expectedMessage = $ExpectedMessage
        reportCreated = $true
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
$localReadyReportPath = Join-Path (Join-Path $outputPath "local-ready") "admin-vps-bootstrap-smoke-readiness-report.json"

$results += Invoke-ReadinessScenario `
    -Name "provider-case-normalized" `
    -ExpectedExitCode 0 `
    -ExpectedMessage "admin vps bootstrap smoke readiness report valid" `
    -ConfirmBootstrapReset $true `
    -SetConnectionString $true `
    -Provider "postgres" `
    -ExpectedProvider "Postgres"

$results += Invoke-ReadinessScenario `
    -Name "url-values-normalized" `
    -ExpectedExitCode 0 `
    -ExpectedMessage "admin vps bootstrap smoke readiness report valid" `
    -LocalSqlite $true `
    -ApiBaseUrl " https://api.example.test " `
    -ExpectedApiBaseUrl "https://api.example.test" `
    -AdminWebUrl " https://admin.example.test " `
    -ExpectedAdminWebUrl "https://admin.example.test"

$results += Invoke-ReadinessScenario `
    -Name "environment-default-normalized" `
    -ExpectedExitCode 0 `
    -ExpectedMessage "admin vps bootstrap smoke readiness report valid" `
    -LocalSqlite $true `
    -EnvironmentName " " `
    -ExpectedEnvironmentName "Production" `
    -UseEnvironmentNameEnv $true

$results += Invoke-ReadinessScenario `
    -Name "admin-email-normalized" `
    -ExpectedExitCode 0 `
    -ExpectedMessage "admin vps bootstrap smoke readiness report valid" `
    -LocalSqlite $true `
    -AdminEmail " admin@example.test " `
    -ExpectedAdminEmail "admin@example.test"

$results += Invoke-ReadinessScenario `
    -Name "password-env-name-normalized" `
    -ExpectedExitCode 0 `
    -ExpectedMessage "admin vps bootstrap smoke readiness report valid" `
    -LocalSqlite $true `
    -AdminPasswordEnvName " ADMIN_VPS_BOOTSTRAP_SMOKE_ADMIN_PASSWORD " `
    -ExpectedPasswordEnvName "ADMIN_VPS_BOOTSTRAP_SMOKE_ADMIN_PASSWORD"

$results += Invoke-ReadinessScenario `
    -Name "bad-password-env-name" `
    -ExpectedExitCode 1 `
    -ExpectedMessage "passwordEnvName must be a safe environment variable name containing" `
    -LocalSqlite $true `
    -AdminPasswordEnvName "ADMIN-VPS-PASSWORD" `
    -ExpectedFailedCheckName "password-env-name-safe"

$reportPathsScenarioPath = Join-Path $outputPath "report-paths-normalized"
$results += Invoke-ReadinessScenario `
    -Name "report-paths-normalized" `
    -ExpectedExitCode 0 `
    -ExpectedMessage "admin vps bootstrap smoke readiness report valid" `
    -LocalSqlite $true `
    -PadReportPaths `
    -ExpectedReadinessReportPath (Join-Path $reportPathsScenarioPath "admin-vps-bootstrap-smoke-readiness-report.json") `
    -ExpectedSmokeReportPath (Join-Path $reportPathsScenarioPath "admin-vps-smoke-report.json") `
    -ExpectedPreflightReportPath (Join-Path $reportPathsScenarioPath "admin-vps-smoke-preflight-report.json") `
    -ExpectedBootstrapSmokeReportPath (Join-Path $reportPathsScenarioPath "admin-vps-bootstrap-smoke-report.json")

$results += Invoke-ReadinessScenario `
    -Name "workspace-paths-normalized" `
    -ExpectedExitCode 0 `
    -ExpectedMessage "admin vps bootstrap smoke readiness report valid" `
    -LocalSqlite $true `
    -ProjectPath " backend/src/VpnPlatform.Api/VpnPlatform.Api.csproj " `
    -FrontendPath " frontend "

$results += Invoke-ReadinessValidatorScenario `
    -Name "mismatched-readiness-report-self-link" `
    -SourceReportPath $localReadyReportPath `
    -ExpectedMessage "mismatch for readinessReportPath" `
    -Mutate {
        param($reportPath)
        $report = Get-Content -LiteralPath $reportPath -Raw -Encoding UTF8 | ConvertFrom-Json
        $report.readinessReportPath = Join-Path (Split-Path -Parent $reportPath) "other-readiness-report.json"
        Write-JsonFile -Path $reportPath -Value $report
    }

$results += Invoke-ReadinessValidatorScenario `
    -Name "mismatched-readiness-ready-flag" `
    -SourceReportPath $localReadyReportPath `
    -ExpectedMessage "readyForBootstrapSmoke must match checks" `
    -RequireReady $false `
    -Mutate {
        param($reportPath)
        $report = Get-Content -LiteralPath $reportPath -Raw -Encoding UTF8 | ConvertFrom-Json
        $report.checks[0].passed = $false
        Write-JsonFile -Path $reportPath -Value $report
    }

$results += Invoke-ReadinessValidatorScenario `
    -Name "mismatched-readiness-local-provider" `
    -SourceReportPath $localReadyReportPath `
    -ExpectedMessage "provider must be Sqlite when localSqlite is true" `
    -Mutate {
        param($reportPath)
        $report = Get-Content -LiteralPath $reportPath -Raw -Encoding UTF8 | ConvertFrom-Json
        $report.provider = "Postgres"
        Write-JsonFile -Path $reportPath -Value $report
    }

$results += Invoke-ReadinessValidatorScenario `
    -Name "mismatched-readiness-password-env-name-safe" `
    -SourceReportPath $localReadyReportPath `
    -ExpectedMessage "passwordEnvName must be a safe environment variable name containing" `
    -Mutate {
        param($reportPath)
        $report = Get-Content -LiteralPath $reportPath -Raw -Encoding UTF8 | ConvertFrom-Json
        $report.passwordEnvName = "Path"
        Write-JsonFile -Path $reportPath -Value $report
    }

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
