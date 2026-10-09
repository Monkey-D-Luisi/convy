#!/usr/bin/env bash
set -euo pipefail
if [ "${1:-}" != "--profile" ]; then
  echo 'Legacy deployment is disabled. Use a reviewed profile, immutable bundle, manifest digest and approved plan digest.' >&2
  exit 1
fi
exec python3 "$(dirname "$0")/safe-release.py" apply "$@"
