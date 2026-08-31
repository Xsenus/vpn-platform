#!/usr/bin/env bash
set -euo pipefail

app_dir="${1:?app directory is required}"
override_env="$app_dir/shared/production.override.env"
drop_in_dir="/etc/systemd/system/vpn-platform-api.service.d"
drop_in="$drop_in_dir/20-production-override.conf"

mkdir -p "$app_dir/shared" "$drop_in_dir"
touch "$override_env"
chmod 600 "$override_env"

temporary_env="$(mktemp)"
cleanup() {
  rm -f "$temporary_env"
}
trap cleanup EXIT

awk -F= '$1 != "Vpn__X3Ui__Mode"' "$override_env" > "$temporary_env"
printf '%s\n' 'Vpn__X3Ui__Mode=Production' >> "$temporary_env"
install -m 600 "$temporary_env" "$override_env"

cat > "$drop_in" <<EOF
[Service]
EnvironmentFile=-$override_env
EOF

systemctl daemon-reload
systemctl restart vpn-platform-api

for _ in $(seq 1 60); do
  if curl -fsS http://127.0.0.1:8080/health/ready >/dev/null; then
    echo "Production VPN mode configured and API readiness passed."
    exit 0
  fi
  sleep 2
done

systemctl status vpn-platform-api --no-pager >&2 || true
echo "API readiness failed after enabling production VPN mode." >&2
exit 1

