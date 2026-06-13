#!/usr/bin/env bash
set -euo pipefail

: "${API_BASE_URL:?Set API_BASE_URL, for example http://127.0.0.1:8080}"
: "${PUBLIC_WEB_URL:?Set PUBLIC_WEB_URL, for example http://example.com}"
: "${CABINET_WEB_URL:?Set CABINET_WEB_URL, for example http://example.com:5174}"
: "${ADMIN_WEB_URL:?Set ADMIN_WEB_URL, for example http://example.com:5175}"

RETRIES="${POST_DEPLOY_SMOKE_RETRIES:-20}"
SLEEP_SECONDS="${POST_DEPLOY_SMOKE_SLEEP_SECONDS:-3}"
TIMEOUT_SECONDS="${POST_DEPLOY_SMOKE_TIMEOUT_SECONDS:-10}"
REQUIRE_PUBLIC_PAYMENT_PROVIDERS="${REQUIRE_PUBLIC_PAYMENT_PROVIDERS:-true}"

require_integer() {
  local name="$1"
  local value="$2"
  if ! [[ "$value" =~ ^[0-9]+$ ]] || [[ "$value" -lt 1 ]]; then
    echo "[FAIL] $name must be a positive integer, got: $value" >&2
    exit 64
  fi
}

trim_url() {
  local value="$1"
  value="${value%/}"
  printf '%s' "$value"
}

fetch_with_retry() {
  local name="$1"
  local url="$2"
  local output="$3"

  for attempt in $(seq 1 "$RETRIES"); do
    if curl -fsSL --max-time "$TIMEOUT_SECONDS" "$url" -o "$output"; then
      echo "[ok] $name -> $url"
      return 0
    fi

    echo "[wait] $name attempt $attempt/$RETRIES failed: $url" >&2
    sleep "$SLEEP_SECONDS"
  done

  echo "[FAIL] $name did not become reachable: $url" >&2
  return 1
}

assert_contains() {
  local name="$1"
  local pattern="$2"
  local file="$3"

  if ! grep -Eiq "$pattern" "$file"; then
    echo "[FAIL] $name response did not match expected pattern: $pattern" >&2
    echo "Response preview:" >&2
    head -c 500 "$file" >&2 || true
    echo >&2
    return 1
  fi
}

require_integer "POST_DEPLOY_SMOKE_RETRIES" "$RETRIES"
require_integer "POST_DEPLOY_SMOKE_SLEEP_SECONDS" "$SLEEP_SECONDS"
require_integer "POST_DEPLOY_SMOKE_TIMEOUT_SECONDS" "$TIMEOUT_SECONDS"

if ! command -v curl >/dev/null 2>&1; then
  echo "[FAIL] curl is required for post-deploy smoke." >&2
  exit 127
fi

API_BASE_URL="$(trim_url "$API_BASE_URL")"
PUBLIC_WEB_URL="$(trim_url "$PUBLIC_WEB_URL")"
CABINET_WEB_URL="$(trim_url "$CABINET_WEB_URL")"
ADMIN_WEB_URL="$(trim_url "$ADMIN_WEB_URL")"
work_dir="$(mktemp -d)"
trap 'rm -rf "$work_dir"' EXIT

echo "Post-deploy smoke"
echo "API_BASE_URL=$API_BASE_URL"
echo "PUBLIC_WEB_URL=$PUBLIC_WEB_URL"
echo "CABINET_WEB_URL=$CABINET_WEB_URL"
echo "ADMIN_WEB_URL=$ADMIN_WEB_URL"

fetch_with_retry "API live health" "$API_BASE_URL/health/live" "$work_dir/live.json"
assert_contains "API live health" '"status"[[:space:]]*:[[:space:]]*"ok"' "$work_dir/live.json"

fetch_with_retry "API ready health" "$API_BASE_URL/health/ready" "$work_dir/ready.json"
assert_contains "API ready health" '"status"[[:space:]]*:[[:space:]]*"Ready"' "$work_dir/ready.json"

fetch_with_retry "API metrics" "$API_BASE_URL/metrics" "$work_dir/metrics.txt"
assert_contains "API metrics" 'vpnplatform_http_requests_total' "$work_dir/metrics.txt"

fetch_with_retry "Public payment providers" "$API_BASE_URL/api/public/payments/providers" "$work_dir/providers.json"
assert_contains "Public payment providers" '^\s*\[' "$work_dir/providers.json"
if [[ "$REQUIRE_PUBLIC_PAYMENT_PROVIDERS" == "true" ]]; then
  assert_contains "Public payment providers" '"provider"[[:space:]]*:' "$work_dir/providers.json"
fi

fetch_with_retry "Public web" "$PUBLIC_WEB_URL/" "$work_dir/public.html"
assert_contains "Public web" '<!doctype html|<div id="root"|<script[^>]+type="module"' "$work_dir/public.html"

fetch_with_retry "Cabinet web" "$CABINET_WEB_URL/" "$work_dir/cabinet.html"
assert_contains "Cabinet web" '<!doctype html|<div id="root"|<script[^>]+type="module"' "$work_dir/cabinet.html"

fetch_with_retry "Admin web" "$ADMIN_WEB_URL/" "$work_dir/admin.html"
assert_contains "Admin web" '<!doctype html|<div id="root"|<script[^>]+type="module"' "$work_dir/admin.html"

if [[ -n "${GITHUB_STEP_SUMMARY:-}" ]]; then
  {
    echo "### Post-deploy smoke"
    echo "- API live health: \`$API_BASE_URL/health/live\`"
    echo "- API ready health: \`$API_BASE_URL/health/ready\`"
    echo "- Public payment providers: \`$API_BASE_URL/api/public/payments/providers\`"
    echo "- Public web: \`$PUBLIC_WEB_URL/\`"
    echo "- Cabinet web: \`$CABINET_WEB_URL/\`"
    echo "- Admin web: \`$ADMIN_WEB_URL/\`"
    echo "- Result: passed"
  } >> "$GITHUB_STEP_SUMMARY"
fi

echo "Post-deploy smoke passed."
