#!/usr/bin/env bash
set -euo pipefail

APP_ROOT="${APP_ROOT:-/opt/convy}"
DATA_DEVICE="${DATA_DEVICE:-}"

if [ "$(id -u)" -ne 0 ]; then
  exec sudo --preserve-env=APP_ROOT,DATA_DEVICE "$0" "$@"
fi

if ! command -v docker >/dev/null 2>&1; then
  install -m 0755 -d /etc/apt/keyrings
  curl -fsSL https://download.docker.com/linux/ubuntu/gpg -o /etc/apt/keyrings/docker.asc
  chmod a+r /etc/apt/keyrings/docker.asc
  /bin/sh -c 'echo "deb [arch=$(dpkg --print-architecture) signed-by=/etc/apt/keyrings/docker.asc] https://download.docker.com/linux/ubuntu $(. /etc/os-release && echo "$VERSION_CODENAME") stable" > /etc/apt/sources.list.d/docker.list'
  apt-get update
  apt-get install -y docker-ce docker-ce-cli containerd.io docker-buildx-plugin docker-compose-plugin
fi

systemctl enable --now docker

if [ -n "$DATA_DEVICE" ]; then
  if [ ! -b "$DATA_DEVICE" ]; then
    echo "Data device $DATA_DEVICE was not found." >&2
    exit 1
  fi

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
else
  mkdir -p "$APP_ROOT"
fi

mkdir -p \
  "$APP_ROOT/backups" \
  "$APP_ROOT/backups/postgres/daily" \
  "$APP_ROOT/backups/postgres/weekly" \
  "$APP_ROOT/backups/postgres/monthly" \
  "$APP_ROOT/backups/postgres/metadata" \
  "$APP_ROOT/caddy/config" \
  "$APP_ROOT/caddy/data" \
  "$APP_ROOT/legal" \
  "$APP_ROOT/postgres" \
  "$APP_ROOT/releases" \
  "$APP_ROOT/shared"

chown -R root:root "$APP_ROOT"
chmod 755 "$APP_ROOT"
chmod 700 "$APP_ROOT/shared"
chmod 700 "$APP_ROOT/backups"
chmod 755 "$APP_ROOT/legal"

ufw allow OpenSSH
ufw allow 80/tcp
ufw allow 443/tcp
ufw --force enable

echo "Convy VPS bootstrap completed at $APP_ROOT"
