# Changelog

All notable changes to MyDash are documented here.

## [Unreleased]

### Added

- **Hub (Blazor Server)** — full web UI with MudBlazor 7, dark theme, JetBrains Mono, Material Icons Rounded
  - Login page with 6-digit PIN mask and gradient background
  - Dashboard KPI cards (servers, services, CPU, memory) and recent audit table
  - Fleet page with enrolled agents table, enrollment token management, and download snippets
  - Server Detail page with service grid/table toggle and per-service actions
  - Scanner page with terminal-style output and port registration
  - Audit Log with sortable columns and pagination
  - Settings page with SMS provider radio group, theme/density, and notification toggles
  - Connect Server wizard dialog (3 steps: name/tags → install commands → enrollment polling)
  - Add Service dialog
- **gRPC services** — `AgentService`, `AuthService`, `FleetService` with mTLS-ready server and streaming enrollment events
- **SMS PIN authentication** — 6-digit codes, BCrypt hashing, 5/hr/IP rate limit, configurable lockout
  - Pluggable `ISmsSender`: TextBelt (default), Twilio, Vonage, ClickSend, SMSAPI
- **Clean architecture layers** — Domain → Application → Infrastructure / Hub / Agent; domain depends on nothing
- **Domain entities** — `Server`, `Service`, `EnrollmentToken`, `AuditEntry`, `PinChallenge`, `UserPreferences`
- **Application use cases** — `RequestPinHandler`, `VerifyPinHandler`; 6 repository interfaces
- **Infrastructure** — EF Core 9 (SQL Server in production, SQLite in-memory for tests), 6 repositories
- **Background services** — `AgentHeartbeatWatchdog` (30 s poll), `EnrollmentTokenJanitor` (5 min), `PinChallengeJanitor` (1 min)
- **Agent** — lightweight Go-style background service; ECDsa keypair on first enroll; exponential backoff 2 → 60 s; heartbeat every 5 s, service report every 60 s
- **Service discovery** — Docker.DotNet for container ports + TCP scan of 18 known services (Plex, Sonarr, Radarr, Home Assistant, Pi-hole, Nextcloud, Gitea, Portainer, Grafana, Prometheus, Syncthing, Vaultwarden, AdGuard, Traefik, Nginx, Jellyfin, Lidarr, Prowlarr)
- **SQL Server schema** — 6 idempotent `IF NOT EXISTS` scripts in `db/tables/` applied by a `db-init` Docker sidecar via `sqlcmd`
- **Docker Compose** — 4 services: `db` (SQL Server 2022), `db-init`, `hub` (port 8080/7777), `agent` (optional `--profile agent`)
- **Agent install scripts** — `install.sh` (Linux x64/arm64/arm, systemd unit) and `install.ps1` (Windows x64, sc.exe service)
- **Tests** — 25 passing unit tests across Domain (13), Application (6), Infrastructure (6); SQLite in-memory for Infrastructure layer

### Technical decisions

- Switched from EF Core migrations to plain SQL scripts to avoid schema-drift risk and keep the database layer legible without ORM knowledge.
- FluentAssertions pinned to 7.2.0 — 8.x introduced breaking constraint API changes that conflicted with `Should()` on reference types.
- `EnsureCreated()` removed from production bootstrap; schema is fully owned by `db-init`.
- Static `ConcurrentDictionary` in `FleetGrpcService` lets `AgentGrpcService` push enrollment events to waiting wizard clients without an extra message-bus dependency.
