#!/usr/bin/env bash
set -euo pipefail

if [ "$#" -lt 4 ]; then
  echo "Usage: $0 <precheck|provision> <host> <ssh-user> <ssh-port> [private-key-path] [workdir]" >&2
  exit 1
fi

MODE="$1"
HOST="$2"
SSH_USER="$3"
SSH_PORT="$4"
PRIVATE_KEY_PATH="${5:-}"
WORKDIR="${6:-/tmp/vpnplatform-manual-$MODE}"

case "$MODE" in
  precheck)
    PLAYBOOK="infra/ansible/playbooks/precheck-node.yml"
    EXTRA_ARGS=(--check)
    ;;
  provision)
    PLAYBOOK="infra/ansible/playbooks/provision-node.yml"
    EXTRA_ARGS=()
    ;;
  *)
    echo "Unknown mode: $MODE" >&2
    exit 1
    ;;
esac

CMD=(python3 infra/ansible/runner/run_playbook.py --playbook "$PLAYBOOK" --host "$HOST" --ssh-user "$SSH_USER" --ssh-port "$SSH_PORT" --workdir "$WORKDIR" --skip-host-key-checking)

if [ -n "$PRIVATE_KEY_PATH" ]; then
  CMD+=(--private-key-path "$PRIVATE_KEY_PATH")
fi

CMD+=("${EXTRA_ARGS[@]}")

printf 'Running: %s\n' "${CMD[*]}"
"${CMD[@]}"
