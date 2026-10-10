#!/usr/bin/env bash
set -euo pipefail
if [[ -e /run/lock/shared-staging-deployment.lock || -e /etc/convy-staging/profile.json ]]; then
  echo 'OCI fallback writers are disabled on shared staging. Use the coordinated VPS tooling.' >&2
  exit 75
fi

if [ "$(id -u)" -ne 0 ]; then
  exec sudo "$0" "$@"
fi

install -m 0644 /opt/convy/current/ops/oci/convy-backup.service /etc/systemd/system/convy-backup.service
install -m 0644 /opt/convy/current/ops/oci/convy-backup.timer /etc/systemd/system/convy-backup.timer
systemctl daemon-reload
systemctl enable --now convy-backup.timer
systemctl list-timers convy-backup.timer
