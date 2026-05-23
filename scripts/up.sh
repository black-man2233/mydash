#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")/.."

# ─── colours ──────────────────────────────────────────────────────────────────
G='\033[0;32m'; Y='\033[1;33m'; C='\033[0;36m'; R='\033[0;31m'; N='\033[0m'

log()  { echo -e "${C}[mydash]${N} $*"; }
ok()   { echo -e "${G}[  ok  ]${N} $*"; }
warn() { echo -e "${Y}[ warn ]${N} $*"; }
err()  { echo -e "${R}[ err  ]${N} $*"; }

# ─── load .env if present ─────────────────────────────────────────────────────
[ -f .env ] && { log "Loading .env"; set -a; source .env; set +a; }

# ─── parse args ───────────────────────────────────────────────────────────────
SERVICES=()
BUILD=true
DETACH=true
AGENT=false

for arg in "$@"; do
  case $arg in
    --no-build)  BUILD=false ;;
    --attach)    DETACH=false ;;
    --agent)     AGENT=true ;;
    hub|web|db)  SERVICES+=("$arg") ;;
    *)           warn "Unknown arg: $arg" ;;
  esac
done

# ─── banner ───────────────────────────────────────────────────────────────────
echo ""
echo -e "${C}  ███╗   ███╗██╗   ██╗██████╗  █████╗ ███████╗██╗  ██╗${N}"
echo -e "${C}  ████╗ ████║╚██╗ ██╔╝██╔══██╗██╔══██╗██╔════╝██║  ██║${N}"
echo -e "${C}  ██╔████╔██║ ╚████╔╝ ██║  ██║███████║███████╗███████║${N}"
echo -e "${C}  ██║╚██╔╝██║  ╚██╔╝  ██║  ██║██╔══██║╚════██║██╔══██║${N}"
echo -e "${C}  ██║ ╚═╝ ██║   ██║   ██████╔╝██║  ██║███████║██║  ██║${N}"
echo -e "${C}  ╚═╝     ╚═╝   ╚═╝   ╚═════╝ ╚═╝  ╚═╝╚══════╝╚═╝  ╚═╝${N}"
echo ""

# ─── compose args ─────────────────────────────────────────────────────────────
COMPOSE_ARGS=()
$BUILD   && COMPOSE_ARGS+=(--build)
$DETACH  && COMPOSE_ARGS+=(-d)
$AGENT   && COMPOSE_ARGS+=(--profile agent)

if [ ${#SERVICES[@]} -gt 0 ]; then
  log "Starting services: ${SERVICES[*]}"
  docker compose up "${COMPOSE_ARGS[@]}" "${SERVICES[@]}"
else
  log "Starting all core services (hub + web + db)"
  docker compose up "${COMPOSE_ARGS[@]}"
fi

# ─── post-start info ──────────────────────────────────────────────────────────
if $DETACH; then
  echo ""
  ok "Stack is up!"
  echo ""
  WEB_PORT="${WEB_PORT:-9999}"
  HUB_PORT="${HUB_GRPC_PORT:-8080}"
  echo -e "  ${G}→${N}  Web UI  : ${C}http://localhost:${WEB_PORT}${N}"
  echo -e "  ${G}→${N}  Hub API : ${C}http://localhost:${HUB_PORT}/api${N}"
  echo ""
  echo -e "  Run ${Y}./scripts/logs.sh${N}    to tail logs"
  echo -e "  Run ${Y}./scripts/down.sh${N}   to stop"
  echo -e "  Run ${Y}./scripts/rebuild.sh web${N} to rebuild a service"
  echo ""
fi
