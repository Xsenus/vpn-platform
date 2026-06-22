param(
    [string]$OutputDirectory = "tmp/admin-vps-bootstrap-smoke-evidence-validator-test"
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

function Write-JsonFile {
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [Parameter(Mandatory = $true)]$Value
    )

    $parent = Split-Path -Parent $Path
    if (-not (Test-Path -LiteralPath $parent -PathType Container)) {
        New-Item -ItemType Directory -Path $parent -Force | Out-Null
    }

    [System.IO.File]::WriteAllText(
        $Path,
        ($Value | ConvertTo-Json -Depth 12),
        [System.Text.UTF8Encoding]::new($false))
}

function New-ReadinessReport {
    param(
        [string]$Path,
        [string]$SmokeReportPath,
        [string]$PreflightReportPath,
        [string]$BootstrapReportPath,
        [DateTimeOffset]$GeneratedAt,
        [bool]$Ready = $true
    )

    $checks = @(
        "api-base-url",
        "admin-web-url",
        "admin-email",
        "password-env-name",
        "password-env-present",
        "password-length",
        "provider-supported",
        "local-or-confirm-reset",
        "connection-string",
        "project-file",
        "frontend-directory",
        "package-command",
        "bootstrap-script",
        "smoke-wrapper",
        "readiness-validator",
        "bootstrap-report-validator"
    ) | ForEach-Object {
        [ordered]@{
            name = $_
            passed = $Ready
            message = "Synthetic check for bootstrap smoke evidence validator."
        }
    }

    [ordered]@{
        reportId = "admin-vps-bootstrap-smoke-readiness-test"
        generatedAt = $GeneratedAt.ToString("o")
        environmentName = "Local"
        operator = "bootstrap-smoke-evidence-validator-test"
        releaseId = "bootstrap-smoke-evidence-validator-test"
        apiBaseUrl = "http://127.0.0.1:18211"
        adminWebUrl = "http://127.0.0.1:18215"
        adminEmail = "admin@example.test"
        provider = "Sqlite"
        localSqlite = $true
        applyMigrations = $false
        confirmBootstrapReset = $false
        connectionStringPresent = $true
        passwordEnvName = "ADMIN_VPS_BOOTSTRAP_SMOKE_ADMIN_PASSWORD"
        passwordEnvPresent = $true
        passwordLengthOk = $true
        smokeReportPath = $SmokeReportPath
        preflightReportPath = $PreflightReportPath
        bootstrapSmokeReportPath = $BootstrapReportPath
        readinessReportPath = $Path
        readyForBootstrapSmoke = $Ready
        checks = @($checks)
    } | ForEach-Object {
        Write-JsonFile -Path $Path -Value $_
    }
}

function New-PreflightReport {
    param(
        [string]$Path,
        [string]$SmokeReportPath,
        [DateTimeOffset]$GeneratedAt
    )

    $checks = @(
        "api-base-url",
        "admin-web-url",
        "admin-email",
        "password-env-present",
        "frontend-directory",
        "package-command",
        "browser-runner",
        "report-validator",
        "preflight-validator"
    ) | ForEach-Object {
        [ordered]@{
            name = $_
            passed = $true
            message = "Synthetic check for bootstrap smoke evidence validator."
        }
    }

    Write-JsonFile -Path $Path -Value ([ordered]@{
        reportId = "admin-vps-smoke-preflight-test"
        generatedAt = $GeneratedAt.ToString("o")
        environmentName = "Local"
        operator = "bootstrap-smoke-evidence-validator-test"
        releaseId = "bootstrap-smoke-evidence-validator-test"
        apiBaseUrl = "http://127.0.0.1:18211"
        adminWebUrl = "http://127.0.0.1:18215"
        adminEmail = "admin@example.test"
        smokeReportPath = $SmokeReportPath
        preflightReportPath = [System.IO.Path]::GetFullPath($Path)
        passwordEnvPresent = $true
        readyForLiveSmoke = $true
        checks = @($checks)
    })
}

function New-SmokeReport {
    param(
        [string]$Path,
        [DateTimeOffset]$GeneratedAt
    )

    $sections = @(
        "dashboard", "users", "payments", "tariffs", "subscriptions", "vpn",
        "nodes", "panels", "support", "bot", "releases", "faq", "content",
        "scenarios", "provisioning", "audit"
    ) | ForEach-Object {
        [ordered]@{
            id = $_
            route = "/admin/#$_"
            status = "passed"
            loaded = $true
            httpStatus = 200
            evidence = "Synthetic passed evidence for $_ section."
        }
    }

    Write-JsonFile -Path $Path -Value ([ordered]@{
        reportId = "admin-vps-smoke-test"
        startedAt = $GeneratedAt.ToString("o")
        completedAt = $GeneratedAt.AddMinutes(1).ToString("o")
        environmentName = "Local"
        operator = "bootstrap-smoke-evidence-validator-test"
        releaseId = "bootstrap-smoke-evidence-validator-test"
        apiBaseUrl = "http://127.0.0.1:18211"
        adminWebUrl = "http://127.0.0.1:18215"
        adminEmail = "admin@example.test"
        smokeReportPath = [System.IO.Path]::GetFullPath($Path)
        notes = "Synthetic real evidence for bootstrap smoke evidence validator."
        adminLoginPassed = $true
        accountBootstrapChecked = $true
        passwordEnvPresent = $true
        noJsErrors = $true
        noUnauthorizedAfterLogin = $true
        sections = @($sections)
    })
}

function New-BootstrapReport {
    param(
        [string]$Path,
        [string]$SmokeReportPath,
        [string]$PreflightReportPath,
        [string]$ReadinessReportPath,
        [string]$BootstrapReportPath,
        [DateTimeOffset]$GeneratedAt,
        [string]$AdminWebUrl = "http://127.0.0.1:18215"
    )

    Write-JsonFile -Path $Path -Value ([ordered]@{
        reportId = "admin-vps-bootstrap-smoke-test"
        environmentName = "Local"
        apiBaseUrl = "http://127.0.0.1:18211"
        adminWebUrl = $AdminWebUrl
        adminEmail = "admin@example.test"
        provider = "Sqlite"
        bootstrapResetConfirmed = $false
        localSqlite = $true
        dryRun = $false
        accountBootstrapChecked = $true
        passwordEnvName = "ADMIN_VPS_BOOTSTRAP_SMOKE_ADMIN_PASSWORD"
        passwordEnvPresent = $true
        smokeReportPath = $SmokeReportPath
        preflightReportPath = $PreflightReportPath
        readinessReportPath = $ReadinessReportPath
        bootstrapSmokeReportPath = $BootstrapReportPath
        generatedAt = $GeneratedAt.ToString("o")
        completedAt = $GeneratedAt.AddMinutes(2).ToString("o")
        releaseId = "bootstrap-smoke-evidence-validator-test"
        operator = "bootstrap-smoke-evidence-validator-test"
        status = "passed"
        notes = "Synthetic sanitized bootstrap+smoke evidence without credentials."
    })
}

function Invoke-ValidatorScenario {
    param(
        [Parameter(Mandatory = $true)][string]$Name,
        [Parameter(Mandatory = $true)][int]$ExpectedExitCode,
        [Parameter(Mandatory = $true)][string]$ExpectedMessage,
        [string[]]$AdditionalExpectedMessages = @(),
        [scriptblock]$Mutate
    )

    $scenarioPath = Join-Path $outputPath $Name
    New-Item -ItemType Directory -Path $scenarioPath -Force | Out-Null
    $readinessPath = Join-Path $scenarioPath "admin-vps-bootstrap-smoke-readiness-report.json"
    $bootstrapPath = Join-Path $scenarioPath "admin-vps-bootstrap-smoke-report.json"
    $preflightPath = Join-Path $scenarioPath "admin-vps-smoke-preflight-report.json"
    $smokePath = Join-Path $scenarioPath "admin-vps-smoke-report.json"
    $stdoutPath = Join-Path $scenarioPath "stdout.txt"
    $stderrPath = Join-Path $scenarioPath "stderr.txt"
    $baseAt = [DateTimeOffset]::UtcNow.AddMinutes(-10)

    New-ReadinessReport -Path $readinessPath -SmokeReportPath $smokePath -PreflightReportPath $preflightPath -BootstrapReportPath $bootstrapPath -GeneratedAt $baseAt
    New-PreflightReport -Path $preflightPath -SmokeReportPath $smokePath -GeneratedAt $baseAt.AddMinutes(1)
    New-SmokeReport -Path $smokePath -GeneratedAt $baseAt.AddMinutes(2)
    New-BootstrapReport -Path $bootstrapPath -SmokeReportPath $smokePath -PreflightReportPath $preflightPath -ReadinessReportPath $readinessPath -BootstrapReportPath $bootstrapPath -GeneratedAt $baseAt.AddMinutes(3)

    if ($null -ne $Mutate) {
        & $Mutate $readinessPath $bootstrapPath $preflightPath $smokePath
    }

    $process = Start-Process -FilePath "powershell" `
        -ArgumentList @(
            "-NoProfile",
            "-ExecutionPolicy", "Bypass",
            "-File", (Join-Path $repoRoot "scripts/validate-admin-vps-bootstrap-smoke-evidence.ps1"),
            "-ReadinessReportPath", $readinessPath,
            "-BootstrapSmokeReportPath", $bootstrapPath
        ) `
        -WorkingDirectory $repoRoot `
        -RedirectStandardOutput $stdoutPath `
        -RedirectStandardError $stderrPath `
        -PassThru `
        -Wait `
        -WindowStyle Hidden

    $output = ((Get-Content -LiteralPath $stdoutPath -Raw -ErrorAction SilentlyContinue) + "`n" + (Get-Content -LiteralPath $stderrPath -Raw -ErrorAction SilentlyContinue))
    if ($process.ExitCode -ne $ExpectedExitCode) {
        throw "Scenario $Name exit code $($process.ExitCode), expected $ExpectedExitCode. Output: $output"
    }

    if ($output.IndexOf($ExpectedMessage, [System.StringComparison]::OrdinalIgnoreCase) -lt 0) {
        throw "Scenario $Name did not include expected message '$ExpectedMessage'. Output: $output"
    }

    foreach ($additionalExpectedMessage in $AdditionalExpectedMessages) {
        if ($output.IndexOf($additionalExpectedMessage, [System.StringComparison]::OrdinalIgnoreCase) -lt 0) {
            throw "Scenario $Name did not include expected message '$additionalExpectedMessage'. Output: $output"
        }
    }

    [ordered]@{
        name = $Name
        exitCode = $process.ExitCode
        expectedMessage = $ExpectedMessage
        additionalExpectedMessages = $AdditionalExpectedMessages
    }
}

Assert-InWorkspace $outputPath
if (Test-Path -LiteralPath $outputPath -PathType Container) {
    Remove-Item -LiteralPath $outputPath -Recurse -Force
}
New-Item -ItemType Directory -Path $outputPath -Force | Out-Null

$results = @()
$results += Invoke-ValidatorScenario -Name "valid" -ExpectedExitCode 0 -ExpectedMessage "admin vps bootstrap smoke evidence valid" -AdditionalExpectedMessages @("apiBaseUrl", "adminWebUrl", "adminEmail", "operator", "readyForBootstrapSmoke", "bootstrapStatus", "preflightReportPath", "sectionsContractPath")
$results += Invoke-ValidatorScenario -Name "mismatched-admin-url" -ExpectedExitCode 1 -ExpectedMessage "mismatch for readiness adminWebUrl" -Mutate {
    param($readinessPath, $bootstrapPath)
    $report = Get-Content -LiteralPath $bootstrapPath -Raw -Encoding UTF8 | ConvertFrom-Json
    $report.adminWebUrl = "http://127.0.0.1:19000"
    Write-JsonFile -Path $bootstrapPath -Value $report
}
$results += Invoke-ValidatorScenario -Name "readiness-not-ready" -ExpectedExitCode 1 -ExpectedMessage "readyForBootstrapSmoke must be true" -Mutate {
    param($readinessPath)
    $report = Get-Content -LiteralPath $readinessPath -Raw -Encoding UTF8 | ConvertFrom-Json
    $report.readyForBootstrapSmoke = $false
    Write-JsonFile -Path $readinessPath -Value $report
}
$results += Invoke-ValidatorScenario -Name "mismatched-release-id" -ExpectedExitCode 1 -ExpectedMessage "mismatch for readiness releaseId" -Mutate {
    param($readinessPath, $bootstrapPath)
    $report = Get-Content -LiteralPath $readinessPath -Raw -Encoding UTF8 | ConvertFrom-Json
    $report.releaseId = "bootstrap-smoke-evidence-validator-other-release"
    Write-JsonFile -Path $readinessPath -Value $report
}
$results += Invoke-ValidatorScenario -Name "mismatched-readiness-report-path" -ExpectedExitCode 1 -ExpectedMessage "mismatch for readinessReportPath" -Mutate {
    param($readinessPath, $bootstrapPath)
    $report = Get-Content -LiteralPath $readinessPath -Raw -Encoding UTF8 | ConvertFrom-Json
    $report.readinessReportPath = Join-Path (Split-Path -Parent $readinessPath) "other-readiness-report.json"
    Write-JsonFile -Path $readinessPath -Value $report
}
$results += Invoke-ValidatorScenario -Name "missing-bootstrap-readiness-report-link" -ExpectedExitCode 1 -ExpectedMessage "linked readiness report was not found" -Mutate {
    param($readinessPath, $bootstrapPath)
    $report = Get-Content -LiteralPath $bootstrapPath -Raw -Encoding UTF8 | ConvertFrom-Json
    $report.readinessReportPath = Join-Path (Split-Path -Parent $readinessPath) "other-bootstrap-readiness-report.json"
    Write-JsonFile -Path $bootstrapPath -Value $report
}
$results += Invoke-ValidatorScenario -Name "mismatched-readiness-bootstrap-report-path" -ExpectedExitCode 1 -ExpectedMessage "mismatch for readiness bootstrapSmokeReportPath" -Mutate {
    param($readinessPath, $bootstrapPath)
    $report = Get-Content -LiteralPath $readinessPath -Raw -Encoding UTF8 | ConvertFrom-Json
    $report.bootstrapSmokeReportPath = Join-Path (Split-Path -Parent $bootstrapPath) "other-readiness-bootstrap-report.json"
    Write-JsonFile -Path $readinessPath -Value $report
}
$results += Invoke-ValidatorScenario -Name "mismatched-readiness-smoke-report-path" -ExpectedExitCode 1 -ExpectedMessage "mismatch for readiness smokeReportPath" -Mutate {
    param($readinessPath, $bootstrapPath)
    $report = Get-Content -LiteralPath $readinessPath -Raw -Encoding UTF8 | ConvertFrom-Json
    $report.smokeReportPath = Join-Path (Split-Path -Parent $bootstrapPath) "other-readiness-smoke-report.json"
    Write-JsonFile -Path $readinessPath -Value $report
}
$results += Invoke-ValidatorScenario -Name "mismatched-readiness-preflight-report-path" -ExpectedExitCode 1 -ExpectedMessage "mismatch for readiness preflightReportPath" -Mutate {
    param($readinessPath, $bootstrapPath)
    $report = Get-Content -LiteralPath $readinessPath -Raw -Encoding UTF8 | ConvertFrom-Json
    $report.preflightReportPath = Join-Path (Split-Path -Parent $bootstrapPath) "other-readiness-preflight-report.json"
    Write-JsonFile -Path $readinessPath -Value $report
}
$results += Invoke-ValidatorScenario -Name "mismatched-readiness-provider" -ExpectedExitCode 1 -ExpectedMessage "mismatch for readiness provider" -Mutate {
    param($readinessPath, $bootstrapPath)
    $report = Get-Content -LiteralPath $readinessPath -Raw -Encoding UTF8 | ConvertFrom-Json
    $report.provider = "Postgres"
    Write-JsonFile -Path $readinessPath -Value $report
}
$results += Invoke-ValidatorScenario -Name "mismatched-readiness-password-env-name" -ExpectedExitCode 1 -ExpectedMessage "mismatch for readiness passwordEnvName" -Mutate {
    param($readinessPath, $bootstrapPath)
    $report = Get-Content -LiteralPath $readinessPath -Raw -Encoding UTF8 | ConvertFrom-Json
    $report.passwordEnvName = "OTHER_ADMIN_PASSWORD_ENV"
    Write-JsonFile -Path $readinessPath -Value $report
}
$results += Invoke-ValidatorScenario -Name "mismatched-readiness-local-sqlite" -ExpectedExitCode 1 -ExpectedMessage "mismatch for readiness localSqlite" -Mutate {
    param($readinessPath, $bootstrapPath)
    $report = Get-Content -LiteralPath $readinessPath -Raw -Encoding UTF8 | ConvertFrom-Json
    $report.localSqlite = $false
    $report.confirmBootstrapReset = $true
    Write-JsonFile -Path $readinessPath -Value $report
}
$results += Invoke-ValidatorScenario -Name "mismatched-readiness-confirm-bootstrap-reset" -ExpectedExitCode 1 -ExpectedMessage "mismatch for readiness confirmBootstrapReset" -Mutate {
    param($readinessPath, $bootstrapPath)
    $report = Get-Content -LiteralPath $readinessPath -Raw -Encoding UTF8 | ConvertFrom-Json
    $report.confirmBootstrapReset = $true
    Write-JsonFile -Path $readinessPath -Value $report
}
$results += Invoke-ValidatorScenario -Name "mismatched-bootstrap-smoke-report-path" -ExpectedExitCode 1 -ExpectedMessage "mismatch for bootstrapSmokeReportPath" -Mutate {
    param($readinessPath, $bootstrapPath)
    $report = Get-Content -LiteralPath $bootstrapPath -Raw -Encoding UTF8 | ConvertFrom-Json
    $report.bootstrapSmokeReportPath = Join-Path (Split-Path -Parent $bootstrapPath) "other-bootstrap-smoke-report.json"
    Write-JsonFile -Path $bootstrapPath -Value $report
}
$results += Invoke-ValidatorScenario -Name "mismatched-bootstrap-admin-email" -ExpectedExitCode 1 -ExpectedMessage "mismatch for preflight adminEmail" -Mutate {
    param($readinessPath, $bootstrapPath)
    $readiness = Get-Content -LiteralPath $readinessPath -Raw -Encoding UTF8 | ConvertFrom-Json
    $bootstrap = Get-Content -LiteralPath $bootstrapPath -Raw -Encoding UTF8 | ConvertFrom-Json
    $readiness.adminEmail = "other-admin@example.test"
    $bootstrap.adminEmail = "other-admin@example.test"
    Write-JsonFile -Path $readinessPath -Value $readiness
    Write-JsonFile -Path $bootstrapPath -Value $bootstrap
}
$results += Invoke-ValidatorScenario -Name "mismatched-bootstrap-environment" -ExpectedExitCode 1 -ExpectedMessage "mismatch for preflight apiBaseUrl" -Mutate {
    param($readinessPath, $bootstrapPath)
    $readiness = Get-Content -LiteralPath $readinessPath -Raw -Encoding UTF8 | ConvertFrom-Json
    $bootstrap = Get-Content -LiteralPath $bootstrapPath -Raw -Encoding UTF8 | ConvertFrom-Json
    $readiness.environmentName = "Other"
    $bootstrap.environmentName = "Other"
    $readiness.operator = "other-bootstrap-operator"
    $bootstrap.operator = "other-bootstrap-operator"
    $readiness.apiBaseUrl = "http://127.0.0.1:19011"
    $bootstrap.apiBaseUrl = "http://127.0.0.1:19011"
    Write-JsonFile -Path $readinessPath -Value $readiness
    Write-JsonFile -Path $bootstrapPath -Value $bootstrap
}
$results += Invoke-ValidatorScenario -Name "mismatched-smoke-release-id" -ExpectedExitCode 1 -ExpectedMessage "mismatch for preflight releaseId" -Mutate {
    param($readinessPath, $bootstrapPath, $preflightPath, $smokePath)
    $preflight = Get-Content -LiteralPath $preflightPath -Raw -Encoding UTF8 | ConvertFrom-Json
    $smoke = Get-Content -LiteralPath $smokePath -Raw -Encoding UTF8 | ConvertFrom-Json
    $preflight.releaseId = "bootstrap-smoke-evidence-validator-other-smoke-release"
    $smoke.releaseId = "bootstrap-smoke-evidence-validator-other-smoke-release"
    Write-JsonFile -Path $preflightPath -Value $preflight
    Write-JsonFile -Path $smokePath -Value $smoke
}
$results += Invoke-ValidatorScenario -Name "preflight-generated-before-readiness" -ExpectedExitCode 1 -ExpectedMessage "linked preflight generatedAt must not be before readiness generatedAt" -Mutate {
    param($readinessPath, $bootstrapPath, $preflightPath, $smokePath)
    $readiness = Get-Content -LiteralPath $readinessPath -Raw -Encoding UTF8 | ConvertFrom-Json
    $preflight = Get-Content -LiteralPath $preflightPath -Raw -Encoding UTF8 | ConvertFrom-Json
    $preflight.generatedAt = ([DateTimeOffset]::Parse([string]$readiness.generatedAt).AddSeconds(-1)).ToString("o")
    Write-JsonFile -Path $preflightPath -Value $preflight
}
$results += Invoke-ValidatorScenario -Name "bad-timing" -ExpectedExitCode 1 -ExpectedMessage "generatedAt must not be before linked smoke completedAt" -Mutate {
    param($readinessPath, $bootstrapPath)
    $readiness = Get-Content -LiteralPath $readinessPath -Raw -Encoding UTF8 | ConvertFrom-Json
    $report = Get-Content -LiteralPath $bootstrapPath -Raw -Encoding UTF8 | ConvertFrom-Json
    $report.generatedAt = ([DateTimeOffset]::Parse([string]$readiness.generatedAt).AddMinutes(-1)).ToString("o")
    Write-JsonFile -Path $bootstrapPath -Value $report
}
$results += Invoke-ValidatorScenario -Name "bootstrap-generated-before-smoke-completed" -ExpectedExitCode 1 -ExpectedMessage "generatedAt must not be before linked smoke completedAt" -Mutate {
    param($readinessPath, $bootstrapPath, $preflightPath, $smokePath)
    $smoke = Get-Content -LiteralPath $smokePath -Raw -Encoding UTF8 | ConvertFrom-Json
    $report = Get-Content -LiteralPath $bootstrapPath -Raw -Encoding UTF8 | ConvertFrom-Json
    $report.generatedAt = ([DateTimeOffset]::Parse([string]$smoke.completedAt).AddSeconds(-1)).ToString("o")
    Write-JsonFile -Path $bootstrapPath -Value $report
}
$results += Invoke-ValidatorScenario -Name "bad-smoke-route" -ExpectedExitCode 1 -ExpectedMessage "route must match sections contract" -Mutate {
    param($readinessPath, $bootstrapPath, $preflightPath, $smokePath)
    $report = Get-Content -LiteralPath $smokePath -Raw -Encoding UTF8 | ConvertFrom-Json
    $report.sections[0].route = "/admin/$($report.sections[0].id)"
    Write-JsonFile -Path $smokePath -Value $report
}

Write-Host "admin vps bootstrap smoke evidence validator regression passed $($results | ConvertTo-Json -Compress)"
