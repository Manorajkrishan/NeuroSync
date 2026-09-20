# Deploy NeuroSync V1 (blocker fixes)

## Local (dev)

```bash
cd NeuroSync.Api
# Auth off by default in Development
dotnet run
```

Open `http://localhost:5063/v1.html`

## Production (Docker)

```bash
cp .env.example .env
# set NEUROSYNC_API_KEY and NEUROSYNC_ORIGIN
docker compose up --build
```

- Health: `GET /health` (no API key)
- API: send header `X-Api-Key: <your key>`
- SignalR: ` /emotionHub?api_key=<your key>`
- Demo UI: store key in privacy panel → `localStorage.ns_api_key`

## Secrets

- Never commit tokens. `appsettings.json` is gitignored.
- Use `NeuroSync.Api/appsettings.Example.json` as a template.
- Rotate any keys that were previously committed to git history.

## CORS

Set `Cors__AllowedOrigins__0=https://your-domain` (Production fails closed if empty).

## Facial analysis

Experimental and **off** by default. Requires `FaceAnalysisConsent`. Never drives SafetyGate.
