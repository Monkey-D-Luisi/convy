#!/usr/bin/env bash
set -euo pipefail

APP_ROOT="${APP_ROOT:-/opt/convy}"
ENV_FILE="${ENV_FILE:-$APP_ROOT/shared/api.env}"
BACKUP_ROOT="${BACKUP_ROOT:-$APP_ROOT/backups/postgres}"
DB_CONTAINER="${DB_CONTAINER:-convy-db}"
BACKUP_PATH="${1:-}"

if [ -z "$BACKUP_PATH" ]; then
  BACKUP_PATH="$(find "$BACKUP_ROOT" -type f -name '*.dump' -printf '%T@ %p\n' | sort -nr | awk 'NR == 1 {print $2}')"
fi

if [ -z "$BACKUP_PATH" ] || [ ! -f "$BACKUP_PATH" ]; then
  echo "No backup file found for restore verification." >&2
  exit 1
fi

set -a
# shellcheck disable=SC1090
. "$ENV_FILE"
set +a

TARGET_DB="convy_restore_verify_$(date -u +%Y%m%d%H%M%S)"
"$(dirname "$0")/restore-postgres.sh" "$BACKUP_PATH" "$TARGET_DB"
docker exec "$DB_CONTAINER" psql -v ON_ERROR_STOP=1 -U "$POSTGRES_USER" -d "$TARGET_DB" -c "SELECT 1;" >/dev/null
docker exec "$DB_CONTAINER" dropdb -U "$POSTGRES_USER" --if-exists "$TARGET_DB"

echo "Restore verification completed for $BACKUP_PATH"
