#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "$0")/.." && pwd)"
cd "$ROOT_DIR"
PYTHON_BIN="${PYTHON_BIN:-python3}"

echo '[1/6] backend restore/build/test when .NET SDK is available'
if command -v dotnet >/dev/null 2>&1; then
  (cd backend && dotnet restore && dotnet build --no-restore && dotnet test --no-build)
else
  echo 'dotnet SDK not found; skipping backend compile/test in this environment.'
fi

echo '[2/6] frontend install + typecheck + tests + build'
cd frontend
npm install
npm run typecheck
npm run test
npm run build
cd "$ROOT_DIR"

echo '[3/6] python unit tests for ansible runner'
"$PYTHON_BIN" -m unittest discover -s infra/ansible/runner/tests -v

echo '[4/6] ansible syntax check when ansible-playbook is available'
if command -v ansible-playbook >/dev/null 2>&1; then
  TMP_DIR="$(mktemp -d)"
  cleanup_ansible_tmp() {
    rm -rf "$TMP_DIR"
  }
  trap cleanup_ansible_tmp EXIT
  printf '[vpn_nodes]\nlocal ansible_connection=local ansible_python_interpreter=/usr/bin/python3\n' > "$TMP_DIR/inventory.ini"
  ansible-playbook --syntax-check -i "$TMP_DIR/inventory.ini" infra/ansible/playbooks/precheck-node.yml
  ansible-playbook --syntax-check -i "$TMP_DIR/inventory.ini" infra/ansible/playbooks/provision-node.yml
else
  echo 'ansible-playbook not found; skipping ansible syntax check in this environment.'
fi

echo '[5/6] yaml/json sanity checks'
"$PYTHON_BIN" - <<'PY'
from pathlib import Path
import json
for path in [
    Path('backend/src/VpnPlatform.Api/appsettings.json'),
    Path('backend/src/VpnPlatform.Api/appsettings.Development.json'),
    Path('backend/src/VpnPlatform.Api/appsettings.Production.example.json'),
    Path('.config/dotnet-tools.json'),
]:
    json.loads(path.read_text(encoding='utf-8'))
print('JSON checks passed.')
PY

if command -v ruby >/dev/null 2>&1; then
  ruby -e 'require "yaml"; ARGV.each { |p| YAML.unsafe_load_file(p); puts "yaml ok #{p}" }' \
    infra/prometheus/prometheus.yml \
    infra/grafana/provisioning/datasources/datasource.yml \
    infra/loki/loki-config.yml \
    infra/ansible/playbooks/precheck-node.yml \
    infra/ansible/playbooks/provision-node.yml
else
  "$PYTHON_BIN" - <<'PY'
from pathlib import Path
try:
    import yaml
except Exception as exc:
    raise SystemExit('Neither ruby nor PyYAML is available for YAML checks.') from exc
for path in [Path('infra/prometheus/prometheus.yml'), Path('infra/grafana/provisioning/datasources/datasource.yml'), Path('infra/loki/loki-config.yml'), Path('infra/ansible/playbooks/precheck-node.yml'), Path('infra/ansible/playbooks/provision-node.yml')]:
    yaml.safe_load(path.read_text(encoding='utf-8'))
print('YAML checks passed.')
PY
fi

echo '[6/6] production safety grep'
if grep -RInE 'admin123456|admin@example.com|EnsureCreated|unsafe-dev-key|checkout\.example|node\.example|replace-me|queued_for_manual_recheck|refund_requested|backend-driven' \
  backend frontend infra scripts .env.example docker-compose.yml \
  --exclude-dir=dist --exclude=package-lock.json --exclude=validate_repo.sh; then
  echo 'Unsafe placeholder grep found matches above.' >&2
  exit 1
fi

echo '[completed] repository validation finished'
