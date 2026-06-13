#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"

require() {
  if ! command -v "$1" >/dev/null 2>&1; then
    echo "[FAIL] Required command is missing: $1" >&2
    exit 127
  fi
}

require node
require npm

cd "$ROOT_DIR"
export CI="${CI:-true}"

echo "[1/6] Node/npm environment"
node --version
npm --version
npm config get registry

echo "[2/6] Frontend npm lock/config safety check"
./scripts/check-frontend-lockfile.sh

echo "[3/6] Frontend npm ci"
cd "$ROOT_DIR/frontend"
npm ci

echo "[4/6] Frontend typecheck"
npm run typecheck

echo "[5/6] Frontend build"
npm run build

echo "[6/6] Frontend unit tests and high-severity audit"
npm run test
npm audit --audit-level=high

echo "[OK] frontend validation gate completed."
