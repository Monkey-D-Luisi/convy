#!/usr/bin/env bash
set -euo pipefail

APP_ROOT="${APP_ROOT:-/opt/convy}"
BACKUP_ROOT="${BACKUP_ROOT:-$APP_ROOT/backups/postgres}"

find "$BACKUP_ROOT/daily" -type f -name '*.dump' -mtime +7 -delete
find "$BACKUP_ROOT/weekly" -type f -name '*.dump' -mtime +35 -delete
find "$BACKUP_ROOT/monthly" -type f -name '*.dump' -mtime +120 -delete
find "$BACKUP_ROOT/metadata" -type f -name '*.json' -mtime +120 -delete

echo "Backup retention pruning completed under $BACKUP_ROOT"
