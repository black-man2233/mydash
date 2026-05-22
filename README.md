# MyDash

Self-hosted homelab dashboard with a hub + agent architecture. One hub serves a Blazor Server web UI; lightweight agents on each machine dial back over gRPC and report ports, Docker containers, and system metrics. Access is gated by SMS PIN.

## Stack

| Layer | Technology |
|---|---|
| Web UI | Blazor Server + MudBlazor 7 |
| Transport | gRPC (hub ↔ agents, mTLS) |
| Database | SQL Server 2022 |
| Schema | SQL scripts in `db/tables/` |
| Auth | SMS PIN → sliding cookie |
| Logging | Serilog → console + rolling file |
| Runtime | .NET 9 |

---

## Prerequisites

- [Docker](https://docs.docker.com/get-docker/) + [Docker Compose v2](https://docs.docker.com/compose/install/)
- (Optional) .NET 9 SDK for local development

---

## Production deploy (Docker)

```bash
# 1. Clone and copy env file
git clone https://github.com/user/mydash && cd mydash
cp .env.example .env
# Edit .env — at minimum set SMS_PHONE and DB_PASSWORD

# 2. Start hub + database (SQL Server is initialised automatically)
docker compose up -d

# 3. Visit the dashboard
open http://localhost:8080
```

SQL Server starts, `db-init` applies all scripts from `db/tables/`, then the hub starts. The PIN gate appears on first visit.

---

## Connect a server (agent enrolment)

1. Open **Fleet** → **Connect Server** in the UI.
2. Give the server a name and click **Next** to get an enrolment token.
3. On the target machine run the install command shown in the wizard:

**Linux / macOS**
```bash
curl -fsSL https://<hub-host>/agent/install.sh | sudo bash -s -- \
  --hub https://<hub-host> \
  --token <TOKEN> \
  --name myserver
```

**Windows (PowerShell)**
```powershell
irm https://<hub-host>/agent/install.ps1 | iex -- `
  -Hub https://<hub-host> -Token <TOKEN> -Name myserver
```

**Docker (same host)**
```bash
docker compose --profile agent up -d \
  -e AGENT_TOKEN=<TOKEN> -e AGENT_NAME=myserver
```

The agent appears on the Fleet page within a few seconds.

---

## Local development

```bash
# Start SQL Server only
docker compose up db db-init -d

# Run the hub
cd src/MyDash.Hub
dotnet run

# Run the agent (separate terminal)
cd src/MyDash.Agent
MYDASH_HUB=http://localhost:8080 MYDASH_TOKEN=<token> dotnet run
```

### Database schema

The schema lives in `db/tables/` as idempotent SQL scripts. To re-apply manually:

```bash
docker compose run --rm db-init
```

To add a new table or column, add/edit the appropriate file in `db/tables/` and re-run `db-init`.

---

## Running tests

```bash
dotnet test
```

All test projects use SQLite in-memory for fast CI runs (Infrastructure layer). No SQL Server needed to run tests.

---

## Environment variables

| Variable | Default | Description |
|---|---|---|
| `DB_PASSWORD` | `MyDash_Str0ng!` | SQL Server SA password |
| `DB_NAME` | `MyDash` | Database name |
| `HUB_HTTP_PORT` | `8080` | Hub web port |
| `HUB_GRPC_PORT` | `7777` | Agent gRPC port |
| `HUB_PUBLIC_URL` | `http://localhost:8080` | Used in wizard install snippets |
| `SMS_PROVIDER` | `TextBelt` | `TextBelt`, `Twilio`, `Vonage`, `ClickSend`, `SMSAPI` |
| `SMS_PHONE` | *(required)* | E.164 phone number for PIN codes |
| `SMS_API_KEY` | `textbelt` | Provider API key |
| `AGENT_TOKEN` | — | Enrolment token (when using `--profile agent`) |
| `AGENT_NAME` | `docker-agent` | Name reported by the bundled agent |

---

## Architecture

```
Browser  ──SignalR──▶  Hub (Blazor Server)
                          │  in-process gRPC
                          ▼
                    Application layer
                          │
                    Infrastructure
                       │       │
                   SQL Server  gRPC server (:7777)
                                     ▲
                              Agent (each machine)
```

See [ARCHITECTURE.md](handoff/ARCHITECTURE.md) for the full layered diagram.
