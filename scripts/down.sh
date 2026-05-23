#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")/.."

C='\033[0;36m'; Y='\033[1;33m'; N='\033[0m'

log() { echo -e "${C}[mydash]${N} $*"; }

VOLUMES=false
for arg in "$@"; do
  case $arg in
    -v|--volumes) VOLUMES=true ;;
  esac
done

if $VOLUMES; then
  log "Stopping and removing containers + volumes..."
  docker compose down -v
else
  log "Stopping containers (data volumes preserved)..."
  docker compose down
fi

echo -e "${Y}[  ok  ]${N} Stack stopped."
