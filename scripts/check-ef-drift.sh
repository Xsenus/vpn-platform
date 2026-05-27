#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
BACKEND_DIR="$ROOT_DIR/backend"
MIGRATIONS_REL="backend/src/VpnPlatform.Infrastructure/Persistence/Migrations"
MIGRATIONS_DIR="$ROOT_DIR/$MIGRATIONS_REL"
MIGRATION_NAME="__ModelDriftCheck"
SNAPSHOT_REL="$MIGRATIONS_REL/ApplicationDbContextModelSnapshot.cs"
SNAPSHOT_FILE="$ROOT_DIR/$SNAPSHOT_REL"

if ! command -v dotnet >/dev/null 2>&1; then
  echo "[FAIL] dotnet CLI is required for EF drift check." >&2
  exit 127
fi

if ! command -v git >/dev/null 2>&1; then
  echo "[FAIL] git is required because this check verifies that EF does not change migration files." >&2
  exit 127
fi

cd "$ROOT_DIR"
./scripts/check-validation-safety.sh
if ! git rev-parse --is-inside-work-tree >/dev/null 2>&1; then
  echo "[FAIL] EF drift check must run inside a git work tree so migration changes can be detected." >&2
  exit 1
fi

if [[ ! -d "$MIGRATIONS_DIR" ]]; then
  echo "[FAIL] EF migrations directory is missing: $MIGRATIONS_REL" >&2
  exit 1
fi

SNAPSHOT_WAS_CLEAN=0
if [[ -f "$SNAPSHOT_FILE" ]] && git diff --quiet -- "$SNAPSHOT_REL" >/dev/null 2>&1; then
  SNAPSHOT_WAS_CLEAN=1
fi

cleanup() {
  rm -f "$MIGRATIONS_DIR"/*"$MIGRATION_NAME"*.cs
  if [[ "$SNAPSHOT_WAS_CLEAN" == "1" ]]; then
    git checkout -- "$SNAPSHOT_REL" >/dev/null 2>&1 || true
  fi
}
trap cleanup EXIT

export ASPNETCORE_ENVIRONMENT="${ASPNETCORE_ENVIRONMENT:-Development}"
export ConnectionStrings__DefaultConnection="${ConnectionStrings__DefaultConnection:-Host=localhost;Port=5432;Database=vpnplatform_drift;Username=vpnplatform;Password=vpnplatform}"
export Jwt__Issuer="${Jwt__Issuer:-vpn-platform}"
export Jwt__Audience="${Jwt__Audience:-vpn-platform}"
export Jwt__SigningKey="${Jwt__SigningKey:-ef-drift-signing-key-00000000000000000000000000}"
export Security__SecretEncryptionKey="${Security__SecretEncryptionKey:-ef-drift-secret-encryption-key-00000000000000000000}"
export Database__ApplyMigrationsOnStartup="false"
export Database__SeedDemoData="false"
export AdminBootstrap__Enabled="false"
export Auth__RefreshTokenDays="30"
export Auth__PasswordReset__ExpiryMinutes="30"
export Auth__PasswordReset__ReturnTokenForValidation="false"
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

cd "$BACKEND_DIR"
dotnet tool restore

set +e
dotnet ef migrations has-pending-model-changes \
  --project src/VpnPlatform.Infrastructure \
  --startup-project src/VpnPlatform.Api \
  --context ApplicationDbContext
pending_status=$?
set -e

if [[ "$pending_status" == "0" ]]; then
  cd "$ROOT_DIR"
  if git diff --quiet -- "$MIGRATIONS_REL"; then
    echo "[OK] EF model has no pending migration changes."
    exit 0
  fi
  echo "[FAIL] EF changed migration files even though pending-model-changes reported clean." >&2
  git diff -- "$MIGRATIONS_REL" >&2 || true
  exit 1
fi

# Fallback diagnostic for SDK/EF versions where has-pending-model-changes is unavailable
# or reports pending changes. The generated migration must be empty.
dotnet ef migrations add "$MIGRATION_NAME" \
  --project src/VpnPlatform.Infrastructure \
  --startup-project src/VpnPlatform.Api \
  --context ApplicationDbContext \
  --output-dir Persistence/Migrations

DRIFT_FILE="$(find "$MIGRATIONS_DIR" -maxdepth 1 -name "*${MIGRATION_NAME}.cs" | head -n 1)"
if [[ -z "$DRIFT_FILE" ]]; then
  echo "[FAIL] EF did not generate a drift migration file, but pending-model-changes returned non-zero." >&2
  exit 1
fi

if grep -q "migrationBuilder\." "$DRIFT_FILE"; then
  echo "[FAIL] EF model drift detected. Review generated migration before removing it: $DRIFT_FILE" >&2
  sed -n '1,260p' "$DRIFT_FILE" >&2
  exit 1
fi

cd "$ROOT_DIR"
cleanup
trap - EXIT

if git diff --quiet -- "$MIGRATIONS_REL"; then
  echo "[OK] EF generated an empty drift migration and migration files are clean."
  exit 0
fi

echo "[FAIL] EF drift check left migration changes after cleanup." >&2
git diff -- "$MIGRATIONS_REL" >&2 || true
exit 1
