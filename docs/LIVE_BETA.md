# NeuroSync Live Beta

Invite-only beta for signed-in users. The frozen demo remains `v1.0.0`. This document is the path from that tag to a public release.

## What is in this build

- Email and password accounts (ASP.NET Core Identity) and short-lived JWTs.
- Refresh tokens are hashed at rest and sent only as an HttpOnly cookie.
- The server ignores a client `userId` that does not match the signed-in account.
- Postgres for accounts, consent, preferences, memory, conversation history, and baseline. SQLite is available for a single local process. Tests use the in-memory provider.
- Redis session cache when `Redis:ConnectionString` is set. Otherwise one process keeps the 45-minute session in memory.
- LLM calls use `Companion:Llm:ApiKey` from the server environment. The browser never receives it.
- Daily cost cap, per-user request cap, timeout, and retry. A failed or capped call falls back to the template companion.
- Memory and learning-data sharing stay off until the signed-in user turns them on.

## Run it locally with sign-in

```powershell
$env:ASPNETCORE_ENVIRONMENT = "Development"
$env:Auth__RequireAuthentication = "true"
$env:Auth__Jwt__SigningKey = "replace-this-with-a-unique-32-character-secret"
$env:Database__Provider = "Sqlite"
$env:ConnectionStrings__DefaultConnection = "Data Source=Data/live-beta.db"
dotnet run --project NeuroSync.Api
```

Open `http://localhost:5063/`. Create an account, accept the notice, then chat. `Data/live-beta.db` is gitignored.

Without those variables, Development still runs the old demo mode (no sign-in). Do not put real personal data there.

## Staging with Docker

```powershell
copy .env.example .env
# Replace JWT_SIGNING_KEY, POSTGRES_PASSWORD, NEUROSYNC_DB_CONNECTION, and BETA_INVITE_CODE.
# Placeholder values are rejected on startup.
docker compose up --build
```

Browsers use `https://localhost` (Caddy). The API container is not published on a host port.

## Beta checklist

Invite 10 to 20 people. Give each person the site URL and one invite code. Ask them to try:

- ordinary chat
- Tanglish
- changing the subject mid-conversation
- a long message
- memory on, then memory off
- sign out and sign in on the same browser
- export, then delete the account
- a direct crisis sentence, and a quoted or joking sentence that only mentions crisis words
- refresh the page mid-chat
- two tabs
- a phone browser
- airplane mode, then send again

Keep notes on wrong replies. The labelled set in `NeuroSync.Api.Tests/Evaluation/EvaluationDataset.cs` is the regression gate (about 400 cases, capped for the safety suite). Do not start a custom emotion-model fine-tune until this beta has real failure notes.

## Accounts you still have to create

The repo cannot create these for you:

| Need | Where |
| --- | --- |
| Managed Postgres | Azure Database for PostgreSQL, or another Postgres host |
| Redis | Azure Cache for Redis, or another Redis host |
| HTTPS hostname | Azure Front Door, Caddy, or the host's load balancer |
| LLM key | the provider account, stored only as `Companion__Llm__ApiKey` |
| Error tracking / uptime | the monitor you already use, pointed at `/health/ready` |
| Invite codes | `Beta__InviteCodes__0` and further indexes |

Copy the connection strings into the host's environment, not into `appsettings.json`.
