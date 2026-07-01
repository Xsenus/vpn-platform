param(
    [string]$ApiPort = "18211",
    [string]$AdminPort = "18215",
    [string]$MaxEvidenceChainMinutes = $(if ($env:ADMIN_VPS_SMOKE_MAX_EVIDENCE_CHAIN_MINUTES) { $env:ADMIN_VPS_SMOKE_MAX_EVIDENCE_CHAIN_MINUTES } else { "120" }),
    [switch]$KeepArtifacts
)

$ErrorActionPreference = "Stop"

$root = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$tmp = Join-Path $root "tmp\local-admin-vps-bootstrap-smoke"
$tmpRoot = Join-Path $root "tmp"
$resolvedRoot = (Resolve-Path $root).Path

function Assert-InWorkspace {
    param([string]$Path)

    $fullPath = if (Test-Path $Path) { (Resolve-Path $Path).Path } else { [System.IO.Path]::GetFullPath($Path) }
    if (-not $fullPath.StartsWith($resolvedRoot, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "Refusing to touch path outside workspace: $fullPath"
    }
}

function Assert-PortFree {
    param(
        [int]$Port,
        [string]$Name
    )

    $listener = $null
    try {
        $listener = [System.Net.Sockets.TcpListener]::new([System.Net.IPAddress]::Loopback, $Port)
        $listener.Start()
    }
    catch [System.Net.Sockets.SocketException] {
        throw "$Name port $Port is already occupied."
    }
    finally {
        if ($null -ne $listener) {
            $listener.Stop()
        }
    }
}

function Set-ScopedEnv {
    param(
        [hashtable]$Previous,
        [string]$Name,
        [AllowEmptyString()][string]$Value
    )

    if (-not $Previous.ContainsKey($Name)) {
        $Previous[$Name] = [Environment]::GetEnvironmentVariable($Name, "Process")
    }

    [Environment]::SetEnvironmentVariable($Name, $Value, "Process")
}

function Stop-ProcessTree {
    param([System.Diagnostics.Process]$Process)

    if ($null -eq $Process) {
        return
    }

    $processId = [int]$Process.Id
    if (-not (Get-Process -Id $processId -ErrorAction SilentlyContinue)) {
        return
    }

    try {
        & "$env:SystemRoot\System32\taskkill.exe" /PID $processId /T /F 2>&1 | Out-Null
        if ($LASTEXITCODE -ne 0 -and (Get-Process -Id $processId -ErrorAction SilentlyContinue)) {
            Stop-Process -Id $processId -Force -ErrorAction SilentlyContinue
        }
    }
    catch {
        Stop-Process -Id $processId -Force -ErrorAction SilentlyContinue
    }
}

function Wait-HttpOk {
    param(
        [string]$Url,
        [int]$Attempts = 120
    )

    for ($attempt = 0; $attempt -lt $Attempts; $attempt++) {
        Start-Sleep -Milliseconds 500
        try {
            Invoke-WebRequest -Uri $Url -UseBasicParsing -TimeoutSec 3 | Out-Null
            return
        }
        catch {
        }
    }

    throw "URL did not become ready: $Url"
}

function Convert-MaxEvidenceChainMinutes {
    param([AllowEmptyString()][string]$Value)

    if ([string]::IsNullOrWhiteSpace($Value)) {
        throw "MaxEvidenceChainMinutes must be an integer."
    }

    $parsed = 0
    if (-not [int]::TryParse($Value.Trim(), [System.Globalization.NumberStyles]::Integer, [System.Globalization.CultureInfo]::InvariantCulture, [ref]$parsed)) {
        throw "MaxEvidenceChainMinutes must be an integer."
    }

    if ($parsed -le 0) {
        throw "MaxEvidenceChainMinutes must be greater than 0."
    }

    if ($parsed -gt 1440) {
        throw "MaxEvidenceChainMinutes must be less than or equal to 1440."
    }

    return $parsed
}

function Convert-TcpPort {
    param(
        [AllowEmptyString()][string]$Value,
        [Parameter(Mandatory = $true)][string]$Name
    )

    if ([string]::IsNullOrWhiteSpace($Value)) {
        throw "$Name must be an integer."
    }

    $parsed = 0
    if (-not [int]::TryParse($Value.Trim(), [System.Globalization.NumberStyles]::Integer, [System.Globalization.CultureInfo]::InvariantCulture, [ref]$parsed)) {
        throw "$Name must be an integer."
    }

    if ($parsed -lt 1 -or $parsed -gt 65535) {
        throw "$Name must be between 1 and 65535."
    }

    return $parsed
}

Assert-InWorkspace $tmp
$apiPortValue = Convert-TcpPort -Value $ApiPort -Name "ApiPort"
$adminPortValue = Convert-TcpPort -Value $AdminPort -Name "AdminPort"
if ($apiPortValue -eq $adminPortValue) {
    throw "ApiPort and AdminPort must be different."
}

$maxEvidenceChainMinutesValue = Convert-MaxEvidenceChainMinutes -Value $MaxEvidenceChainMinutes

Assert-PortFree -Port $apiPortValue -Name "API"
Assert-PortFree -Port $adminPortValue -Name "Admin web"

if (Test-Path $tmp) {
    Remove-Item -LiteralPath $tmp -Recurse -Force
}

New-Item -ItemType Directory -Path $tmp | Out-Null
$logDir = Join-Path $tmp "logs"
New-Item -ItemType Directory -Path $logDir | Out-Null

$apiUrl = "http://127.0.0.1:$apiPortValue"
$adminUrl = "http://127.0.0.1:$adminPortValue"
$databasePath = Join-Path $tmp "vpnplatform-local-admin-bootstrap-smoke.db"
$connectionString = "Data Source=$databasePath"
$keyPath = Join-Path $tmp "keys"
$reportRelativePath = "tmp/local-admin-vps-bootstrap-smoke/admin-vps-smoke-report.json"
$preflightReportRelativePath = "tmp/local-admin-vps-bootstrap-smoke/admin-vps-smoke-preflight-report.json"
$bootstrapSmokeReportRelativePath = "tmp/local-admin-vps-bootstrap-smoke/admin-vps-bootstrap-smoke-report.json"
$readinessReportRelativePath = "tmp/local-admin-vps-bootstrap-smoke/admin-vps-bootstrap-smoke-readiness-report.json"
$password = "LocalBootstrapSmokePassword123!"
$previousEnv = @{}
$apiProcess = $null
$adminProcess = $null

try {
    & (Join-Path $PSScriptRoot "admin-bootstrap.ps1") `
        -LocalSqlite `
        -EnvironmentName "Local" `
        -Email "fresh-bootstrap-admin@example.test" `
        -Password $password `
        -DisplayName "Fresh Bootstrap Local Admin" `
        -RolesCsv "SuperAdmin" `
        -ConnectionString $connectionString `
        -DataProtectionKeyPath $keyPath

    $envMap = @{
        ASPNETCORE_ENVIRONMENT = "Local"
        ASPNETCORE_URLS = $apiUrl
        "Cors__AllowedOrigins__0" = $adminUrl
        "Cors__AllowedOrigins__1" = "http://localhost:$AdminPort"
        "Database__Provider" = "Sqlite"
        "Database__ApplyMigrationsOnStartup" = "false"
        "Database__UseEnsureCreatedForLocalSqlite" = "true"
        "Database__SeedDemoData" = "true"
        "ConnectionStrings__DefaultConnection" = $connectionString
        "AdminBootstrap__Enabled" = "false"
        "Jwt__SigningKey" = "local-admin-bootstrap-smoke-jwt-signing-key-64-characters-safe"
        "Security__SecretEncryptionKey" = "local-admin-bootstrap-smoke-key-32"
        "DataProtection__KeyPath" = $keyPath
        "Vpn__X3Ui__Mode" = "Sandbox"
    }

    foreach ($key in $envMap.Keys) {
        Set-ScopedEnv -Previous $previousEnv -Name $key -Value ([string]$envMap[$key])
    }

    $apiProcess = Start-Process -FilePath "dotnet" `
        -ArgumentList @("run", "--project", "backend\src\VpnPlatform.Api\VpnPlatform.Api.csproj", "--urls", $apiUrl) `
        -WorkingDirectory $root `
        -RedirectStandardOutput (Join-Path $logDir "api.out.log") `
        -RedirectStandardError (Join-Path $logDir "api.err.log") `
        -PassThru `
        -WindowStyle Hidden

    Wait-HttpOk "$apiUrl/health/live"
    Wait-HttpOk "$apiUrl/health/ready"

    Set-ScopedEnv -Previous $previousEnv -Name "VITE_API_BASE_URL" -Value $apiUrl
    $adminProcess = Start-Process -FilePath "npm.cmd" `
        -ArgumentList @("--workspace", "apps/admin-panel", "run", "dev", "--", "--host", "127.0.0.1", "--port", "$AdminPort", "--strictPort") `
        -WorkingDirectory (Join-Path $root "frontend") `
        -RedirectStandardOutput (Join-Path $logDir "admin.out.log") `
        -RedirectStandardError (Join-Path $logDir "admin.err.log") `
        -PassThru `
        -WindowStyle Hidden

    Wait-HttpOk $adminUrl

    Set-ScopedEnv -Previous $previousEnv -Name "ADMIN_VPS_BOOTSTRAP_SMOKE_ADMIN_PASSWORD" -Value $password

    & (Join-Path $PSScriptRoot "admin-vps-bootstrap-smoke.ps1") `
        -ApiBaseUrl $apiUrl `
        -AdminWebUrl $adminUrl `
        -AdminEmail "fresh-bootstrap-admin@example.test" `
        -ConnectionString $connectionString `
        -DataProtectionKeyPath $keyPath `
        -SmokeReportPath $reportRelativePath `
        -PreflightReportPath $preflightReportRelativePath `
        -BootstrapSmokeReportPath $bootstrapSmokeReportRelativePath `
        -ReadinessReportPath $readinessReportRelativePath `
        -EnvironmentName "Local" `
        -Operator "local-admin-vps-bootstrap-smoke" `
        -MaxEvidenceChainMinutes $maxEvidenceChainMinutesValue `
        -LocalSqlite

    Write-Output "local admin vps bootstrap smoke ok api=$apiUrl admin=$adminUrl report=$reportRelativePath"
}
finally {
    Stop-ProcessTree -Process $adminProcess
    Stop-ProcessTree -Process $apiProcess

    foreach ($key in $previousEnv.Keys) {
        [Environment]::SetEnvironmentVariable($key, $previousEnv[$key], "Process")
    }

    Start-Sleep -Milliseconds 500
    if (-not $KeepArtifacts) {
        Remove-Item -LiteralPath $tmp -Recurse -Force -ErrorAction SilentlyContinue
        if ((Test-Path -LiteralPath $tmpRoot) -and -not (Get-ChildItem -LiteralPath $tmpRoot -Force)) {
            Remove-Item -LiteralPath $tmpRoot -Force
        }
    }
    else {
        Write-Host "Artifacts kept in $tmp"
    }
}
