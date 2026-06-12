#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
COMPOSE_FILE="$ROOT_DIR/docker-compose.validation.yml"
WORKFLOW_FILE="$ROOT_DIR/.github/workflows/staging-validation.yml"
ENV_EXAMPLE="$ROOT_DIR/.env.example"
VALIDATE_BACKEND="$ROOT_DIR/scripts/validate-backend.sh"
VALIDATE_DOCKER="$ROOT_DIR/scripts/validate-docker.sh"
CHECK_EF_DRIFT="$ROOT_DIR/scripts/check-ef-drift.sh"
CHECK_EF_DRIFT_PS1="$ROOT_DIR/scripts/check-ef-drift.ps1"
SCAN_SECRETS="$ROOT_DIR/scripts/scan-secrets.sh"
LIVE_SECRET_PATTERN='([0-9]{9,}:AA[A-Za-z0-9_-]{20,}|sk_live_|pk_live_|xox[baprs]-|AKIA[0-9A-Z]{16}|ghp_[A-Za-z0-9_]{20,}|glpat-[A-Za-z0-9_-]{20,}|ya29\.[A-Za-z0-9_-]{20,}|-----BEGIN[[:space:]]+(RSA|OPENSSH|EC)?[[:space:]]*PRIVATE KEY-----)'

fail() {
  echo "[FAIL] $*" >&2
  exit 1
}

ok() {
  echo "[OK] $*"
}

require_file() {
  [[ -f "$1" ]] || fail "Missing required validation safety file: ${1#$ROOT_DIR/}"
}

expect_text() {
  local file="$1"
  local pattern="$2"
  local description="$3"
  if ! grep -Eq "$pattern" "$file"; then
    fail "${description} is not enforced in ${file#$ROOT_DIR/}"
  fi
  ok "${description}"
}

reject_text() {
  local file="$1"
  local pattern="$2"
  local description="$3"
  if grep -Eq "$pattern" "$file"; then
    fail "${description} found in ${file#$ROOT_DIR/}"
  fi
  ok "${description} not present"
}

require_file "$COMPOSE_FILE"
require_file "$WORKFLOW_FILE"
require_file "$ENV_EXAMPLE"
require_file "$VALIDATE_BACKEND"
require_file "$VALIDATE_DOCKER"
require_file "$CHECK_EF_DRIFT"
require_file "$CHECK_EF_DRIFT_PS1"
require_file "$SCAN_SECRETS"

# Compose validation override must keep live integrations off inside containers.
expect_text "$COMPOSE_FILE" 'TelegramBot__Enabled:[[:space:]]*"?false"?' 'compose disables Telegram bot runtime'
expect_text "$COMPOSE_FILE" 'AdminBootstrap__Enabled:[[:space:]]*"?false"?' 'compose disables admin bootstrap'
expect_text "$COMPOSE_FILE" 'Auth__PasswordReset__ReturnTokenForValidation:[[:space:]]*"?false"?' 'compose disables password-reset validation token return'
expect_text "$COMPOSE_FILE" 'Provisioning__LiveExecutionEnabled:[[:space:]]*"?false"?' 'compose disables live provisioning execution'
expect_text "$COMPOSE_FILE" 'Provisioning__AllowLiveDeploy:[[:space:]]*"?false"?' 'compose disables live provisioning deploy'
expect_text "$COMPOSE_FILE" 'Vpn__X3Ui__Mode:[[:space:]]*Sandbox' 'compose uses x3-ui sandbox mode'
expect_text "$COMPOSE_FILE" 'X3UI_BASE_URL:[[:space:]]*""' 'compose clears X3UI_BASE_URL'
expect_text "$COMPOSE_FILE" 'X3UI_USERNAME:[[:space:]]*""' 'compose clears X3UI_USERNAME'
expect_text "$COMPOSE_FILE" 'X3UI_PASSWORD:[[:space:]]*""' 'compose clears X3UI_PASSWORD'
for provider in YooMoney YooKassa RoboKassa TelegramStars CloudPayments TBankAcquiring Prodamus Stripe PayPal; do
  expect_text "$COMPOSE_FILE" "Payments__${provider}__Mode:[[:space:]]*Disabled" "compose disables ${provider} payments"
done

# GitHub workflow validation env must also stay safe.
expect_text "$WORKFLOW_FILE" 'TelegramBot__Enabled:[[:space:]]*"false"' 'workflow disables Telegram bot runtime'
expect_text "$WORKFLOW_FILE" 'AdminBootstrap__Enabled:[[:space:]]*"false"' 'workflow disables admin bootstrap'
expect_text "$WORKFLOW_FILE" 'Auth__PasswordReset__ReturnTokenForValidation:[[:space:]]*"false"' 'workflow disables password-reset validation token return'
expect_text "$WORKFLOW_FILE" 'Provisioning__LiveExecutionEnabled:[[:space:]]*"false"' 'workflow disables live provisioning execution'
expect_text "$WORKFLOW_FILE" 'Provisioning__AllowLiveDeploy:[[:space:]]*"false"' 'workflow disables live provisioning deploy'
expect_text "$WORKFLOW_FILE" 'Vpn__X3Ui__Mode:[[:space:]]*Sandbox' 'workflow uses x3-ui sandbox mode'
expect_text "$WORKFLOW_FILE" 'X3UI_BASE_URL:[[:space:]]*""' 'workflow clears X3UI_BASE_URL'
expect_text "$WORKFLOW_FILE" 'X3UI_USERNAME:[[:space:]]*""' 'workflow clears X3UI_USERNAME'
expect_text "$WORKFLOW_FILE" 'X3UI_PASSWORD:[[:space:]]*""' 'workflow clears X3UI_PASSWORD'
for provider in YooMoney YooKassa RoboKassa TelegramStars CloudPayments TBankAcquiring Prodamus Stripe PayPal; do
  expect_text "$WORKFLOW_FILE" "Payments__${provider}__Mode:[[:space:]]*Disabled" "workflow disables ${provider} payments"
done

# .env.example must remain non-live by default and must not contain obvious real tokens.
expect_text "$ENV_EXAMPLE" '^TelegramBot__Enabled=false$' '.env.example disables Telegram by default'
expect_text "$ENV_EXAMPLE" '^AdminBootstrap__Enabled=false$' '.env.example disables admin bootstrap by default'
expect_text "$ENV_EXAMPLE" '^Auth__PasswordReset__ReturnTokenForValidation=false$' '.env.example disables password-reset validation token return by default'
expect_text "$ENV_EXAMPLE" '^Provisioning__LiveExecutionEnabled=false$' '.env.example disables live provisioning execution by default'
expect_text "$ENV_EXAMPLE" '^Provisioning__AllowLiveDeploy=false$' '.env.example disables live provisioning deploy by default'
expect_text "$ENV_EXAMPLE" '^Vpn__X3Ui__Mode=Sandbox$' '.env.example uses x3-ui sandbox mode'
expect_text "$ENV_EXAMPLE" '^X3UI_BASE_URL=$' '.env.example leaves X3UI_BASE_URL empty'
expect_text "$ENV_EXAMPLE" '^X3UI_USERNAME=$' '.env.example leaves X3UI_USERNAME empty'
expect_text "$ENV_EXAMPLE" '^X3UI_PASSWORD=$' '.env.example leaves X3UI_PASSWORD empty'
for provider in YooMoney YooKassa RoboKassa TelegramStars CloudPayments TBankAcquiring Prodamus Stripe PayPal; do
  expect_text "$ENV_EXAMPLE" "^Payments__${provider}__Mode=Disabled$" ".env.example disables ${provider} payments"
done

reject_text "$ENV_EXAMPLE" "$LIVE_SECRET_PATTERN" 'obvious live secret/token/private-key pattern'
reject_text "$COMPOSE_FILE" "$LIVE_SECRET_PATTERN" 'obvious live secret/token/private-key pattern'
reject_text "$WORKFLOW_FILE" "$LIVE_SECRET_PATTERN" 'obvious live secret/token/private-key pattern'

# Validation entry points must run this safety gate before backend/Docker/EF work.
expect_text "$VALIDATE_BACKEND" 'check-validation-safety\.sh' 'backend validation runs validation-safety gate'
expect_text "$VALIDATE_BACKEND" 'scan-secrets\.sh' 'backend validation runs secret scan'
expect_text "$VALIDATE_BACKEND" 'scan-secrets\.sh.*dotnet restore|scan-secrets\.sh' 'backend validation includes secret scan before backend work'
expect_text "$VALIDATE_DOCKER" 'check-validation-safety\.sh' 'Docker validation runs validation-safety gate'
expect_text "$CHECK_EF_DRIFT" 'check-validation-safety\.sh' 'EF drift validation runs validation-safety gate'
expect_text "$CHECK_EF_DRIFT_PS1" 'has-pending-model-changes' 'PowerShell EF drift validation checks pending model changes'
expect_text "$CHECK_EF_DRIFT_PS1" 'Database__ApplyMigrationsOnStartup[[:space:]]*=[[:space:]]*"false"' 'PowerShell EF drift validation disables auto migrations'
expect_text "$CHECK_EF_DRIFT_PS1" 'Provisioning__LiveExecutionEnabled[[:space:]]*=[[:space:]]*"false"' 'PowerShell EF drift validation disables live provisioning'
expect_text "$WORKFLOW_FILE" 'check-validation-safety\.sh' 'GitHub validation workflow runs validation-safety gate'
expect_text "$SCAN_SECRETS" 'Telegram bot token' 'secret scanner checks Telegram bot tokens'
expect_text "$SCAN_SECRETS" 'GitHub token' 'secret scanner checks GitHub tokens'
expect_text "$SCAN_SECRETS" 'Private key PEM' 'secret scanner checks private keys'

ok "validation safety checks completed"
