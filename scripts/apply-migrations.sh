#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "$0")/.." && pwd)"
cd "$ROOT_DIR"

: "${ConnectionStrings__DefaultConnection:?Set ConnectionStrings__DefaultConnection for EF Core migrations}"
: "${DATABASE_URL:?Set DATABASE_URL so the pre-migration backup can be created with pg_dump}"

BACKUP_PATH="$(BACKUP_DIR="${BACKUP_DIR:-./backups/db}" ./scripts/backup-db.sh)"
echo "Created database backup: $BACKUP_PATH"

dotnet tool restore >/dev/null 2>&1 || true
dotnet ef database update \
  --project backend/src/VpnPlatform.Infrastructure/VpnPlatform.Infrastructure.csproj \
  --startup-project backend/src/VpnPlatform.Api/VpnPlatform.Api.csproj
