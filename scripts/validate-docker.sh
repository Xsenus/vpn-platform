#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
API_BASE_URL="${API_BASE_URL:-http://localhost:8080}"
BOT_BASE_URL="${BOT_BASE_URL:-http://localhost:8081}"
KEEP_STACK="${KEEP_STACK:-0}"
BUILD_SERVICES=(backend-api telegram-bot public-web cabinet admin-panel)
SMOKE_SERVICES=(postgres redis rabbitmq backend-api telegram-bot)
TMP_DIR="$(mktemp -d "${TMPDIR:-/tmp}/vpnplatform-validate-docker.XXXXXX")"
CURL_OUTPUT_FILE="$TMP_DIR/curl-output.txt"
CURL_ERROR_FILE="$TMP_DIR/curl-error.txt"
COMPOSE_CONFIG_FILE="$TMP_DIR/compose-config.yml"
RUNTIME_LOG_FILE="$TMP_DIR/runtime-logs.txt"

require() {
  if ! command -v "$1" >/dev/null 2>&1; then
    echo "[FAIL] Required command is missing: $1" >&2
    exit 127
  fi
}

compose() {
  docker compose -f "$ROOT_DIR/docker-compose.yml" -f "$ROOT_DIR/docker-compose.validation.yml" "$@"
}

run() {
  echo "+ $*"
  "$@"
}

run_compose() {
  echo "+ docker compose -f docker-compose.yml -f docker-compose.validation.yml $*"
  compose "$@"
}

redacted_state() {
  local value="${1:-}"
  if [[ -z "$value" ]]; then
    echo "<empty>"
  else
    echo "<set-redacted>"
  fi
}

wait_url() {
  local name="$1"
  local url="$2"
  local attempts="${3:-60}"
  local delay="${4:-2}"

  echo "[wait] $name $url"
  for ((i = 1; i <= attempts; i++)); do
    if curl -fsS "$url" >"$CURL_OUTPUT_FILE" 2>"$CURL_ERROR_FILE"; then
      cat "$CURL_OUTPUT_FILE"
      echo
      return 0
    fi
    if (( i == attempts )); then
      echo "[FAIL] $name did not become healthy after $attempts attempts: $url" >&2
      echo "[last curl stderr]" >&2
      cat "$CURL_ERROR_FILE" >&2 || true
      echo "[compose ps]" >&2
      compose ps >&2 || true
      echo "[recent logs]" >&2
      compose logs --tail=200 backend-api telegram-bot >&2 || true
      return 1
    fi
    sleep "$delay"
  done
}

cleanup() {
  if [[ "$KEEP_STACK" != "1" ]]; then
    echo "[cleanup] docker compose down"
    compose down --remove-orphans >/dev/null 2>&1 || true
  fi

  if [[ -n "${TMP_DIR:-}" && -d "$TMP_DIR" ]]; then
    rm -rf "$TMP_DIR"
  fi
}

trap cleanup EXIT

cd "$ROOT_DIR"
./scripts/check-validation-safety.sh

require docker
require curl

if [[ ! -f "$ROOT_DIR/docker-compose.validation.yml" ]]; then
  echo "[FAIL] Missing docker-compose.validation.yml safe override file." >&2
  exit 1
fi

# Safe validation defaults. The compose validation override also injects these into containers.
# They are repeated here for variable interpolation and manual visibility.
export ASPNETCORE_ENVIRONMENT="Development"
export Database__ApplyMigrationsOnStartup="true"
export Database__SeedDemoData="true"
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
export Vpn__X3Ui__Mode="Sandbox"
export X3UI_BASE_URL=""
export X3UI_USERNAME=""
export X3UI_PASSWORD=""
export Payments__YooMoney__Mode="Disabled"
export Payments__YooKassa__Mode="Disabled"
export Payments__RoboKassa__Mode="Disabled"
export Payments__TelegramStars__Mode="Disabled"
export Payments__CloudPayments__Mode="Disabled"
export Payments__TBankAcquiring__Mode="Disabled"
export Payments__Prodamus__Mode="Disabled"
export Payments__Stripe__Mode="Disabled"
export Payments__PayPal__Mode="Disabled"

echo "[1/10] Docker environment"
run docker --version
run_compose version

echo "[security] validation mode keeps live integrations disabled"
echo "  TelegramBot__Enabled=${TelegramBot__Enabled}"
echo "  AdminBootstrap__Enabled=${AdminBootstrap__Enabled}"
echo "  Vpn__X3Ui__Mode=${Vpn__X3Ui__Mode}"
echo "  X3UI_BASE_URL=$(redacted_state "$X3UI_BASE_URL")"
echo "  X3UI_USERNAME=$(redacted_state "$X3UI_USERNAME")"
echo "  X3UI_PASSWORD=$(redacted_state "$X3UI_PASSWORD")"
echo "  payment providers: Disabled"

echo "[2/10] Docker compose config"
run_compose config >"$COMPOSE_CONFIG_FILE"
wc -l "$COMPOSE_CONFIG_FILE"

echo "[3/10] Docker compose build ${BUILD_SERVICES[*]}"
run_compose build "${BUILD_SERVICES[@]}"

echo "[4/10] Docker compose up -d ${SMOKE_SERVICES[*]}"
run_compose up -d "${SMOKE_SERVICES[@]}"

echo "[5/10] Docker compose ps"
run_compose ps

echo "[6/10] API runtime health"
wait_url "API live health" "$API_BASE_URL/health/live"
wait_url "API ready health" "$API_BASE_URL/health/ready"
wait_url "API metrics" "$API_BASE_URL/metrics"

echo "[7/10] Telegram bot runtime health"
wait_url "Telegram bot live health" "$BOT_BASE_URL/health/live"
wait_url "Telegram bot ready health" "$BOT_BASE_URL/health/ready"

echo "[8/10] Dependency health checks"
run_compose exec -T postgres pg_isready -U "${POSTGRES_USER:-vpnplatform}" -d "${POSTGRES_DB:-vpnplatform}"
run_compose exec -T redis redis-cli ping
run_compose exec -T rabbitmq rabbitmq-diagnostics -q ping

echo "[9/10] Runtime logs"
run_compose logs --tail=250 backend-api telegram-bot > "$RUNTIME_LOG_FILE"
cat "$RUNTIME_LOG_FILE"

echo "[10/10] Fatal log scan"
if grep -Eiq "fatal|unhandled exception|application startup exception|host terminated unexpectedly" "$RUNTIME_LOG_FILE"; then
  echo "[FAIL] fatal-looking runtime log entries detected:" >&2
  grep -Ein "fatal|unhandled exception|application startup exception|host terminated unexpectedly" "$RUNTIME_LOG_FILE" >&2
  exit 1
fi

echo "[OK] docker validation gate completed."
if [[ "$KEEP_STACK" == "1" ]]; then
  echo "[info] KEEP_STACK=1, leaving compose stack running. Stop it with: docker compose -f docker-compose.yml -f docker-compose.validation.yml down --remove-orphans"
fi
