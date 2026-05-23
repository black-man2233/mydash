#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")/.."

C='\033[0;36m'; N='\033[0m'
log() { echo -e "${C}[mydash]${N} $*"; }

SERVICE="${1:-}"
LINES="${2:-50}"

if [ -n "$SERVICE" ]; then
  log "Tailing logs for ${SERVICE} (last ${LINES} lines)..."
  docker compose logs -f --tail="$LINES" "$SERVICE"
else
  log "Tailing logs for all services (last ${LINES} lines)..."
  docker compose logs -f --tail="$LINES"
fi
