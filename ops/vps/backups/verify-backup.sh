#!/usr/bin/env bash
set -euo pipefail

BACKUP_PATH="${1:?Usage: verify-backup.sh <backup-file>}"

if [ ! -f "$BACKUP_PATH" ]; then
  echo "Backup file not found: $BACKUP_PATH" >&2
  exit 1
fi

backup_dir="$(cd "$(dirname "$BACKUP_PATH")" && pwd)"
backup_file="$(basename "$BACKUP_PATH")"

MSYS_NO_PATHCONV=1 docker run --rm \
  -v "$backup_dir:/backup:ro" \
  postgres:16-alpine \
  pg_restore --list "/backup/$backup_file" >/dev/null

echo "Backup catalog verified: $BACKUP_PATH"
