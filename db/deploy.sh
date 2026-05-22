#!/usr/bin/env bash
# Waits for SQL Server to accept connections, creates the database, and applies all table scripts.
set -e

SERVER="${DB_SERVER:-db}"
SA_USER="${DB_USER:-sa}"
SA_PASS="${DB_PASSWORD:-MyDash_Str0ng!}"
DB_NAME="${DB_NAME:-MyDash}"
SQLCMD="/opt/mssql-tools18/bin/sqlcmd"

echo "[db-init] Waiting for SQL Server at $SERVER..."
for i in $(seq 1 30); do
    if $SQLCMD -S "$SERVER" -U "$SA_USER" -P "$SA_PASS" -C -Q "SELECT 1" &>/dev/null; then
        echo "[db-init] SQL Server is up."
        break
    fi
    echo "[db-init] Attempt $i/30 — not ready yet, sleeping 5s..."
    sleep 5
    if [ "$i" -eq 30 ]; then
        echo "[db-init] ERROR: SQL Server did not become ready in time."
        exit 1
    fi
done

echo "[db-init] Creating database '$DB_NAME' if it does not exist..."
$SQLCMD -S "$SERVER" -U "$SA_USER" -P "$SA_PASS" -C -Q "
IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = N'$DB_NAME')
BEGIN
    CREATE DATABASE [$DB_NAME];
    PRINT 'Database created.';
END
ELSE
    PRINT 'Database already exists.';
"

echo "[db-init] Applying table scripts..."
for script in /db/tables/*.sql; do
    echo "[db-init]   -> $script"
    $SQLCMD -S "$SERVER" -U "$SA_USER" -P "$SA_PASS" -C -d "$DB_NAME" -i "$script"
done

echo "[db-init] Schema deployment complete."
