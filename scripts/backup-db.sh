#!/usr/bin/env bash
set -euo pipefail

: "${DATABASE_URL:?Set DATABASE_URL, for example postgres://user:pass@host:5432/vpnplatform}"
BACKUP_DIR="${BACKUP_DIR:-./backups/db}"
BACKUP_RETENTION_DAYS="${BACKUP_RETENTION_DAYS:-14}"
mkdir -p "$BACKUP_DIR"
STAMP="$(date -u +%Y%m%dT%H%M%SZ)"
OUT="$BACKUP_DIR/vpnplatform-$STAMP.dump"

if ! command -v pg_dump >/dev/null 2>&1; then
  echo "[FAIL] pg_dump is required for PostgreSQL backups." >&2
  exit 127
fi

pg_dump --format=custom --no-owner --no-privileges --file "$OUT" "$DATABASE_URL"

if command -v pg_restore >/dev/null 2>&1; then
  pg_restore --list "$OUT" > "$OUT.list"
fi

if [[ "$BACKUP_RETENTION_DAYS" =~ ^[0-9]+$ ]] && [[ "$BACKUP_RETENTION_DAYS" -gt 0 ]]; then
  find "$BACKUP_DIR" -type f \( -name 'vpnplatform-*.dump' -o -name 'vpnplatform-*.dump.list' \) -mtime +"$BACKUP_RETENTION_DAYS" -delete
fi

printf '%s\n' "$OUT"
