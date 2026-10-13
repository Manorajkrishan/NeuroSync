# Operations

## Health

- `GET /health` — process is up. No authentication.
- `GET /health/ready` — database, and Redis when it is configured.
- `GET /api/ops/metrics` — counters for turns, fallbacks, token totals, estimated spend, and last latency. Send `X-Metrics-Key`. The route is 404 until `Ops:MetricsKey` is set. The payload has no message text.

Logs are structured lines on stdout: method, path, status, elapsed milliseconds. Query strings and bodies are not part of the request log. Audit rows record the action name, account id, status, and a hash of the client address. They do not record messages, passwords, or tokens.

## What to alert on

- `/health/ready` failing for more than two minutes
- a burst of HTTP 500s
- LLM fallback or timeout counters climbing while `Companion:Llm:Enabled` is true
- estimated daily spend passing 80% of `Companion:Llm:DailyCostLimitUsd`
- backup job failure

Point the uptime check at `/health/ready`. Ship container stdout to the host's log store (Azure Monitor, the platform's log drain, or similar).

## Database

Production uses Postgres and EF Core migrations (`Database:ApplyMigrations=true`). The migration history is created with `dotnet ef` from this repo.

```powershell
dotnet tool restore
dotnet tool run dotnet-ef migrations add <Name> --project NeuroSync.Api --startup-project NeuroSync.Api
```

SQLite (`Database:Provider=Sqlite`) creates its schema from the model for a single local process. It is not the production migration path.

Account deletion removes the Identity user and, by foreign key, consent, preferences, memory, conversation history, baseline, and refresh tokens. Older conversation rows are also deleted by the daily retention sweep (`Database:RetentionDays`, default 365).

## Backup and restore

With the compose Postgres service running:

```powershell
./scripts/postgres-backup.ps1
./scripts/postgres-restore-test.ps1 -Backup backups/<file>.sql
```

The restore test loads the dump into `neurosync_restore_test`, checks that `AspNetUsers` is present, and drops that database. It does not replace the live database.

Take a backup before a migration on a shared host. Store dumps outside the container disk. A managed Postgres service should also have its own automated backups; this script is the copy you can rehearse.

## Secrets

Rotate by setting a new value and restarting the API:

- `Auth__Jwt__SigningKey` — existing access tokens stop validating. Users sign in again. Revoke refresh tokens by leaving the old hashes in place; they no longer match new cookies if you also clear the `RefreshTokens` table.
- `Companion__Llm__ApiKey` — next LLM call uses the new key. Nothing in the browser changes.
- `POSTGRES_PASSWORD` and `NEUROSYNC_DB_CONNECTION` — update both together. `dotnet ef` reads `NEUROSYNC_DESIGN_CONNECTION` from the environment and has no connection string in source.
- `Ops__MetricsKey` and invite codes — replace the previous value; the old one stops working.

Do not put these in `appsettings.json`, the repo, or `localStorage`.

## Dependency review

CI runs `dotnet list package --vulnerable --include-transitive`. Dependabot opens weekly NuGet and GitHub Actions updates.
