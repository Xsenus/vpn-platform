#!/usr/bin/env bash
set -euo pipefail

APP_DIR="${APP_DIR:-/opt/vpn-platform}"
KEEP_RELEASES="${KEEP_RELEASES:-5}"
LOG_RETENTION_DAYS="${LOG_RETENTION_DAYS:-14}"
ARCHIVE_RETENTION_DAYS="${ARCHIVE_RETENTION_DAYS:-7}"
JOURNAL_RETENTION_DAYS="${JOURNAL_RETENTION_DAYS:-14}"
DOCKER_PRUNE_UNTIL="${DOCKER_PRUNE_UNTIL:-168h}"
DOCKER_KEEP_STORAGE="${DOCKER_KEEP_STORAGE:-2GB}"
DRY_RUN=true
ENABLE_DOCKER_PRUNE=false

usage() {
  cat <<'USAGE'
Usage: scripts/vps-maintenance.sh [--dry-run] [--apply] [--docker-prune] [--app-dir /opt/vpn-platform]

Safe VPS maintenance for VPN Platform.

Default mode is --dry-run. Real cleanup requires --apply.

Environment:
  APP_DIR                 Application directory. Default: /opt/vpn-platform
  KEEP_RELEASES           Number of newest release directories to keep. Default: 5
  LOG_RETENTION_DAYS      App *.log retention in APP_DIR/logs. Default: 14
  ARCHIVE_RETENTION_DAYS  Stray release archive retention. Default: 7
  JOURNAL_RETENTION_DAYS  systemd journal retention. Default: 14
  DOCKER_PRUNE_UNTIL      Docker prune age filter. Default: 168h
  DOCKER_KEEP_STORAGE     Docker build cache keep-storage. Default: 2GB
USAGE
}

while [[ $# -gt 0 ]]; do
  case "$1" in
    --dry-run)
      DRY_RUN=true
      shift
      ;;
    --apply)
      DRY_RUN=false
      shift
      ;;
    --docker-prune)
      ENABLE_DOCKER_PRUNE=true
      shift
      ;;
    --app-dir)
      APP_DIR="${2:?--app-dir requires a value}"
      shift 2
      ;;
    --help|-h)
      usage
      exit 0
      ;;
    *)
      echo "[FAIL] Unknown argument: $1" >&2
      usage >&2
      exit 64
      ;;
  esac
done

require_integer() {
  local name="$1"
  local value="$2"
  if ! [[ "$value" =~ ^[0-9]+$ ]]; then
    echo "[FAIL] $name must be a non-negative integer, got: $value" >&2
    exit 64
  fi
}

resolve_path() {
  local path="$1"
  realpath -m "$path"
}

run_or_echo() {
  if [[ "$DRY_RUN" == "true" ]]; then
    printf '[dry-run]'
    printf ' %q' "$@"
    printf '\n'
  else
    "$@"
  fi
}

print_memory() {
  if command -v free >/dev/null 2>&1; then
    free -h
  else
    echo "[skip] free not found"
  fi
}

safe_rm_rf() {
  local target="$1"
  local resolved_target
  resolved_target="$(resolve_path "$target")"

  if [[ "$resolved_target" != "$RESOLVED_APP_DIR/"* ]]; then
    echo "[FAIL] Refusing to remove path outside APP_DIR: $resolved_target" >&2
    exit 65
  fi

  case "$resolved_target" in
    "$RESOLVED_APP_DIR"|"$RESOLVED_APP_DIR/"|"$RESOLVED_APP_DIR/shared"|"$RESOLVED_APP_DIR/current"|"$RESOLVED_RELEASES_DIR")
      echo "[FAIL] Refusing to remove protected path: $resolved_target" >&2
      exit 65
      ;;
  esac

  if [[ "$DRY_RUN" == "true" ]]; then
    echo "[dry-run] rm -rf $resolved_target"
  else
    rm -rf -- "$resolved_target"
  fi
}

require_integer "KEEP_RELEASES" "$KEEP_RELEASES"
require_integer "LOG_RETENTION_DAYS" "$LOG_RETENTION_DAYS"
require_integer "ARCHIVE_RETENTION_DAYS" "$ARCHIVE_RETENTION_DAYS"
require_integer "JOURNAL_RETENTION_DAYS" "$JOURNAL_RETENTION_DAYS"

if [[ "$KEEP_RELEASES" -lt 1 ]]; then
  echo "[FAIL] KEEP_RELEASES must be at least 1." >&2
  exit 64
fi

RESOLVED_APP_DIR="$(resolve_path "$APP_DIR")"
RESOLVED_RELEASES_DIR="$(resolve_path "$RESOLVED_APP_DIR/releases")"
RESOLVED_LOGS_DIR="$(resolve_path "$RESOLVED_APP_DIR/logs")"

if [[ "$RESOLVED_APP_DIR" == "/" || "$RESOLVED_APP_DIR" == "/opt" || "$RESOLVED_APP_DIR" == "/var" ]]; then
  echo "[FAIL] Refusing unsafe APP_DIR: $RESOLVED_APP_DIR" >&2
  exit 65
fi

echo "VPN Platform VPS maintenance"
echo "Mode: $([[ "$DRY_RUN" == "true" ]] && echo dry-run || echo apply)"
echo "APP_DIR: $RESOLVED_APP_DIR"
echo "KEEP_RELEASES: $KEEP_RELEASES"

echo "== Before =="
df -h "$RESOLVED_APP_DIR" 2>/dev/null || df -h
print_memory
if [[ -d "$RESOLVED_APP_DIR" ]]; then
  du -sh "$RESOLVED_APP_DIR" 2>/dev/null || true
fi
if [[ -d "$RESOLVED_RELEASES_DIR" ]]; then
  du -sh "$RESOLVED_RELEASES_DIR" 2>/dev/null || true
fi

echo "== Release cleanup =="
if [[ -d "$RESOLVED_RELEASES_DIR" ]]; then
  current_target=""
  if [[ -L "$RESOLVED_APP_DIR/current" ]]; then
    current_target="$(readlink -f "$RESOLVED_APP_DIR/current" || true)"
  fi

  index=0
  while IFS= read -r release_dir; do
    index=$((index + 1))
    release_base="$(basename "$release_dir")"
    resolved_release="$(resolve_path "$release_dir")"

    if [[ "$index" -le "$KEEP_RELEASES" ]]; then
      echo "[keep] $resolved_release"
      continue
    fi

    if [[ -n "$current_target" && "$resolved_release" == "$current_target" ]]; then
      echo "[keep-current] $resolved_release"
      continue
    fi

    if ! [[ "$release_base" =~ ^[0-9a-fA-F]{7,40}$ ]]; then
      echo "[skip] release directory name is not a git sha: $resolved_release"
      continue
    fi

    safe_rm_rf "$resolved_release"
  done < <(find "$RESOLVED_RELEASES_DIR" -mindepth 1 -maxdepth 1 -type d -printf '%T@ %p\n' | sort -rn | sed 's/^[^ ]* //')

  echo "== Stray archive cleanup =="
  while IFS= read -r archive_file; do
    if [[ "$DRY_RUN" == "true" ]]; then
      echo "[dry-run] rm -f $archive_file"
    else
      rm -f -- "$archive_file"
    fi
  done < <(find "$RESOLVED_RELEASES_DIR" -mindepth 1 -maxdepth 3 -type f \( -name 'release.tar.gz' -o -name 'systemd-release.tar.gz' \) -mtime +"$ARCHIVE_RETENTION_DAYS")
else
  echo "[skip] releases directory does not exist: $RESOLVED_RELEASES_DIR"
fi

echo "== App log cleanup =="
if [[ -d "$RESOLVED_LOGS_DIR" ]]; then
  while IFS= read -r log_file; do
    if [[ "$DRY_RUN" == "true" ]]; then
      echo "[dry-run] rm -f $log_file"
    else
      rm -f -- "$log_file"
    fi
  done < <(find "$RESOLVED_LOGS_DIR" -type f \( -name '*.log' -o -name '*.log.*' \) -mtime +"$LOG_RETENTION_DAYS")
else
  echo "[skip] app logs directory does not exist: $RESOLVED_LOGS_DIR"
fi

echo "== systemd journal =="
if command -v journalctl >/dev/null 2>&1; then
  if [[ "$DRY_RUN" == "true" ]]; then
    journalctl --disk-usage || true
    echo "[dry-run] journalctl --vacuum-time=${JOURNAL_RETENTION_DAYS}d"
  else
    journalctl --vacuum-time="${JOURNAL_RETENTION_DAYS}d" || true
  fi
else
  echo "[skip] journalctl not found"
fi

echo "== apt cache =="
if command -v apt-get >/dev/null 2>&1; then
  run_or_echo apt-get clean
  run_or_echo apt-get autoclean
else
  echo "[skip] apt-get not found"
fi

echo "== Docker cache =="
if command -v docker >/dev/null 2>&1; then
  docker system df 2>/dev/null || echo "[skip] docker daemon is not reachable"
  if [[ "$ENABLE_DOCKER_PRUNE" == "true" ]]; then
    run_or_echo docker docker builder prune -f --filter "until=$DOCKER_PRUNE_UNTIL" --keep-storage "$DOCKER_KEEP_STORAGE"
    run_or_echo docker docker image prune -f --filter "until=$DOCKER_PRUNE_UNTIL"
    run_or_echo docker docker container prune -f --filter "until=$DOCKER_PRUNE_UNTIL"
  else
    echo "[skip] Docker prune disabled. Re-run with --docker-prune to prune build cache, dangling images and stopped containers. Volumes are never pruned by this script."
  fi
else
  echo "[skip] docker not found"
fi

echo "== After =="
df -h "$RESOLVED_APP_DIR" 2>/dev/null || df -h
print_memory
if [[ -d "$RESOLVED_APP_DIR" ]]; then
  du -sh "$RESOLVED_APP_DIR" 2>/dev/null || true
fi
if [[ -d "$RESOLVED_RELEASES_DIR" ]]; then
  du -sh "$RESOLVED_RELEASES_DIR" 2>/dev/null || true
fi

echo "VPS maintenance completed."
