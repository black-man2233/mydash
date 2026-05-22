FROM mcr.microsoft.com/dotnet/runtime:9.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY MyDash.sln .
COPY src/MyDash.Contracts/MyDash.Contracts.csproj src/MyDash.Contracts/
COPY src/MyDash.Agent/MyDash.Agent.csproj         src/MyDash.Agent/

RUN dotnet restore src/MyDash.Agent/MyDash.Agent.csproj

COPY src/MyDash.Contracts/ src/MyDash.Contracts/
COPY src/MyDash.Agent/     src/MyDash.Agent/

RUN dotnet publish src/MyDash.Agent/MyDash.Agent.csproj \
    -c Release -o /app/publish --no-restore

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .

RUN mkdir -p /var/lib/mydash-agent /app/logs

ENV MYDASH_HUB=http://hub:8080
ENV MYDASH_NAME=agent

ENTRYPOINT ["dotnet", "MyDash.Agent.dll"]
