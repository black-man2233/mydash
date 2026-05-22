#!/usr/bin/env bash
# MyDash Agent installer for Linux (x64, arm64, arm)
# Usage: curl -fsSL https://mydash.example.com/agent/install.sh | sudo bash -s -- --hub <URL> --token <TOKEN> --name <NAME>
set -euo pipefail

HUB=""
TOKEN=""
NAME=$(hostname -s)
VERSION="latest"
INSTALL_DIR="/usr/local/bin"
DATA_DIR="/var/lib/mydash-agent"
SERVICE_FILE="/etc/systemd/system/mydash-agent.service"

usage() {
    echo "Usage: $0 --hub <URL> --token <TOKEN> [--name <NAME>] [--version <VERSION>]"
    exit 1
}

while [[ $# -gt 0 ]]; do
    case "$1" in
        --hub)     HUB="$2";     shift 2 ;;
        --token)   TOKEN="$2";   shift 2 ;;
        --name)    NAME="$2";    shift 2 ;;
        --version) VERSION="$2"; shift 2 ;;
        *)         usage ;;
    esac
done

[[ -z "$HUB"   ]] && { echo "ERROR: --hub is required";   usage; }
[[ -z "$TOKEN" ]] && { echo "ERROR: --token is required"; usage; }

# Detect architecture
ARCH=$(uname -m)
case "$ARCH" in
    x86_64)          RID="linux-x64"   ;;
    aarch64 | arm64) RID="linux-arm64" ;;
    armv7l | armv6l) RID="linux-arm"   ;;
    *)               echo "Unsupported arch: $ARCH"; exit 1 ;;
esac

BINARY_URL="https://github.com/user/mydash/releases/${VERSION}/download/mydash-agent-${RID}"

echo "[install] Downloading MyDash Agent (${RID})..."
curl -fsSL "$BINARY_URL" -o /tmp/mydash-agent
chmod +x /tmp/mydash-agent

echo "[install] Installing to $INSTALL_DIR..."
mv /tmp/mydash-agent "$INSTALL_DIR/mydash-agent"
mkdir -p "$DATA_DIR"

echo "[install] Creating systemd service..."
cat > "$SERVICE_FILE" <<EOF
[Unit]
Description=MyDash Agent
After=network-online.target
Wants=network-online.target

[Service]
Type=simple
User=root
ExecStart=$INSTALL_DIR/mydash-agent
Restart=on-failure
RestartSec=10
Environment=MYDASH_HUB=$HUB
Environment=MYDASH_TOKEN=$TOKEN
Environment=MYDASH_NAME=$NAME

[Install]
WantedBy=multi-user.target
EOF

systemctl daemon-reload
systemctl enable --now mydash-agent

echo "[install] Done! mydash-agent is running."
echo "          Check status: systemctl status mydash-agent"
