param(
    [string]$BackupFile = $env:BACKUP_FILE,
    [string]$RestoreDatabaseUrl = $env:RESTORE_DATABASE_URL,
    [string]$DatabaseUrl = $env:DATABASE_URL,
    [bool]$AllowDatabaseUrlMatch = $(if ($env:RESTORE_ALLOW_DATABASE_URL_MATCH) { $env:RESTORE_ALLOW_DATABASE_URL_MATCH -eq "true" } else { $false })
)

$ErrorActionPreference = "Stop"

function Fail([string]$Message) {
    Write-Error "[FAIL] $Message"
    exit 1
}

if ([string]::IsNullOrWhiteSpace($BackupFile)) {
    Fail "Set BACKUP_FILE to a .dump file created by scripts\backup-db.ps1."
}

if ([string]::IsNullOrWhiteSpace($RestoreDatabaseUrl)) {
    Fail "Set RESTORE_DATABASE_URL for the target restore database."
}

if (-not (Test-Path -LiteralPath $BackupFile -PathType Leaf)) {
    Fail "Backup file not found: $BackupFile"
}

if (-not [string]::IsNullOrWhiteSpace($DatabaseUrl) -and $RestoreDatabaseUrl -eq $DatabaseUrl -and -not $AllowDatabaseUrlMatch) {
    Fail "RESTORE_DATABASE_URL matches DATABASE_URL. Restore to a separate DB or set RESTORE_ALLOW_DATABASE_URL_MATCH=true intentionally."
}

$pgRestore = Get-Command pg_restore -ErrorAction SilentlyContinue
if (-not $pgRestore) {
    Fail "pg_restore is required for PostgreSQL restore."
}

& $pgRestore.Source `
    --clean `
    --if-exists `
    --no-owner `
    --no-privileges `
    --dbname $RestoreDatabaseUrl `
    $BackupFile
if ($LASTEXITCODE -ne 0) {
    Fail "pg_restore failed."
}

Write-Host "[OK] Restored PostgreSQL backup into target database."
