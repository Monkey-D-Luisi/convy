#!/usr/bin/env bash
set -euo pipefail

if [ "$(id -u)" -ne 0 ]; then
  exec sudo "$0" "$@"
fi

install -m 0644 /opt/convy/current/ops/oci/convy-backup.service /etc/systemd/system/convy-backup.service
install -m 0644 /opt/convy/current/ops/oci/convy-backup.timer /etc/systemd/system/convy-backup.timer
systemctl daemon-reload
systemctl enable --now convy-backup.timer
systemctl list-timers convy-backup.timer
