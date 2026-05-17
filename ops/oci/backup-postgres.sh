#!/usr/bin/env bash
set -euo pipefail

APP_ROOT="${APP_ROOT:-/opt/convy}"
ENV_FILE="$APP_ROOT/shared/api.env"
BACKUP_DIR="$APP_ROOT/backups/postgres"
RETENTION_DAYS="${RETENTION_DAYS:-14}"

if [ "$(id -u)" -ne 0 ]; then
  exec sudo --preserve-env=APP_ROOT,RETENTION_DAYS "$0" "$@"
fi

if [ ! -f "$ENV_FILE" ]; then
  echo "Missing $ENV_FILE" >&2
  exit 1
fi

set -a
# shellcheck disable=SC1090
. "$ENV_FILE"
set +a

mkdir -p "$BACKUP_DIR"
chmod 700 "$BACKUP_DIR"

STAMP="$(date -u +%Y%m%dT%H%M%SZ)"
OUT="$BACKUP_DIR/convy-$STAMP.sql.gz"

docker exec convy-db pg_dump -U "$POSTGRES_USER" -d "$POSTGRES_DB" --clean --if-exists | gzip -9 > "$OUT"
chmod 600 "$OUT"

find "$BACKUP_DIR" -type f -name 'convy-*.sql.gz' -mtime +"$RETENTION_DAYS" -delete

echo "PostgreSQL backup created at $OUT"
