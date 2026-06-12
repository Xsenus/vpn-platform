#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"

is_skipped_path() {
  local path="${1#$ROOT_DIR/}"
  case "$path" in
    .git/*|.serena/*|.playwright-mcp/*|*/node_modules/*|node_modules/*|*/bin/*|bin/*|*/obj/*|obj/*|*/dist/*|dist/*|*/build/*|build/*|*/TestResults/*|TestResults/*|*/artifacts/*|artifacts/*|*/coverage/*|coverage/*|*/playwright-report/*|playwright-report/*|*/backups/*|backups/*)
      return 0
      ;;
  esac
  return 1
}

is_text_candidate() {
  local file="$1"
  local name
  name="$(basename "$file")"
  case "$name" in
    .env.example|.gitignore|Dockerfile|docker-compose.yml|docker-compose.validation.yml|Dockerfile*)
      return 0
      ;;
  esac
  case "$file" in
    *.cs|*.csproj|*.json|*.md|*.ps1|*.sh|*.ts|*.tsx|*.js|*.jsx|*.css|*.html|*.yml|*.yaml|*.env|*.example|*.config|*.conf|*.log|*.txt|*.sql)
      return 0
      ;;
  esac
  return 1
}

is_allowed_fixture() {
  local relative="$1"
  local line="$2"
  case "$relative" in
    backend/tests/*|frontend/tests/*|scripts/scan-secrets.ps1|scripts/scan-secrets.sh)
      return 0
      ;;
  esac
  if printf '%s' "$line" | grep -Eiq '(placeholder|example|change-me|local-dev|local-validation|schema-audit|ef-drift|dummy|fixture|must-not-leak|redacted)'; then
    return 0
  fi
  return 1
}

scan_line() {
  local relative="$1"
  local line_number="$2"
  local line="$3"
  local pattern_name=''

  if printf '%s' "$line" | grep -Eq '\b[0-9]{8,10}:AA[A-Za-z0-9_-]{30,}\b'; then
    pattern_name='Telegram bot token'
  elif printf '%s' "$line" | grep -Eq '\b(sk|rk|pk)_(live|test)_[A-Za-z0-9]{16,}\b|\bsk-(proj-|svcacct-)?[A-Za-z0-9_-]{32,}\b'; then
    pattern_name='Stripe/OpenAI style API key'
  elif printf '%s' "$line" | grep -Eq '\bgh[pousr]_[A-Za-z0-9_]{30,}\b'; then
    pattern_name='GitHub token'
  elif printf '%s' "$line" | grep -Eq '\bglpat-[A-Za-z0-9_-]{20,}\b'; then
    pattern_name='GitLab token'
  elif printf '%s' "$line" | grep -Eq '\bAKIA[0-9A-Z]{16}\b'; then
    pattern_name='AWS access key'
  elif printf '%s' "$line" | grep -Eq '\bAIza[0-9A-Za-z_-]{35}\b'; then
    pattern_name='Google API key'
  elif printf '%s' "$line" | grep -Eq '\bxox[baprs]-[A-Za-z0-9-]{20,}\b'; then
    pattern_name='Slack token'
  elif printf '%s' "$line" | grep -Eq -- '-----BEGIN (RSA |OPENSSH |EC |DSA )?PRIVATE KEY-----'; then
    pattern_name='Private key PEM'
  fi

  if [[ -n "$pattern_name" ]] && ! is_allowed_fixture "$relative" "$line"; then
    printf '%s:%s: %s\n' "$relative" "$line_number" "$pattern_name"
  fi
}

findings_file="$(mktemp)"
trap 'rm -f "$findings_file"' EXIT
scanned=0

while IFS= read -r -d '' file; do
  if is_skipped_path "$file" || ! is_text_candidate "$file"; then
    continue
  fi
  relative="${file#$ROOT_DIR/}"
  scanned=$((scanned + 1))
  line_number=0
  while IFS= read -r line || [[ -n "$line" ]]; do
    line_number=$((line_number + 1))
    scan_line "$relative" "$line_number" "$line" >> "$findings_file"
  done < "$file"
done < <(find "$ROOT_DIR" -type f -print0)

if [[ -s "$findings_file" ]]; then
  echo "[FAIL] Secret scan failed:" >&2
  cat "$findings_file" >&2
  exit 1
fi

echo "[OK] secret scan completed. Files scanned: $scanned. Findings: 0."
