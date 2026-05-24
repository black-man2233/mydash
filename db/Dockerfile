# ── Stage 1: Build DACPAC from the SQL Database Project ───────────────────────
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS dacpac-builder
WORKDIR /src
COPY src/MyDash.Database/ ./MyDash.Database/
RUN dotnet build ./MyDash.Database/MyDash.Database.sqlproj -c Release -o /dacpac-out

# ── Stage 2: Wait for SQL Server and publish via SqlPackage ───────────────────
# sqlpackage targets .NET 8, so use the .NET 8 SDK base
FROM mcr.microsoft.com/dotnet/sdk:8.0
WORKDIR /app

# Install sqlcmd (for readiness check) and sqlpackage (for DACPAC deploy)
RUN apt-get update -qq && \
    apt-get install -y --no-install-recommends curl gnupg2 unixodbc-dev && \
    curl -fsSL https://packages.microsoft.com/keys/microsoft.asc | \
        gpg --dearmor -o /usr/share/keyrings/microsoft-prod.gpg && \
    printf 'deb [arch=amd64,arm64 signed-by=/usr/share/keyrings/microsoft-prod.gpg] https://packages.microsoft.com/debian/12/prod bookworm main\n' \
        > /etc/apt/sources.list.d/mssql-release.list && \
    apt-get update -qq && \
    ACCEPT_EULA=Y apt-get install -y --no-install-recommends mssql-tools18 && \
    dotnet tool install -g microsoft.sqlpackage && \
    apt-get clean && rm -rf /var/lib/apt/lists/*

ENV PATH="$PATH:/opt/mssql-tools18/bin:/root/.dotnet/tools"

COPY --from=dacpac-builder /dacpac-out/MyDash.Database.dacpac /app/MyDash.dacpac
COPY db/deploy.sh /app/deploy.sh
RUN chmod +x /app/deploy.sh

ENTRYPOINT ["/bin/bash", "/app/deploy.sh"]
