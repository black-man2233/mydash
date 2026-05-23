#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")/.."

G='\033[0;32m'; Y='\033[1;33m'; C='\033[0;36m'; R='\033[0;31m'; N='\033[0m'
log()  { echo -e "${C}[mydash]${N} $*"; }
ok()   { echo -e "${G}[  ok  ]${N} $*"; }
warn() { echo -e "${Y}[ warn ]${N} $*"; }
err()  { echo -e "${R}[ err  ]${N} $*"; }

[ -f .env ] && { set -a; source .env; set +a; }

SERVICES=(); BUILD=true; DETACH=true; AGENT=false; CLEAN=false
for arg in "$@"; do
  case $arg in
    --no-build) BUILD=false ;;
    --attach)   DETACH=false ;;
    --agent)    AGENT=true ;;
    --clean)    CLEAN=true ;;
    hub|web|db) SERVICES+=("$arg") ;;
  esac
done

echo ""
echo -e "${C}  ███╗   ███╗██╗   ██╗██████╗  █████╗ ███████╗██╗  ██╗${N}"
echo -e "${C}  ████╗ ████║╚██╗ ██╔╝██╔══██╗██╔══██╗██╔════╝██║  ██║${N}"
echo -e "${C}  ██╔████╔██║ ╚████╔╝ ██║  ██║███████║███████╗███████║${N}"
echo -e "${C}  ██║╚██╔╝██║  ╚██╔╝  ██║  ██║██╔══██║╚════██║██╔══██║${N}"
echo -e "${C}  ██║ ╚═╝ ██║   ██║   ██████╔╝██║  ██║███████║██║  ██║${N}"
echo -e "${C}  ╚═╝     ╚═╝   ╚═╝   ╚═════╝ ╚═╝  ╚═╝╚══════╝╚═╝  ╚═╝${N}"
echo ""

# ─── free up disk if docker build cache is large ──────────────────────────────
CACHE_GB=$(docker system df --format '{{.Size}}' 2>/dev/null | grep -oP '[\d.]+(?=GB)' | head -1 || echo 0)
if (( $(echo "$CACHE_GB > 3" | bc -l 2>/dev/null || echo 0) )); then
  warn "Build cache is ${CACHE_GB}GB — pruning..."
  docker builder prune -f &>/dev/null || true
fi

COMPOSE_ARGS=()
$BUILD  && COMPOSE_ARGS+=(--build)
$DETACH && COMPOSE_ARGS+=(-d)
$AGENT  && COMPOSE_ARGS+=(--profile agent)

run_stack() {
  if [ ${#SERVICES[@]} -gt 0 ]; then
    log "Starting: ${SERVICES[*]}"
    docker compose up "${COMPOSE_ARGS[@]}" "${SERVICES[@]}"
  else
    log "Starting all services..."
    docker compose up "${COMPOSE_ARGS[@]}"
  fi
}

# ─── clean start if requested ─────────────────────────────────────────────────
if $CLEAN; then
  warn "--clean: tearing down volumes for a fresh start..."
  docker compose down -v 2>/dev/null || true
fi

# ─── first attempt ────────────────────────────────────────────────────────────
if run_stack; then
  # check db-init didn't silently fail
  DBINIT_STATE=$(docker inspect --format='{{.State.ExitCode}}' mydash-db-init-1 2>/dev/null || echo 0)
  if [ "$DBINIT_STATE" != "0" ] && [ "$DBINIT_STATE" != "" ]; then
    warn "db-init exited with code $DBINIT_STATE — automatic recovery..."
    AUTO_RECOVER=true
  else
    AUTO_RECOVER=false
  fi
else
  # compose itself returned non-zero (db-init failure surfaces here)
  warn "Stack failed to start — attempting automatic recovery..."
  AUTO_RECOVER=true
fi

# ─── auto-recover: wipe volumes and retry once ────────────────────────────────
if ${AUTO_RECOVER:-false}; then
  log "Wiping database volume and restarting (data was likely corrupted)..."
  docker compose down -v 2>/dev/null || true

  log "Retrying..."
  if run_stack; then
    ok "Recovery succeeded."
  else
    err "Stack still failed after recovery. Check logs:"
    err "  ./scripts/logs.sh db-init"
    err "  ./scripts/logs.sh hub"
    exit 1
  fi
fi

# ─── post-start summary ───────────────────────────────────────────────────────
if $DETACH; then
  echo ""
  ok "Stack is up!"
  echo ""
  WEB_PORT="${WEB_PORT:-9999}"
  HUB_PORT="${HUB_GRPC_PORT:-8080}"
  echo -e "  ${G}→${N}  Web UI  : ${C}http://localhost:${WEB_PORT}${N}"
  echo -e "  ${G}→${N}  Hub API : ${C}http://localhost:${HUB_PORT}/api${N}"
  echo ""
  echo -e "  ${Y}./scripts/logs.sh${N}          tail all logs"
  echo -e "  ${Y}./scripts/rebuild.sh web${N}   rebuild a service"
  echo -e "  ${Y}./scripts/down.sh${N}          stop everything"
  echo ""
fi
