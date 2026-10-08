# Companion LLM (V2)

`LlmCompanionProvider` implements async `ICompanionProvider.GenerateAsync(CompanionContext, CancellationToken)` with **template fallback**.

## Interface

```csharp
Task<CompanionReply> GenerateAsync(CompanionContext context, CancellationToken cancellationToken = default);
```

`CompanionContext` includes: current message, intent, mode, safety, uncertainty, emotion signals, recent turns, and consent-approved relevant memory.

## Defaults

| Setting | Default |
|---------|---------|
| `Companion:Llm:Enabled` | `false` |
| `Companion:Llm:BaseUrl` | `https://api.openai.com/v1` |
| `Companion:Llm:Model` | `gpt-4o-mini` |
| `Companion:Llm:TimeoutSeconds` | `12` |

When disabled, missing API key, timeout, or HTTP error → `TemplateCompanionProvider`.

## Enable locally

1. Copy `NeuroSync.Api/appsettings.Example.json` → `appsettings.json` (gitignored).  
   Do not put `#` comments in JSON — JSON does not allow them.
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

- SafetyGate / DecisionEngine remain authoritative **before** companion generation.
- LLM must not control safety decisions or execute IoT actions.
- LLM receives structured `CompanionContext` only — not unrestricted app state.
- HTTP uses `HttpClient.SendAsync` (no sync `.Result` / `.Wait()` / `Send`).
- Do not show internal emotion scores in the UI.
- Branch: `feature/llm-companion` — keep V1 `master` frozen at `v1.0.0` until merge.
