#!/usr/bin/env bash
set -euo pipefail

API_HEALTH_URL="${API_HEALTH_URL:-https://api.convyapp.com/health/ready}"
AUTH_HEALTH_URL="${AUTH_HEALTH_URL:-https://auth.convyapp.com/health}"
MCP_HEALTH_URL="${MCP_HEALTH_URL:-https://mcp.convyapp.com/health}"
HEALTH_TIMEOUT_SECONDS="${HEALTH_TIMEOUT_SECONDS:-10}"
HEALTH_RETRY_COUNT="${HEALTH_RETRY_COUNT:-2}"

json_escape() {
  printf "%s" "$1" | sed 's/\\/\\\\/g; s/"/\\"/g'
}

send_alert() {
  local service="$1"
  local url="$2"
  local status="$3"

  if [ -z "${ALERT_WEBHOOK_URL:-}" ]; then
    return 0
  fi

  local body
  body="$(printf '{"service":"%s","url":"%s","status":"%s","source":"convy-health-check"}' \
    "$(json_escape "$service")" \
    "$(json_escape "$url")" \
    "$(json_escape "$status")")"

  if [ -n "${ALERT_WEBHOOK_BEARER:-}" ]; then
    curl -fsS \
      --max-time "$HEALTH_TIMEOUT_SECONDS" \
      -H "content-type: application/json" \
      -H "authorization: Bearer ${ALERT_WEBHOOK_BEARER}" \
      -d "$body" \
      "$ALERT_WEBHOOK_URL" >/dev/null
  else
    curl -fsS \
      --max-time "$HEALTH_TIMEOUT_SECONDS" \
      -H "content-type: application/json" \
      -d "$body" \
      "$ALERT_WEBHOOK_URL" >/dev/null
  fi
}

check_url() {
  local service="$1"
  local url="$2"

  if curl -fsS \
    --max-time "$HEALTH_TIMEOUT_SECONDS" \
    --retry "$HEALTH_RETRY_COUNT" \
    --retry-delay 2 \
    "$url" >/dev/null; then
    echo "$service healthy: $url"
    return 0
  fi

  echo "$service unhealthy: $url" >&2
  send_alert "$service" "$url" "unhealthy"
  return 1
}

failed=0
check_url "api" "$API_HEALTH_URL" || failed=1
check_url "auth" "$AUTH_HEALTH_URL" || failed=1
check_url "mcp" "$MCP_HEALTH_URL" || failed=1

for extra_url in ${EXTRA_HEALTH_URLS:-}; do
  check_url "extra" "$extra_url" || failed=1
done

exit "$failed"
