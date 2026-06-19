param(
    [int]$ApiPort = 18211,
    [int]$AdminPort = 18215,
    [switch]$KeepArtifacts
)

$ErrorActionPreference = "Stop"

$root = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$tmp = Join-Path $root "tmp\local-admin-vps-bootstrap-smoke"
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

    $listeners = @(Get-NetTCPConnection -LocalPort $Port -State Listen -ErrorAction SilentlyContinue)
    if ($listeners.Count -gt 0) {
        throw "$Name port $Port is already occupied."
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

function Get-ChildProcessIds {
    param([int]$ParentId)

    $children = Get-CimInstance Win32_Process -Filter "ParentProcessId=$ParentId" -ErrorAction SilentlyContinue
    foreach ($child in $children) {
        [int]$child.ProcessId
        Get-ChildProcessIds -ParentId ([int]$child.ProcessId)
    }
}

function Stop-ProcessTree {
    param([System.Diagnostics.Process]$Process)

    if ($null -eq $Process) {
        return
    }

    $ids = @(Get-ChildProcessIds -ParentId ([int]$Process.Id)) + @([int]$Process.Id)
    foreach ($id in ($ids | Select-Object -Unique)) {
        Stop-Process -Id $id -Force -ErrorAction SilentlyContinue
    }
}

Assert-InWorkspace $tmp
Assert-PortFree -Port $ApiPort -Name "API"
Assert-PortFree -Port $AdminPort -Name "Admin web"

if (Test-Path $tmp) {
    Remove-Item -LiteralPath $tmp -Recurse -Force
}

New-Item -ItemType Directory -Path $tmp | Out-Null
$logDir = Join-Path $tmp "logs"
New-Item -ItemType Directory -Path $logDir | Out-Null

$apiUrl = "http://127.0.0.1:$ApiPort"
$adminUrl = "http://127.0.0.1:$AdminPort"
$databasePath = Join-Path $tmp "vpnplatform-local-admin-bootstrap-smoke.db"
$connectionString = "Data Source=$databasePath"
$keyPath = Join-Path $tmp "keys"
$reportRelativePath = "tmp/local-admin-vps-bootstrap-smoke/admin-vps-smoke-report.json"
$preflightReportRelativePath = "tmp/local-admin-vps-bootstrap-smoke/admin-vps-smoke-preflight-report.json"
$bootstrapSmokeReportRelativePath = "tmp/local-admin-vps-bootstrap-smoke/admin-vps-bootstrap-smoke-report.json"
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
        -EnvironmentName "Local" `
        -Operator "local-admin-vps-bootstrap-smoke" `
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
    }
    else {
        Write-Host "Artifacts kept in $tmp"
    }
}
