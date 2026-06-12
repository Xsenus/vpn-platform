#!/usr/bin/env bash
set -euo pipefail

: "${BACKUP_FILE:?Set BACKUP_FILE to a .dump file created by scripts/backup-db.sh}"
: "${RESTORE_DATABASE_URL:?Set RESTORE_DATABASE_URL for the target restore database}"

RESTORE_ALLOW_DATABASE_URL_MATCH="${RESTORE_ALLOW_DATABASE_URL_MATCH:-false}"

if ! command -v pg_restore >/dev/null 2>&1; then
  echo "[FAIL] pg_restore is required for PostgreSQL restore." >&2
  exit 127
fi

if [[ ! -f "$BACKUP_FILE" ]]; then
  echo "[FAIL] Backup file not found: $BACKUP_FILE" >&2
  exit 1
fi

if [[ -n "${DATABASE_URL:-}" && "$RESTORE_DATABASE_URL" == "$DATABASE_URL" && "$RESTORE_ALLOW_DATABASE_URL_MATCH" != "true" ]]; then
  echo "[FAIL] RESTORE_DATABASE_URL matches DATABASE_URL. Restore to a separate DB or set RESTORE_ALLOW_DATABASE_URL_MATCH=true intentionally." >&2
  exit 1
fi

pg_restore \
  --clean \
  --if-exists \
  --no-owner \
  --no-privileges \
  --dbname "$RESTORE_DATABASE_URL" \
  "$BACKUP_FILE"

echo "[OK] Restored PostgreSQL backup into target database."
