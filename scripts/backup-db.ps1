param(
    [string]$DatabaseUrl = $env:DATABASE_URL,
    [string]$BackupDir = $(if ($env:BACKUP_DIR) { $env:BACKUP_DIR } else { ".\backups\db" }),
    [int]$RetentionDays = $(if ($env:BACKUP_RETENTION_DAYS) { [int]$env:BACKUP_RETENTION_DAYS } else { 14 })
)

$ErrorActionPreference = "Stop"

function Fail([string]$Message) {
    Write-Error "[FAIL] $Message"
    exit 1
}

if ([string]::IsNullOrWhiteSpace($DatabaseUrl)) {
    Fail "Set DATABASE_URL, for example postgres://user:pass@host:5432/vpnplatform."
}

$pgDump = Get-Command pg_dump -ErrorAction SilentlyContinue
if (-not $pgDump) {
    Fail "pg_dump is required for PostgreSQL backups."
}

New-Item -ItemType Directory -Force -Path $BackupDir | Out-Null
$stamp = [DateTimeOffset]::UtcNow.ToString("yyyyMMddTHHmmssZ")
$out = Join-Path $BackupDir "vpnplatform-$stamp.dump"

& $pgDump.Source --format=custom --no-owner --no-privileges --file $out $DatabaseUrl
if ($LASTEXITCODE -ne 0) {
    Fail "pg_dump failed."
}

$pgRestore = Get-Command pg_restore -ErrorAction SilentlyContinue
if ($pgRestore) {
    & $pgRestore.Source --list $out | Set-Content -Path "$out.list" -Encoding UTF8
}

if ($RetentionDays -gt 0) {
    $threshold = (Get-Date).ToUniversalTime().AddDays(-$RetentionDays)
    Get-ChildItem -Path $BackupDir -File -ErrorAction SilentlyContinue |
        Where-Object { $_.Name -like "vpnplatform-*.dump" -or $_.Name -like "vpnplatform-*.dump.list" } |
        Where-Object { $_.LastWriteTimeUtc -lt $threshold } |
        Remove-Item -Force
}

Write-Output $out
