#!/usr/bin/env bash
set -euo pipefail

app_dir="${1:?app directory is required}"
admin_email="${2:?admin email is required}"

IFS= read -r admin_password
if [ "${#admin_password}" -lt 16 ]; then
  echo "Admin password must contain at least 16 characters." >&2
  exit 1
fi

bootstrap_env="$(mktemp)"
cleanup() {
  if command -v shred >/dev/null 2>&1; then
    shred -u "$bootstrap_env"
  else
    rm -f "$bootstrap_env"
  fi
}
trap cleanup EXIT
chmod 600 "$bootstrap_env"

cat > "$bootstrap_env" <<EOF
AdminBootstrap__Enabled=true
AdminBootstrap__Email=$admin_email
AdminBootstrap__Password=$admin_password
AdminBootstrap__DisplayName=Production Acceptance Admin
AdminBootstrap__RolesCsv=SuperAdmin
AdminBootstrap__ResetExistingPassword=true
Database__ApplyMigrationsOnStartup=false
EOF

systemd-run --quiet --wait --pipe --collect \
  --unit="vpn-platform-admin-bootstrap-$(date +%s)" \
  --property="Type=oneshot" \
  --property="WorkingDirectory=$app_dir/api" \
  --property="EnvironmentFile=$app_dir/shared/.env" \
  --property="EnvironmentFile=$bootstrap_env" \
  "$app_dir/api/VpnPlatform.Api" admin-bootstrap

