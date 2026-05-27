#!/usr/bin/env bash
set -euo pipefail

: "${DATABASE_URL:?Set DATABASE_URL, for example postgres://user:pass@host:5432/vpnplatform}"
BACKUP_DIR="${BACKUP_DIR:-./backups/db}"
mkdir -p "$BACKUP_DIR"
STAMP="$(date -u +%Y%m%dT%H%M%SZ)"
OUT="$BACKUP_DIR/vpnplatform-$STAMP.dump"

pg_dump --format=custom --no-owner --no-privileges --file "$OUT" "$DATABASE_URL"
printf '%s\n' "$OUT"
