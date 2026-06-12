#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
PYTHON_BIN="${PYTHON_BIN:-python3}"
CONFIGURATION="${CONFIGURATION:-Release}"

require() {
  if ! command -v "$1" >/dev/null 2>&1; then
    echo "[FAIL] Required command is missing: $1" >&2
    exit 127
  fi
}

require dotnet
require npm
require "$PYTHON_BIN"
require git

cd "$ROOT_DIR"
export ASPNETCORE_ENVIRONMENT="${ASPNETCORE_ENVIRONMENT:-Development}"
export ConnectionStrings__DefaultConnection="${ConnectionStrings__DefaultConnection:-Host=localhost;Port=5432;Database=vpnplatform_validation;Username=vpnplatform;Password=vpnplatform}"
export Jwt__Issuer="${Jwt__Issuer:-vpn-platform}"
export Jwt__Audience="${Jwt__Audience:-vpn-platform}"
export Jwt__SigningKey="${Jwt__SigningKey:-local-validation-signing-key-0000000000000000000000}"
export Security__SecretEncryptionKey="${Security__SecretEncryptionKey:-local-validation-secret-encryption-key-000000000000000000}"
export Database__ApplyMigrationsOnStartup="${Database__ApplyMigrationsOnStartup:-false}"
export Database__SeedDemoData="${Database__SeedDemoData:-false}"
export AdminBootstrap__Enabled="${AdminBootstrap__Enabled:-false}"
export Vpn__X3Ui__Mode="${Vpn__X3Ui__Mode:-Sandbox}"

if [[ -f global.json ]]; then
  echo "[info] global.json: $(tr -d '\n' < global.json)"
fi

echo "[1/13] dotnet restore"
dotnet restore backend/VpnPlatform.sln

echo "[secret-scan] repository secret scan"
bash ./scripts/scan-secrets.sh

echo "[2/13] dotnet build"
dotnet build backend/VpnPlatform.sln --configuration "$CONFIGURATION" --no-restore

echo "[3/13] dotnet test"
mkdir -p "$ROOT_DIR/backend/TestResults"
dotnet test backend/VpnPlatform.sln \
  --configuration "$CONFIGURATION" \
  --no-build \
  --logger "trx;LogFileName=test-results.trx" \
  --results-directory "$ROOT_DIR/backend/TestResults"

echo "[4/13] dotnet tool restore"
cd "$ROOT_DIR/backend"
dotnet tool restore

echo "[5/13] dotnet ef migrations list"
dotnet ef migrations list \
  --project src/VpnPlatform.Infrastructure \
  --startup-project src/VpnPlatform.Api \
  --context ApplicationDbContext \
  --no-connect

cd "$ROOT_DIR"
echo "[6/13] EF model drift check"
./scripts/check-ef-drift.sh

echo "[7/13] frontend npm lock/config safety check"
./scripts/check-frontend-lockfile.sh

echo "[8/13] frontend npm ci"
cd "$ROOT_DIR/frontend"
npm ci

echo "[9/13] frontend typecheck"
npm run typecheck

echo "[10/13] frontend build"
npm run build

echo "[11/13] frontend test"
npm run test

echo "[12/13] provisioning runner tests"
cd "$ROOT_DIR"
"$PYTHON_BIN" -m unittest discover -s infra/ansible/runner/tests -v

echo "[13/13] optional Ansible syntax check"
if command -v ansible-playbook >/dev/null 2>&1; then
  tmp_dir="$(mktemp -d)"
  trap 'rm -rf "$tmp_dir"' EXIT
  printf '[vpn_nodes]\nlocal ansible_connection=local ansible_python_interpreter=/usr/bin/python3\n' > "$tmp_dir/inventory.ini"
  ansible-playbook --syntax-check -i "$tmp_dir/inventory.ini" infra/ansible/playbooks/precheck-node.yml
  ansible-playbook --syntax-check -i "$tmp_dir/inventory.ini" infra/ansible/playbooks/provision-node.yml
else
  echo "[SKIP] ansible-playbook is not installed; syntax check is optional locally and is enforced in CI."
fi

echo "[OK] validate-all completed."
