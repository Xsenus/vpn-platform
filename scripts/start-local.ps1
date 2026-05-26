param(
    [int]$ApiPort = 8080,
    [int]$PublicPort = 5173,
    [int]$CabinetPort = 5174,
    [int]$AdminPort = 5175
)

$ErrorActionPreference = "Stop"

$root = Resolve-Path (Join-Path $PSScriptRoot "..")
$dataDir = Join-Path $root "data"
$logDir = Join-Path $dataDir "logs"
$pidFile = Join-Path $dataDir "local-processes.json"

New-Item -ItemType Directory -Force -Path $logDir | Out-Null

if (Test-Path $pidFile) {
    Write-Host "Found $pidFile. Run scripts\stop-local.ps1 first, or remove the file if processes are already stopped."
    exit 1
}

function Assert-LocalPortFree {
    param(
        [int]$Port,
        [string]$Name
    )

    $listeners = @(Get-NetTCPConnection -LocalPort $Port -State Listen -ErrorAction SilentlyContinue)
    if ($listeners.Count -eq 0) {
        return
    }

    $owners = $listeners |
        Select-Object -ExpandProperty OwningProcess -Unique |
        ForEach-Object {
            $process = Get-Process -Id $_ -ErrorAction SilentlyContinue
            if ($process) {
                "$($process.ProcessName)[$_]"
            }
            else {
                "pid[$_]"
            }
        }

    Write-Host "$Name port $Port is already occupied: $($owners -join ', ')."
    Write-Host "Stop the conflicting process or pass another port to scripts\start-local.ps1."
    exit 1
}

Assert-LocalPortFree -Port $ApiPort -Name "API"
Assert-LocalPortFree -Port $PublicPort -Name "Public web"
Assert-LocalPortFree -Port $CabinetPort -Name "Cabinet"
Assert-LocalPortFree -Port $AdminPort -Name "Admin"

function Start-LocalProcess {
    param(
        [string]$Name,
        [string]$FilePath,
        [string[]]$Arguments,
        [string]$WorkingDirectory,
        [hashtable]$Environment = @{}
    )

    $previous = @{}
    foreach ($key in $Environment.Keys) {
        $previous[$key] = [Environment]::GetEnvironmentVariable($key, "Process")
        [Environment]::SetEnvironmentVariable($key, [string]$Environment[$key], "Process")
    }

    try {
        $stdout = Join-Path $logDir "$Name.out.log"
        $stderr = Join-Path $logDir "$Name.err.log"
        $process = Start-Process -FilePath $FilePath `
            -ArgumentList $Arguments `
            -WorkingDirectory $WorkingDirectory `
            -PassThru `
            -WindowStyle Hidden `
            -RedirectStandardOutput $stdout `
            -RedirectStandardError $stderr

        return [ordered]@{
            name = $Name
            id = $process.Id
            stdout = $stdout
            stderr = $stderr
        }
    }
    finally {
        foreach ($key in $Environment.Keys) {
            [Environment]::SetEnvironmentVariable($key, $previous[$key], "Process")
        }
    }
}

$apiUrl = "http://127.0.0.1:$ApiPort"
$frontendApiUrl = $apiUrl
$processes = @()

$processes += Start-LocalProcess `
    -Name "api" `
    -FilePath "dotnet" `
    -Arguments @("run", "--project", "backend\src\VpnPlatform.Api\VpnPlatform.Api.csproj", "--urls", $apiUrl) `
    -WorkingDirectory $root `
    -Environment @{
        ASPNETCORE_ENVIRONMENT = "Local"
        ASPNETCORE_URLS = $apiUrl
        "Cors__AllowedOrigins__0" = "http://127.0.0.1:$PublicPort"
        "Cors__AllowedOrigins__1" = "http://127.0.0.1:$CabinetPort"
        "Cors__AllowedOrigins__2" = "http://127.0.0.1:$AdminPort"
        "Cors__AllowedOrigins__3" = "http://localhost:$PublicPort"
        "Cors__AllowedOrigins__4" = "http://localhost:$CabinetPort"
        "Cors__AllowedOrigins__5" = "http://localhost:$AdminPort"
    }

$processes += Start-LocalProcess `
    -Name "public-web" `
    -FilePath "npm.cmd" `
    -Arguments @("--workspace", "apps/public-web", "run", "dev", "--", "--host", "127.0.0.1", "--port", "$PublicPort", "--strictPort") `
    -WorkingDirectory (Join-Path $root "frontend") `
    -Environment @{ VITE_API_BASE_URL = $frontendApiUrl }

$processes += Start-LocalProcess `
    -Name "cabinet" `
    -FilePath "npm.cmd" `
    -Arguments @("--workspace", "apps/cabinet", "run", "dev", "--", "--host", "127.0.0.1", "--port", "$CabinetPort", "--strictPort") `
    -WorkingDirectory (Join-Path $root "frontend") `
    -Environment @{ VITE_API_BASE_URL = $frontendApiUrl }

$processes += Start-LocalProcess `
    -Name "admin-panel" `
    -FilePath "npm.cmd" `
    -Arguments @("--workspace", "apps/admin-panel", "run", "dev", "--", "--host", "127.0.0.1", "--port", "$AdminPort", "--strictPort") `
    -WorkingDirectory (Join-Path $root "frontend") `
    -Environment @{ VITE_API_BASE_URL = $frontendApiUrl }

$processes | ConvertTo-Json -Depth 4 | Set-Content -Path $pidFile -Encoding UTF8

function Wait-LocalUrl {
    param(
        [string]$Url,
        [int]$Attempts = 60
    )

    for ($i = 0; $i -lt $Attempts; $i++) {
        Start-Sleep -Milliseconds 500
        try {
            Invoke-WebRequest -Uri $Url -UseBasicParsing -TimeoutSec 2 | Out-Null
            return $true
        }
        catch {
        }
    }

    return $false
}

if (-not (Wait-LocalUrl "$apiUrl/health/live")) {
    Write-Host "API did not respond to health-check. Logs: $logDir"
    Write-Host "Stop command: powershell -ExecutionPolicy Bypass -File scripts\stop-local.ps1"
    exit 1
}

$frontendUrls = @(
    "http://127.0.0.1:$PublicPort",
    "http://127.0.0.1:$CabinetPort",
    "http://127.0.0.1:$AdminPort"
)

foreach ($url in $frontendUrls) {
    if (-not (Wait-LocalUrl $url)) {
        Write-Host "$url did not respond. Logs: $logDir"
        Write-Host "Stop command: powershell -ExecutionPolicy Bypass -File scripts\stop-local.ps1"
        exit 1
    }
}

if (-not (Test-Path (Join-Path $root "frontend\node_modules\@rollup\rollup-win32-x64-msvc"))) {
    Write-Host "Warning: Rollup optional dependency for Windows is missing. Run 'npm install' in frontend if Vite fails."
}

if (-not (Test-Path (Join-Path $root "frontend\node_modules"))) {
    Write-Host "Frontend dependencies are missing. Run 'npm install' in frontend."
    Write-Host "Stop command: powershell -ExecutionPolicy Bypass -File scripts\stop-local.ps1"
    exit 1
}

Write-Host "Local environment is running without Docker."
Write-Host "API:        $apiUrl/swagger"
Write-Host "Public web: http://127.0.0.1:$PublicPort"
Write-Host "Cabinet:    http://127.0.0.1:$CabinetPort"
Write-Host "Admin:      http://127.0.0.1:$AdminPort"
Write-Host "Admin user: admin@local.test / LocalAdminPassword123!"
Write-Host "Logs:       $logDir"
Write-Host "Stop:       powershell -ExecutionPolicy Bypass -File scripts\stop-local.ps1"
