#!/usr/bin/env bash
set -euo pipefail

DATA_DEVICE="${DATA_DEVICE:-/dev/oracleoci/oraclevdb}"
APP_ROOT="${APP_ROOT:-/opt/convy}"

if [ "$(id -u)" -ne 0 ]; then
  exec sudo --preserve-env=DATA_DEVICE,APP_ROOT "$0" "$@"
fi

if [ ! -b "$DATA_DEVICE" ]; then
  echo "Data device $DATA_DEVICE was not found. Confirm Terraform attached the OCI block volume." >&2
  exit 1
fi

systemctl enable --now docker

if ! blkid "$DATA_DEVICE" >/dev/null 2>&1; then
  mkfs.ext4 -F "$DATA_DEVICE"
fi

mkdir -p "$APP_ROOT"

if ! findmnt "$APP_ROOT" >/dev/null 2>&1; then
  if ! grep -q "$APP_ROOT" /etc/fstab; then
    echo "$DATA_DEVICE $APP_ROOT ext4 defaults,nofail 0 2" >> /etc/fstab
  fi
  mount "$APP_ROOT"
fi

mkdir -p \
  "$APP_ROOT/backups" \
  "$APP_ROOT/caddy/config" \
  "$APP_ROOT/caddy/data" \
  "$APP_ROOT/postgres" \
  "$APP_ROOT/releases" \
  "$APP_ROOT/shared"

chown -R root:root "$APP_ROOT"
chmod 755 "$APP_ROOT"
chmod 700 "$APP_ROOT/shared"
chmod 700 "$APP_ROOT/backups"

ufw allow OpenSSH
ufw allow 80/tcp
ufw allow 443/tcp
ufw --force enable

echo "Convy host bootstrap completed at $APP_ROOT"
