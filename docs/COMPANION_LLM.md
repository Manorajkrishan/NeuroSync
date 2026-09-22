# Companion LLM (V2)

`LlmCompanionProvider` implements `ICompanionProvider` with **template fallback**.

## Defaults

| Setting | Default |
|---------|---------|
| `Companion:Llm:Enabled` | `false` |
| `Companion:Llm:BaseUrl` | `https://api.openai.com/v1` |
| `Companion:Llm:Model` | `gpt-4o-mini` |
| `Companion:Llm:TimeoutSeconds` | `12` |

When disabled, missing API key, timeout, or HTTP error → `TemplateCompanionProvider`.

## Enable locally

1. Copy `appsettings.Example.json` → `appsettings.json` (gitignored).
2. Set:

```json
"Companion": {
  "Llm": {
    "Enabled": true,
    "ApiKey": "sk-...",
    "BaseUrl": "https://api.openai.com/v1",
    "Model": "gpt-4o-mini"
  }
}
```

Or environment variables:

```text
Companion__Llm__Enabled=true
Companion__Llm__ApiKey=sk-...
```

OpenAI-compatible providers work if `BaseUrl` points at their `/v1` root.

## Behaviour notes

- Safety gate still blocks normal flow before the LLM runs.
- LLM receives structured context (mode, safety, uncertainty, emotion summary) — not unrestricted app state.
- Do not show internal emotion scores in the UI.
- Branch: `feature/llm-companion` — keep V1 `master` frozen at `v1.0.0` until merge.
