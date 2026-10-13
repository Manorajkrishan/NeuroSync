# Deploy NeuroSync

Local demo (sign-in off unless you set `Auth__RequireAuthentication=true`):

```powershell
dotnet run --project NeuroSync.Api
```

Open `http://localhost:5063/`.

Live beta staging:

```powershell
copy .env.example .env
# Set JWT_SIGNING_KEY, POSTGRES_PASSWORD, NEUROSYNC_DB_CONNECTION, BETA_INVITE_CODE, and NEUROSYNC_ORIGIN.
docker compose up --build
```

- Browser: `https://localhost` (or the hostname in `SITE_ADDRESS`)
- Liveness: `GET /health`
- Readiness: `GET /health/ready`
- Sign-in is required. A shared `X-Api-Key` is not the account system.
- LLM key, if you enable the model: `LLM_API_KEY` on the server only

Details: [LIVE_BETA.md](LIVE_BETA.md) and [OPERATIONS.md](OPERATIONS.md).

## Secrets

Never commit tokens. `appsettings.json` and `.env` are gitignored. Start from `NeuroSync.Api/appsettings.Example.json` and `.env.example`.

Production refuses to start when the JWT signing key is missing or still a placeholder, when CORS origins are empty, when the database is in-memory, or when invite-only mode has no real invite code.

## CORS

Set `Cors__AllowedOrigins__0` to the exact site origin, including `https://`. Production does not allow every origin.

## Facial analysis

Experimental and off by default. It never drives the safety decision.
