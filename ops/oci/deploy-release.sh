#!/usr/bin/env bash
set -euo pipefail

RELEASE_SHA="${1:?Usage: deploy-release.sh <release-sha>}"
APP_ROOT="${APP_ROOT:-/opt/convy}"
RELEASE_DIR="$APP_ROOT/releases/$RELEASE_SHA"
ENV_FILE="$APP_ROOT/shared/api.env"
FIREBASE_FILE="$APP_ROOT/shared/firebase-admin.json"
COMPOSE_FILE="$RELEASE_DIR/docker/docker-compose.oci.yml"

if [ "$(id -u)" -ne 0 ]; then
  exec sudo --preserve-env=APP_ROOT "$0" "$@"
fi

if [ ! -f "$ENV_FILE" ]; then
  echo "Missing $ENV_FILE. Run ops/oci/push-secrets.ps1 before deploying." >&2
  exit 1
fi

if [ ! -f "$FIREBASE_FILE" ]; then
  echo "Missing $FIREBASE_FILE. Run ops/oci/push-secrets.ps1 before deploying." >&2
  exit 1
fi

if [ ! -f "$COMPOSE_FILE" ]; then
  echo "Missing compose file at $COMPOSE_FILE" >&2
  exit 1
fi

ln -sfn "$RELEASE_DIR" "$APP_ROOT/current"

docker compose --env-file "$ENV_FILE" -f "$COMPOSE_FILE" build api
docker compose --env-file "$ENV_FILE" -f "$COMPOSE_FILE" up -d --remove-orphans --force-recreate

HOSTNAME="$(grep '^CONVY_HOSTNAME=' "$ENV_FILE" | cut -d '=' -f2-)"
if [ -z "$HOSTNAME" ]; then
  echo "CONVY_HOSTNAME is missing from $ENV_FILE" >&2
  exit 1
fi

for _ in $(seq 1 30); do
  if curl -fsS "https://$HOSTNAME/health" >/dev/null; then
    docker image prune -f --filter "until=168h" >/dev/null
    echo "Convy release $RELEASE_SHA deployed and healthy at https://$HOSTNAME/health"
    exit 0
  fi
  sleep 5
done

docker compose --env-file "$ENV_FILE" -f "$COMPOSE_FILE" logs --tail=200
echo "Health check failed for https://$HOSTNAME/health" >&2
exit 1
