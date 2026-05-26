$ErrorActionPreference = "Stop"

$root = Resolve-Path (Join-Path $PSScriptRoot "..")
$pidFile = Join-Path $root "data\local-processes.json"

if (-not (Test-Path $pidFile)) {
    Write-Host "Process file not found: $pidFile"
    Write-Host "If the local environment was started manually, stop dotnet and npm processes manually."
    exit 0
}

$processes = Get-Content $pidFile -Raw | ConvertFrom-Json

function Get-ChildProcessIds {
    param([int]$ParentId)

    $children = Get-CimInstance Win32_Process -Filter "ParentProcessId=$ParentId" -ErrorAction SilentlyContinue
    foreach ($child in $children) {
        [int]$child.ProcessId
        Get-ChildProcessIds -ParentId ([int]$child.ProcessId)
    }
}

foreach ($process in $processes) {
    try {
        $ids = @(Get-ChildProcessIds -ParentId ([int]$process.id)) + @([int]$process.id)
        foreach ($id in ($ids | Select-Object -Unique)) {
            Stop-Process -Id $id -Force -ErrorAction SilentlyContinue
        }
        Write-Host "Stopped $($process.name), PID $($process.id)."
    }
    catch {
        Write-Host "$($process.name), PID $($process.id): not running."
    }
}

Remove-Item $pidFile -Force
Write-Host "Local environment stopped."
