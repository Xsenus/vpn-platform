#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
BACKEND_DIR="$ROOT_DIR/backend"
AUDIT_DIR="${SCHEMA_AUDIT_DIR:-$ROOT_DIR/artifacts/postgres-schema-audit}"
SNAPSHOT_FILE="$AUDIT_DIR/postgres-schema-snapshot.txt"
MIGRATIONS_FILE="$AUDIT_DIR/ef-migrations.txt"
MIGRATION_SQL_FILE="$AUDIT_DIR/postgres-migrations-idempotent.sql"

require() {
  if ! command -v "$1" >/dev/null 2>&1; then
    echo "[FAIL] Required command is missing: $1" >&2
    exit 127
  fi
}

write_metadata() {
  {
    echo "GeneratedAtUtc=$(date -u +"%Y-%m-%dT%H:%M:%SZ")"
    echo "GitCommit=$(git rev-parse --short HEAD 2>/dev/null || echo unknown)"
    echo "DotnetVersion=$(dotnet --version)"
    echo "Mode=${DATABASE_URL:+postgres}"
    if [[ -z "${DATABASE_URL:-}" ]]; then
      echo "Mode=ef-only"
    fi
  } > "$AUDIT_DIR/audit-metadata.env"
}

export ASPNETCORE_ENVIRONMENT="${ASPNETCORE_ENVIRONMENT:-Development}"
export ConnectionStrings__DefaultConnection="${ConnectionStrings__DefaultConnection:-Host=localhost;Port=5432;Database=vpnplatform_schema_audit;Username=vpnplatform;Password=vpnplatform}"
export Jwt__Issuer="${Jwt__Issuer:-vpn-platform}"
export Jwt__Audience="${Jwt__Audience:-vpn-platform}"
export Jwt__SigningKey="${Jwt__SigningKey:-schema-audit-signing-key-0000000000000000000000}"
export Security__SecretEncryptionKey="${Security__SecretEncryptionKey:-schema-audit-secret-encryption-key-000000000000000000}"
export Database__ApplyMigrationsOnStartup="false"
export Database__SeedDemoData="false"
export AdminBootstrap__Enabled="false"
export Provisioning__LiveExecutionEnabled="false"
export Provisioning__AllowLiveDeploy="false"
export TelegramBot__Enabled="false"
export TelegramBot__BotToken=""
export TelegramBot__WebhookUrl=""
export TelegramBot__SecretToken=""
export Payments__YooMoney__Mode="Disabled"
export Payments__YooKassa__Mode="Disabled"
export Payments__RoboKassa__Mode="Disabled"
export Payments__TelegramStars__Mode="Disabled"
export Payments__CloudPayments__Mode="Disabled"
export Payments__TBankAcquiring__Mode="Disabled"
export Payments__Prodamus__Mode="Disabled"
export Payments__Stripe__Mode="Disabled"
export Payments__PayPal__Mode="Disabled"
export Vpn__X3Ui__Mode="Sandbox"
export X3UI_BASE_URL=""
export X3UI_USERNAME=""
export X3UI_PASSWORD=""

cd "$ROOT_DIR"
require dotnet
require git
mkdir -p "$AUDIT_DIR"
write_metadata

cd "$BACKEND_DIR"
dotnet tool restore >/dev/null 2>&1 || true

dotnet ef migrations list \
  --project src/VpnPlatform.Infrastructure \
  --startup-project src/VpnPlatform.Api \
  --context ApplicationDbContext \
  --no-connect > "$MIGRATIONS_FILE"

dotnet ef migrations script \
  --project src/VpnPlatform.Infrastructure \
  --startup-project src/VpnPlatform.Api \
  --context ApplicationDbContext \
  --idempotent \
  --output "$MIGRATION_SQL_FILE"

if [[ -z "${DATABASE_URL:-}" ]]; then
  {
    echo "PostgreSQL snapshot skipped: DATABASE_URL is not set."
    echo "Generated EF artifacts:"
    echo "- $MIGRATIONS_FILE"
    echo "- $MIGRATION_SQL_FILE"
  } > "$SNAPSHOT_FILE"
  echo "[OK] PostgreSQL schema audit generated in EF-only mode: $AUDIT_DIR"
  exit 0
fi

require psql
QUERY_FILE="$(mktemp)"
trap 'rm -f "$QUERY_FILE"' EXIT
cat > "$QUERY_FILE" <<'SQL'
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
SQL

psql "$DATABASE_URL" -v ON_ERROR_STOP=1 -X -f "$QUERY_FILE" -o "$SNAPSHOT_FILE"
echo "[OK] PostgreSQL schema audit generated: $AUDIT_DIR"
