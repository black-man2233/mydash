FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 7777

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy solution and all csproj files first (cache layer)
COPY MyDash.sln .
COPY src/MyDash.Domain/MyDash.Domain.csproj           src/MyDash.Domain/
COPY src/MyDash.Application/MyDash.Application.csproj src/MyDash.Application/
COPY src/MyDash.Infrastructure/MyDash.Infrastructure.csproj src/MyDash.Infrastructure/
COPY src/MyDash.Contracts/MyDash.Contracts.csproj     src/MyDash.Contracts/
COPY src/MyDash.Hub/MyDash.Hub.csproj                 src/MyDash.Hub/
COPY src/MyDash.Agent/MyDash.Agent.csproj             src/MyDash.Agent/

RUN dotnet restore src/MyDash.Hub/MyDash.Hub.csproj

# Copy everything else
COPY src/ src/

RUN dotnet publish src/MyDash.Hub/MyDash.Hub.csproj \
    -c Release -o /app/publish --no-restore

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .

RUN mkdir -p /var/lib/mydash && mkdir -p /app/logs

ENTRYPOINT ["dotnet", "MyDash.Hub.dll"]
