#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
LOCKFILE="$ROOT_DIR/frontend/package-lock.json"
NPMRC="$ROOT_DIR/frontend/.npmrc"

if [[ ! -f "$LOCKFILE" ]]; then
  echo "[FAIL] frontend/package-lock.json is missing. Run npm install/npm ci in frontend and commit the lockfile." >&2
  exit 1
fi

FORBIDDEN_PATTERN='(npm\.openai|registry\.openai|packages\.ace-research\.openai\.org|npm\.pkg\.github\.com|_authToken|always-auth|//[^[:space:]]+:_auth|//[^[:space:]]+:_password|_auth=)'

if grep -EIn "$FORBIDDEN_PATTERN" "$LOCKFILE" ${NPMRC:+"$NPMRC"} 2>/dev/null; then
  echo "[FAIL] Frontend npm lock/config contains an internal registry or private auth token." >&2
  echo "       Use the public npm registry and never commit npm auth material." >&2
  exit 1
fi

if ! grep -q 'https://registry.npmjs.org/' "$LOCKFILE"; then
  echo "[WARN] frontend/package-lock.json does not explicitly contain https://registry.npmjs.org/." >&2
  echo "       This may be fine for file/workspace-only sections, but npm ci must still resolve from public npm." >&2
fi

echo "[OK] frontend npm lock/config does not contain internal registries or private auth tokens."
