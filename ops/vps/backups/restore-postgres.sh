#!/usr/bin/env bash
set -euo pipefail

BACKUP_PATH="${1:?Usage: restore-postgres.sh <backup-file> [target-db]}"
APP_ROOT="${APP_ROOT:-/opt/convy}"
ENV_FILE="${ENV_FILE:-$APP_ROOT/shared/api.env}"
DB_CONTAINER="${DB_CONTAINER:-convy-db}"

if [ ! -f "$BACKUP_PATH" ]; then
  echo "Backup file not found: $BACKUP_PATH" >&2
  exit 1
fi

if [ ! -f "$ENV_FILE" ]; then
  echo "Missing $ENV_FILE" >&2
  exit 1
fi

set -a
# shellcheck disable=SC1090
. "$ENV_FILE"
set +a

TARGET_DB="${2:-convy_restore_verify}"

allow_live_staging_restore="${ALLOW_STAGING_RESTORE:-false}"
# Legacy alias retained for one release after the environment rename.
if [ "$allow_live_staging_restore" != "true" ] && [ "${ALLOW_PRODUCTION_RESTORE:-false}" = "true" ]; then
  allow_live_staging_restore="true"
fi

if [ "$TARGET_DB" = "$POSTGRES_DB" ] && [ "$allow_live_staging_restore" != "true" ]; then
  echo "Refusing to restore over live staging database without ALLOW_STAGING_RESTORE=true." >&2
  exit 1
fi

docker exec "$DB_CONTAINER" dropdb -U "$POSTGRES_USER" --if-exists "$TARGET_DB"
docker exec "$DB_CONTAINER" createdb -U "$POSTGRES_USER" "$TARGET_DB"
docker exec -i "$DB_CONTAINER" pg_restore \
  -U "$POSTGRES_USER" \
  -d "$TARGET_DB" \
  --clean \
  --if-exists \
  --no-owner \
  --no-acl < "$BACKUP_PATH"

echo "Backup restored into database $TARGET_DB"
