#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
PYTHON_BIN="${PYTHON_BIN:-python3}"

if [ "$#" -lt 4 ]; then
  echo "Usage: $0 <precheck|provision> <host> <ssh-user> <ssh-port> [private-key-path] [workdir]" >&2
  exit 1
fi

MODE="$1"
HOST="$2"
SSH_USER="$3"
SSH_PORT="$4"
PRIVATE_KEY_PATH="${5:-}"

case "$MODE" in
  precheck)
    PLAYBOOK="$ROOT_DIR/infra/ansible/playbooks/precheck-node.yml"
    EXTRA_ARGS=(--check)
    ;;
  provision)
    PLAYBOOK="$ROOT_DIR/infra/ansible/playbooks/provision-node.yml"
    EXTRA_ARGS=()
    ;;
  *)
    echo "Unknown mode: $MODE" >&2
    exit 1
    ;;
esac

if [ "$#" -ge 6 ] && [ -n "${6:-}" ]; then
  WORKDIR="$6"
else
  WORKDIR="$(mktemp -d "${TMPDIR:-/tmp}/vpnplatform-manual-$MODE.XXXXXX")"
  cleanup_workdir() {
    rm -rf "$WORKDIR"
  }
  trap cleanup_workdir EXIT
fi

CMD=("$PYTHON_BIN" "$ROOT_DIR/infra/ansible/runner/run_playbook.py" --playbook "$PLAYBOOK" --host "$HOST" --ssh-user "$SSH_USER" --ssh-port "$SSH_PORT" --workdir "$WORKDIR" --skip-host-key-checking)

if [ -n "$PRIVATE_KEY_PATH" ]; then
  CMD+=(--private-key-path "$PRIVATE_KEY_PATH")
fi

CMD+=("${EXTRA_ARGS[@]}")

printf 'Running: %s\n' "${CMD[*]}"
"${CMD[@]}"
