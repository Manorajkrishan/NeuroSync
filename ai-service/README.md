# HF-01 — NeuroSync AI microservice

**Role:** Experimental Hugging Face emotion signals.  
**Baseline:** ML.NET inside ASP.NET Core remains the production path until experiments prove otherwise.

## Run locally

```bash
cd ai-service
python -m venv .venv
.venv\Scripts\activate
pip install -r requirements.txt
uvicorn app.main:app --reload --port 8000
```

- Health: `GET http://localhost:8000/health`
- Analyse: `POST http://localhost:8000/v1/emotion/analyse` `{"text":"..."}`

First call downloads `SamLowe/roberta-base-go_emotions`.

## Important

- Uses **sigmoid** multi-label scores — not softmax single-label.
- Do **not** show raw scores in user chat.
- Safety stays in ASP.NET `SafetyGateService` — independent of this model.
