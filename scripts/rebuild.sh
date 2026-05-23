#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")/.."

G='\033[0;32m'; Y='\033[1;33m'; C='\033[0;36m'; R='\033[0;31m'; N='\033[0m'

log() { echo -e "${C}[mydash]${N} $*"; }
ok()  { echo -e "${G}[  ok  ]${N} $*"; }
err() { echo -e "${R}[ err  ]${N} $*"; exit 1; }

VALID_SERVICES=(hub web agent)
SERVICE="${1:-}"

[ -z "$SERVICE" ] && err "Usage: ./scripts/rebuild.sh <service>   (hub | web | agent)"

VALID=false
for s in "${VALID_SERVICES[@]}"; do
  [ "$s" = "$SERVICE" ] && VALID=true && break
done
$VALID || err "Unknown service '${SERVICE}'. Valid: ${VALID_SERVICES[*]}"

[ -f .env ] && { set -a; source .env; set +a; }

log "Rebuilding ${SERVICE}..."
docker compose build --no-cache "$SERVICE"

log "Restarting ${SERVICE}..."
docker compose up -d "$SERVICE"

ok "${SERVICE} rebuilt and restarted."

WEB_PORT="${WEB_PORT:-9999}"
[ "$SERVICE" = "web" ] && echo -e "  ${G}→${N}  ${C}http://localhost:${WEB_PORT}${N}"
