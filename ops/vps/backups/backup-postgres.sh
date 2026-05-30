#!/usr/bin/env bash
set -euo pipefail

APP_ROOT="${APP_ROOT:-/opt/convy}"
ENV_FILE="${ENV_FILE:-$APP_ROOT/shared/api.env}"
RESTIC_ENV_FILE="${RESTIC_ENV_FILE:-$APP_ROOT/shared/backup.env}"
BACKUP_ROOT="${BACKUP_ROOT:-$APP_ROOT/backups/postgres}"
DB_CONTAINER="${DB_CONTAINER:-convy-db}"
BACKUP_TYPE="${BACKUP_TYPE:-Daily}"
BACKUP_TIMEOUT_SECONDS="${BACKUP_TIMEOUT_SECONDS:-900}"
MIN_FREE_KB="${MIN_FREE_KB:-1048576}"
BACKUP_READ_GROUP="${BACKUP_READ_GROUP:-1654}"

LOCK_FILE="$BACKUP_ROOT/metadata/backup.lock"
STARTED_AT="$(date -u +"%Y-%m-%dT%H:%M:%SZ")"
START_EPOCH_MS="$(date -u +%s%3N)"

generate_uuid() {
  if [ -r /proc/sys/kernel/random/uuid ]; then
    cat /proc/sys/kernel/random/uuid
    return
  fi

  if command -v uuidgen >/dev/null 2>&1; then
    uuidgen
    return
  fi

  local hex
  hex="$(od -An -N16 -tx1 /dev/urandom | tr -d ' \n')"
  printf "%s-%s-%s-%s-%s\n" \
    "${hex:0:8}" \
    "${hex:8:4}" \
    "${hex:12:4}" \
    "${hex:16:4}" \
    "${hex:20:12}"
}

RUN_ID="$(generate_uuid)"

if [ "$(id -u)" -ne 0 ] && [ "$APP_ROOT" = "/opt/convy" ]; then
  exec sudo --preserve-env=APP_ROOT,ENV_FILE,RESTIC_ENV_FILE,BACKUP_ROOT,DB_CONTAINER,BACKUP_TYPE,BACKUP_TIMEOUT_SECONDS,MIN_FREE_KB,BACKUP_READ_GROUP,RESTIC_REPOSITORY,RESTIC_PASSWORD,RESTIC_PASSWORD_FILE,RESTIC_CACHE_DIR,RESTIC_BACKUP_TAGS,AWS_ACCESS_KEY_ID,AWS_SECRET_ACCESS_KEY,AWS_DEFAULT_REGION,AWS_PROFILE "$0" "$@"
fi

if [ ! -f "$ENV_FILE" ]; then
  echo "Missing $ENV_FILE" >&2
  exit 1
fi

set -a
# shellcheck disable=SC1090
. "$ENV_FILE"
set +a

if [ -f "$RESTIC_ENV_FILE" ]; then
  set -a
  # shellcheck disable=SC1090
  . "$RESTIC_ENV_FILE"
  set +a
fi

case "$BACKUP_TYPE" in
  Daily|Weekly|Monthly|Manual) ;;
  *) echo "BACKUP_TYPE must be Daily, Weekly, Monthly, or Manual." >&2; exit 1 ;;
esac

case "$BACKUP_TYPE" in
  Daily) BUCKET="daily" ;;
  Weekly) BUCKET="weekly" ;;
  Monthly) BUCKET="monthly" ;;
  Manual) BUCKET="manual" ;;
esac

mkdir -p "$BACKUP_ROOT/daily" "$BACKUP_ROOT/weekly" "$BACKUP_ROOT/monthly" "$BACKUP_ROOT/manual" "$BACKUP_ROOT/metadata"
chown -R "root:$BACKUP_READ_GROUP" "$BACKUP_ROOT"
find "$BACKUP_ROOT" -type d -exec chmod 750 {} +

free_kb="$(df -Pk "$BACKUP_ROOT" | awk 'NR == 2 {print $4}')"
if [ "${free_kb:-0}" -lt "$MIN_FREE_KB" ]; then
  echo "Not enough free space for backup. Available=${free_kb}KB Required=${MIN_FREE_KB}KB" >&2
  exit 1
fi

backup_name="convy-${POSTGRES_DB}-${BUCKET}-$(date -u +"%Y%m%dT%H%M%SZ").dump"
backup_path="$BACKUP_ROOT/$BUCKET/$backup_name"
metadata_path="$BACKUP_ROOT/metadata/${backup_name}.json"

sql_escape() {
  printf "%s" "$1" | sed "s/'/''/g"
}

record_run() {
  local status="$1"
  local verification_status="$2"
  local file_name="${3:-}"
  local size_bytes="${4:-}"
  local sha256="${5:-}"
  local error_message="${6:-}"
  local finished_at duration_ms

  finished_at="$(date -u +"%Y-%m-%dT%H:%M:%SZ")"
  duration_ms="$(( $(date -u +%s%3N) - START_EPOCH_MS ))"

  docker exec -i "$DB_CONTAINER" psql -v ON_ERROR_STOP=1 -U "$POSTGRES_USER" -d "$POSTGRES_DB" >/dev/null <<SQL
INSERT INTO backup_runs
  (id, status, backup_type, file_name, size_bytes, sha256, duration_ms, verification_status, error_message, started_at, finished_at)
VALUES
  ('$RUN_ID', '$(sql_escape "$status")', '$(sql_escape "$BACKUP_TYPE")', NULLIF('$(sql_escape "$file_name")', ''), NULLIF('$size_bytes', '')::bigint, NULLIF('$(sql_escape "$sha256")', ''), $duration_ms, '$(sql_escape "$verification_status")', NULLIF('$(sql_escape "$error_message")', ''), '$STARTED_AT', '$finished_at');
SQL
}

write_metadata() {
  local status="$1"
  local verification_status="$2"
  local size_bytes="${3:-}"
  local sha256="${4:-}"
  local error_message="${5:-}"
  local offsite_status="${6:-Disabled}"

  cat > "$metadata_path" <<JSON
{
  "id": "$RUN_ID",
  "status": "$status",
  "backupType": "$BACKUP_TYPE",
  "fileName": "$backup_name",
  "sizeBytes": ${size_bytes:-null},
  "sha256": "${sha256:-}",
  "verificationStatus": "$verification_status",
  "offsiteStatus": "$offsite_status",
  "errorMessage": "${error_message:-}",
  "startedAt": "$STARTED_AT",
  "finishedAt": "$(date -u +"%Y-%m-%dT%H:%M:%SZ")"
}
JSON
  chmod 600 "$metadata_path"
}

run_offsite_backup() {
  if [ -z "${RESTIC_REPOSITORY:-}" ]; then
    return 0
  fi

  if [ -z "${RESTIC_PASSWORD:-}" ] && [ -z "${RESTIC_PASSWORD_FILE:-}" ]; then
    echo "RESTIC_REPOSITORY is set but no RESTIC_PASSWORD or RESTIC_PASSWORD_FILE is configured." >&2
    return 1
  fi

  if ! command -v restic >/dev/null 2>&1; then
    echo "RESTIC_REPOSITORY is set but restic is not installed." >&2
    return 1
  fi

  local -a restic_args
  restic_args=(backup "$backup_path" "$metadata_path" --tag convy --tag postgres --tag "$BUCKET")
  for tag in ${RESTIC_BACKUP_TAGS:-}; do
    restic_args+=(--tag "$tag")
  done

  restic "${restic_args[@]}"
}

if command -v flock >/dev/null 2>&1; then
  exec 9>"$LOCK_FILE"
  flock -n 9 || {
    echo "Another PostgreSQL backup is already running." >&2
    exit 1
  }
else
  LOCK_DIR="$LOCK_FILE.d"
  if ! mkdir "$LOCK_DIR" 2>/dev/null; then
    echo "Another PostgreSQL backup is already running." >&2
    exit 1
  fi
  trap 'rmdir "$LOCK_DIR" 2>/dev/null || true' EXIT
fi

error_message=""
if ! timeout "$BACKUP_TIMEOUT_SECONDS" docker exec "$DB_CONTAINER" pg_dump \
  -U "$POSTGRES_USER" \
  -d "$POSTGRES_DB" \
  --format=custom \
  --compress=3 \
  --no-owner \
  --no-acl > "$backup_path"; then
  error_message="pg_dump failed"
  rm -f "$backup_path"
  write_metadata "Failed" "NotRun" "" "" "$error_message"
  record_run "Failed" "NotRun" "" "" "" "$error_message"
  exit 1
fi

chown "root:$BACKUP_READ_GROUP" "$backup_path"
chmod 640 "$backup_path"
size_bytes="$(stat -c '%s' "$backup_path")"
sha256="$(sha256sum "$backup_path" | awk '{print $1}')"

if ! "$(dirname "$0")/verify-backup.sh" "$backup_path"; then
  error_message="pg_restore --list verification failed"
  write_metadata "Failed" "Failed" "$size_bytes" "$sha256" "$error_message"
  record_run "Failed" "Failed" "$backup_name" "$size_bytes" "$sha256" "$error_message"
  exit 1
fi

if [ -n "${RESTIC_REPOSITORY:-}" ]; then
  write_metadata "Success" "PgRestoreListOk" "$size_bytes" "$sha256" "" "Uploaded"
  if ! run_offsite_backup; then
    error_message="restic offsite backup failed"
    write_metadata "Failed" "PgRestoreListOk" "$size_bytes" "$sha256" "$error_message" "Failed"
    record_run "Failed" "PgRestoreListOk" "$backup_name" "$size_bytes" "$sha256" "$error_message"
    exit 1
  fi
else
  write_metadata "Success" "PgRestoreListOk" "$size_bytes" "$sha256" "" "Disabled"
fi

record_run "Success" "PgRestoreListOk" "$backup_name" "$size_bytes" "$sha256" ""

if [ -x "$(dirname "$0")/prune-backups.sh" ]; then
  "$(dirname "$0")/prune-backups.sh"
fi

echo "PostgreSQL backup created at $backup_path"
