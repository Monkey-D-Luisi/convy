#!/usr/bin/env bash
set -euo pipefail

if [ "$(id -u)" -ne 0 ]; then
  exec sudo "$0" "$@"
fi

install -m 0755 -d /etc/systemd/system
install -m 0644 /opt/convy/current/ops/vps/backups/convy-backup.service /etc/systemd/system/convy-backup.service
install -m 0644 /opt/convy/current/ops/vps/backups/convy-backup.timer /etc/systemd/system/convy-backup.timer
install -m 0644 /opt/convy/current/ops/vps/backups/convy-restore-verify.service /etc/systemd/system/convy-restore-verify.service
install -m 0644 /opt/convy/current/ops/vps/backups/convy-restore-verify.timer /etc/systemd/system/convy-restore-verify.timer

systemctl daemon-reload
systemctl enable --now convy-backup.timer convy-restore-verify.timer
systemctl list-timers convy-backup.timer convy-restore-verify.timer
