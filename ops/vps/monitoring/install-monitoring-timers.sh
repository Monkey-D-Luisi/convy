#!/usr/bin/env bash
set -euo pipefail

if [ "$(id -u)" -ne 0 ]; then
  exec sudo "$0" "$@"
fi

install -m 0755 -d /etc/systemd/system
chmod 0755 /opt/convy/current/ops/vps/monitoring/check-health.sh
install -m 0644 /opt/convy/current/ops/vps/monitoring/convy-health-check.service /etc/systemd/system/convy-health-check.service
install -m 0644 /opt/convy/current/ops/vps/monitoring/convy-health-check.timer /etc/systemd/system/convy-health-check.timer

systemctl daemon-reload
systemctl enable --now convy-health-check.timer
systemctl list-timers convy-health-check.timer
