#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
CONFIGURATION="${CONFIGURATION:-Release}"
TEST_RESULTS_DIR="${TEST_RESULTS_DIR:-$ROOT_DIR/backend/TestResults}"

require() {
  if ! command -v "$1" >/dev/null 2>&1; then
    echo "[FAIL] Required command is missing: $1" >&2
    exit 127
  fi
}

run() {
  echo "+ $*"
  "$@"
}

cd "$ROOT_DIR"
./scripts/check-validation-safety.sh

require dotnet
require git

# Safe validation defaults. These intentionally disable live side effects during restore/build/test/EF checks.
export ASPNETCORE_ENVIRONMENT="${ASPNETCORE_ENVIRONMENT:-Development}"
export ConnectionStrings__DefaultConnection="${ConnectionStrings__DefaultConnection:-Host=localhost;Port=5432;Database=vpnplatform_validation;Username=vpnplatform;Password=vpnplatform}"
export Jwt__Issuer="${Jwt__Issuer:-vpn-platform}"
export Jwt__Audience="${Jwt__Audience:-vpn-platform}"
export Jwt__SigningKey="${Jwt__SigningKey:-local-validation-signing-key-0000000000000000000000}"
export Security__SecretEncryptionKey="${Security__SecretEncryptionKey:-local-validation-secret-encryption-key-000000000000000000}"
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

if [[ -f global.json ]]; then
  echo "[info] global.json: $(tr -d '\n' < global.json)"
fi

echo "[1/7] .NET environment"
run dotnet --info

echo "[2/7] Restore backend solution"
run dotnet restore backend/VpnPlatform.sln

echo "[3/7] Build backend solution"
run dotnet build backend/VpnPlatform.sln --configuration "$CONFIGURATION" --no-restore

echo "[4/7] Run backend tests"
mkdir -p "$TEST_RESULTS_DIR"
run dotnet test backend/VpnPlatform.sln \
  --configuration "$CONFIGURATION" \
  --no-build \
  --logger "trx;LogFileName=test-results.trx" \
  --results-directory "$TEST_RESULTS_DIR"

echo "[5/7] Restore dotnet local tools"
cd "$ROOT_DIR/backend"
run dotnet tool restore

echo "[6/7] List EF migrations without opening a database connection"
run dotnet ef migrations list \
  --project src/VpnPlatform.Infrastructure \
  --startup-project src/VpnPlatform.Api \
  --context ApplicationDbContext \
  --no-connect

cd "$ROOT_DIR"
echo "[7/7] EF model drift check"
run ./scripts/check-ef-drift.sh

echo "[OK] backend validation gate completed. Results: $TEST_RESULTS_DIR"
