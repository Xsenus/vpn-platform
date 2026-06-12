param(
    [string]$DatabaseUrl = $env:DATABASE_URL,
    [string]$AuditDir = $(if ($env:SCHEMA_AUDIT_DIR) { $env:SCHEMA_AUDIT_DIR } else { "artifacts\postgres-schema-audit" })
)

$ErrorActionPreference = "Stop"

function Fail([string]$Message) {
    Write-Error "[FAIL] $Message"
    exit 1
}

function RequireCommand([string]$Name) {
    if (-not (Get-Command $Name -ErrorAction SilentlyContinue)) {
        Fail "Required command is missing: $Name"
    }
}

$rootDir = Split-Path -Parent $PSScriptRoot
$backendDir = Join-Path $rootDir "backend"
$auditPath = if ([System.IO.Path]::IsPathRooted($AuditDir)) { $AuditDir } else { Join-Path $rootDir $AuditDir }
$snapshotFile = Join-Path $auditPath "postgres-schema-snapshot.txt"
$migrationsFile = Join-Path $auditPath "ef-migrations.txt"
$migrationSqlFile = Join-Path $auditPath "postgres-migrations-idempotent.sql"

RequireCommand "dotnet"
RequireCommand "git"
New-Item -ItemType Directory -Force -Path $auditPath | Out-Null

$env:ASPNETCORE_ENVIRONMENT = if ($env:ASPNETCORE_ENVIRONMENT) { $env:ASPNETCORE_ENVIRONMENT } else { "Development" }
$env:ConnectionStrings__DefaultConnection = if ($env:ConnectionStrings__DefaultConnection) { $env:ConnectionStrings__DefaultConnection } else { "Host=localhost;Port=5432;Database=vpnplatform_schema_audit;Username=vpnplatform;Password=vpnplatform" }
$env:Jwt__Issuer = if ($env:Jwt__Issuer) { $env:Jwt__Issuer } else { "vpn-platform" }
$env:Jwt__Audience = if ($env:Jwt__Audience) { $env:Jwt__Audience } else { "vpn-platform" }
$env:Jwt__SigningKey = if ($env:Jwt__SigningKey) { $env:Jwt__SigningKey } else { "schema-audit-signing-key-0000000000000000000000" }
$env:Security__SecretEncryptionKey = if ($env:Security__SecretEncryptionKey) { $env:Security__SecretEncryptionKey } else { "schema-audit-secret-encryption-key-000000000000000000" }
$env:Database__ApplyMigrationsOnStartup = "false"
$env:Database__SeedDemoData = "false"
$env:AdminBootstrap__Enabled = "false"
$env:Provisioning__LiveExecutionEnabled = "false"
$env:Provisioning__AllowLiveDeploy = "false"
$env:TelegramBot__Enabled = "false"
$env:TelegramBot__BotToken = ""
$env:TelegramBot__WebhookUrl = ""
$env:TelegramBot__SecretToken = ""
$env:Payments__YooMoney__Mode = "Disabled"
$env:Payments__YooKassa__Mode = "Disabled"
$env:Payments__RoboKassa__Mode = "Disabled"
$env:Payments__TelegramStars__Mode = "Disabled"
$env:Payments__CloudPayments__Mode = "Disabled"
$env:Payments__TBankAcquiring__Mode = "Disabled"
$env:Payments__Prodamus__Mode = "Disabled"
$env:Payments__Stripe__Mode = "Disabled"
$env:Payments__PayPal__Mode = "Disabled"
$env:Vpn__X3Ui__Mode = "Sandbox"
$env:X3UI_BASE_URL = ""
$env:X3UI_USERNAME = ""
$env:X3UI_PASSWORD = ""

$metadata = @(
    "GeneratedAtUtc=$((Get-Date).ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ"))",
    "GitCommit=$((& git -C $rootDir rev-parse --short HEAD 2>$null) -join '')",
    "DotnetVersion=$((& dotnet --version) -join '')",
    "Mode=$(if ($DatabaseUrl) { "postgres" } else { "ef-only" })"
)
[System.IO.File]::WriteAllLines((Join-Path $auditPath "audit-metadata.env"), $metadata, [System.Text.Encoding]::UTF8)

Push-Location $backendDir
try {
    & dotnet tool restore | Out-Null

    & dotnet ef migrations list `
        --project src/VpnPlatform.Infrastructure `
        --startup-project src/VpnPlatform.Api `
        --context ApplicationDbContext `
        --no-connect | Set-Content -Path $migrationsFile -Encoding UTF8
    if ($LASTEXITCODE -ne 0) {
        Fail "dotnet ef migrations list failed."
    }

    & dotnet ef migrations script `
        --project src/VpnPlatform.Infrastructure `
        --startup-project src/VpnPlatform.Api `
        --context ApplicationDbContext `
        --idempotent `
        --output $migrationSqlFile
    if ($LASTEXITCODE -ne 0) {
        Fail "dotnet ef migrations script failed."
    }
}
finally {
    Pop-Location
}

if (-not $DatabaseUrl) {
    [System.IO.File]::WriteAllLines(
        $snapshotFile,
        @(
            "PostgreSQL snapshot skipped: DATABASE_URL is not set.",
            "Generated EF artifacts:",
            "- $migrationsFile",
            "- $migrationSqlFile"
        ),
        [System.Text.Encoding]::UTF8)
    Write-Host "[OK] PostgreSQL schema audit generated in EF-only mode: $auditPath"
    exit 0
}

RequireCommand "psql"
$queryFile = [System.IO.Path]::GetTempFileName()
try {
    @'
\pset pager off
\pset format aligned
\echo == tables ==
SELECT table_schema, table_name
FROM information_schema.tables
WHERE table_schema NOT IN ('pg_catalog', 'information_schema')
  AND table_type = 'BASE TABLE'
ORDER BY table_schema, table_name;

\echo == columns ==
SELECT table_schema, table_name, column_name, data_type, is_nullable, column_default IS NOT NULL AS has_default
FROM information_schema.columns
WHERE table_schema NOT IN ('pg_catalog', 'information_schema')
ORDER BY table_schema, table_name, ordinal_position;

\echo == nullable_columns ==
SELECT table_schema, table_name, column_name, data_type
FROM information_schema.columns
WHERE table_schema NOT IN ('pg_catalog', 'information_schema')
  AND is_nullable = 'YES'
ORDER BY table_schema, table_name, ordinal_position;

\echo == indexes ==
SELECT schemaname, tablename, indexname, indexdef
FROM pg_indexes
WHERE schemaname NOT IN ('pg_catalog', 'information_schema')
ORDER BY schemaname, tablename, indexname;

\echo == foreign_keys ==
SELECT
  tc.table_schema,
  tc.table_name,
  tc.constraint_name,
  kcu.column_name,
  ccu.table_name AS foreign_table_name,
  ccu.column_name AS foreign_column_name,
  rc.update_rule,
  rc.delete_rule
FROM information_schema.table_constraints tc
JOIN information_schema.key_column_usage kcu
  ON tc.constraint_name = kcu.constraint_name
 AND tc.table_schema = kcu.table_schema
JOIN information_schema.constraint_column_usage ccu
  ON ccu.constraint_name = tc.constraint_name
 AND ccu.table_schema = tc.table_schema
LEFT JOIN information_schema.referential_constraints rc
  ON rc.constraint_name = tc.constraint_name
 AND rc.constraint_schema = tc.table_schema
WHERE tc.constraint_type = 'FOREIGN KEY'
ORDER BY tc.table_schema, tc.table_name, tc.constraint_name, kcu.ordinal_position;
'@ | Set-Content -Path $queryFile -Encoding UTF8

    & psql $DatabaseUrl -v ON_ERROR_STOP=1 -X -f $queryFile -o $snapshotFile
    if ($LASTEXITCODE -ne 0) {
        Fail "psql schema snapshot failed."
    }
}
finally {
    Remove-Item -LiteralPath $queryFile -Force -ErrorAction SilentlyContinue
}

Write-Host "[OK] PostgreSQL schema audit generated: $auditPath"
