#!/usr/bin/env bash
set -euo pipefail

RELEASE_SHA="${1:?Usage: deploy-release.sh <release-sha>}"
APP_ROOT="${APP_ROOT:-/opt/convy}"
RELEASE_DIR="$APP_ROOT/releases/$RELEASE_SHA"
ENV_FILE="$APP_ROOT/shared/api.env"
RELEASE_ENV_FILE="${CONVY_RELEASE_ENV_FILE:-$APP_ROOT/shared/release.env}"
FIREBASE_FILE="$APP_ROOT/shared/firebase-admin.json"
COMPOSE_FILE="${COMPOSE_FILE:-$RELEASE_DIR/docker/docker-compose.vps.yml}"
LEGAL_DIR="$APP_ROOT/legal" # Default production path: /opt/convy/legal
BACKUP_ROOT="${BACKUP_ROOT:-$APP_ROOT/backups/postgres}"
BACKUP_READ_GROUP="${BACKUP_READ_GROUP:-1654}"

if [ "$(id -u)" -ne 0 ]; then
  exec sudo --preserve-env=APP_ROOT,COMPOSE_FILE,CONVY_RELEASE_ENV_FILE,BACKUP_ROOT,BACKUP_READ_GROUP "$0" "$@"
fi

if [ ! -f "$ENV_FILE" ]; then
  echo "Missing $ENV_FILE. Run ops/vps/push-secrets.ps1 before deploying." >&2
  exit 1
fi

if [ ! -f "$FIREBASE_FILE" ]; then
  echo "Missing $FIREBASE_FILE. Run ops/vps/push-secrets.ps1 before deploying." >&2
  exit 1
fi

if [ ! -f "$COMPOSE_FILE" ]; then
  echo "Missing compose file at $COMPOSE_FILE" >&2
  exit 1
fi

ln -sfn "$RELEASE_DIR" "$APP_ROOT/current"

ANDROID_BUILD_FILE="$RELEASE_DIR/mobile/androidApp/build.gradle.kts"
ANDROID_VERSION_NAME=""
ANDROID_VERSION_CODE=""
if [ -f "$ANDROID_BUILD_FILE" ]; then
  ANDROID_VERSION_NAME="$(grep -E 'versionName = "' "$ANDROID_BUILD_FILE" | head -n 1 | sed -E 's/.*"([^"]+)".*/\1/' || true)"
  ANDROID_VERSION_CODE="$(grep -E 'versionCode = [0-9]+' "$ANDROID_BUILD_FILE" | head -n 1 | sed -E 's/.*versionCode = ([0-9]+).*/\1/' || true)"
fi

BACKEND_VERSION="${RELEASE_SHA:0:12}"
MOBILE_ANDROID_VERSION="${ANDROID_VERSION_NAME:-unknown}"
if [ -n "$ANDROID_VERSION_CODE" ]; then
  MOBILE_ANDROID_VERSION="$MOBILE_ANDROID_VERSION+$ANDROID_VERSION_CODE"
fi

cat > "$RELEASE_ENV_FILE.tmp" <<EOF
Deploy__ReleaseSha=$RELEASE_SHA
Deploy__LastDeployAt=$(date -u '+%Y-%m-%dT%H:%M:%SZ')
Backend__Version=$BACKEND_VERSION
Mobile__AndroidVersion=$MOBILE_ANDROID_VERSION
EOF
install -m 600 -o root -g root "$RELEASE_ENV_FILE.tmp" "$RELEASE_ENV_FILE"
rm -f "$RELEASE_ENV_FILE.tmp"

if [ -d "$RELEASE_DIR/legal" ]; then
  rm -rf "$APP_ROOT/legal.tmp"
  mkdir -p "$APP_ROOT/legal.tmp"
  cp -a "$RELEASE_DIR/legal/." "$APP_ROOT/legal.tmp/"
  chown -R root:root "$APP_ROOT/legal.tmp"
  find "$APP_ROOT/legal.tmp" -type d -exec chmod 755 {} +
  find "$APP_ROOT/legal.tmp" -type f -exec chmod 644 {} +
  rm -rf "$LEGAL_DIR"
  mv "$APP_ROOT/legal.tmp" "$LEGAL_DIR"
fi

if [ -d "$BACKUP_ROOT" ]; then
  chown -R "root:$BACKUP_READ_GROUP" "$BACKUP_ROOT"
  find "$BACKUP_ROOT" -type d -exec chmod 750 {} +
  find "$BACKUP_ROOT" -type f -name '*.dump' -exec chmod 640 {} +
fi

docker compose --env-file "$ENV_FILE" -f "$COMPOSE_FILE" build api dashboard
docker compose --env-file "$ENV_FILE" -f "$COMPOSE_FILE" up -d --remove-orphans --force-recreate

HOSTNAME="$(grep '^CONVY_API_HOSTNAME=' "$ENV_FILE" | cut -d '=' -f2-)"
if [ -z "$HOSTNAME" ]; then
  echo "CONVY_API_HOSTNAME is missing from $ENV_FILE" >&2
  exit 1
fi

for _ in $(seq 1 30); do
  if curl -fsS "https://$HOSTNAME/health/ready" >/dev/null 2>&1; then
    docker image prune -f --filter "until=168h" >/dev/null
    echo "Convy release $RELEASE_SHA deployed and healthy at https://$HOSTNAME/health/ready"
    exit 0
  fi
  sleep 5
done

docker compose --env-file "$ENV_FILE" -f "$COMPOSE_FILE" logs --tail=200
echo "Health check failed for https://$HOSTNAME/health/ready" >&2
exit 1
