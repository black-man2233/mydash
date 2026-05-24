#!/usr/bin/env bash
# Waits for SQL Server, then publishes the DACPAC (creates DB + applies schema idempotently).
set -e

SERVER="${DB_SERVER:-db}"
SA_USER="${DB_USER:-sa}"
SA_PASS="${DB_PASSWORD:-MyDash_Str0ng!}"
DB_NAME="${DB_NAME:-MyDash}"
SQLCMD="/opt/mssql-tools18/bin/sqlcmd"
DACPAC="/app/MyDash.dacpac"

# ─── wait for SQL Server ──────────────────────────────────────────────────────
echo "[db-init] Waiting for SQL Server at $SERVER..."
for i in $(seq 1 40); do
    if $SQLCMD -S "$SERVER" -U "$SA_USER" -P "$SA_PASS" -C -Q "SELECT 1" &>/dev/null; then
        echo "[db-init] SQL Server is up."
        break
    fi
    echo "[db-init] Attempt $i/40 — not ready yet, sleeping 5s..."
    sleep 5
    if [ "$i" -eq 40 ]; then
        echo "[db-init] ERROR: SQL Server did not become ready in time."
        exit 1
    fi
done

# ─── publish DACPAC ───────────────────────────────────────────────────────────
# SqlPackage creates the database if it doesn't exist, then applies all schema
# changes from the DACPAC in a diff-based, idempotent way.
echo "[db-init] Publishing DACPAC to [$DB_NAME] on $SERVER..."
sqlpackage \
    /Action:Publish \
    /SourceFile:"$DACPAC" \
    /TargetServerName:"$SERVER" \
    /TargetDatabaseName:"$DB_NAME" \
    /TargetUser:"$SA_USER" \
    /TargetPassword:"$SA_PASS" \
    /TargetTrustServerCertificate:true \
    /p:BlockOnPossibleDataLoss=false \
    /p:DropObjectsNotInSource=false \
    /p:DropIndexesNotInSource=false

echo "[db-init] Schema deployment complete."
