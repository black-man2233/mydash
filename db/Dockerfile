FROM mcr.microsoft.com/mssql/server:2022-latest

USER root

# deploy.sh and SQL scripts are mounted at /db via docker-compose volume,
# but for standalone use we copy them in here too.
COPY deploy.sh /db/deploy.sh
COPY tables/   /db/tables/

RUN chmod +x /db/deploy.sh

USER mssql
ENTRYPOINT ["/bin/bash", "/db/deploy.sh"]
