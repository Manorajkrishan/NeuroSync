# NeuroSync V1 — Safety & Evaluation Release (FROZEN)

**Tag target:** `v1.0.0`  
**Positioning:** Emotion-aware **wellbeing companion / affective computing system** — **not** a mental-health diagnostic product.

## In scope (only)

```text
text input
  → safety gate (separate from emotion)
  → emotion / signal estimation + uncertainty
  → interaction mode
  → companion response (ICompanionProvider)
  → optional user-requested action (e.g. Quiet Mode ask)
```

Also required for V1 completeness:

- AI evaluation dataset + metrics
- Consent management (sensitive collection **default OFF**)
- Privacy: export / delete / retention / log hygiene
- Baseline with confidence + minimum samples
- Decision tracing (internal)
- Deployable demo UI (one strong screen)
- CI on every PR

## Explicitly OUT of V1

- Voice emotion, STT/TTS productisation
- Wearables
- Real IoT hardware integrations as product features
- New facial features (facial wellbeing stays **experimental / optional**; must **not** drive safety)
- LLM wiring (interface only — `ICompanionProvider`)
- Python / Hugging Face service (post-`v1.0.0`)
- Clinical diagnosis, therapy replacement claims

## Success criteria

| Criterion | Bar |
|-----------|-----|
| Unit/regression tests | Existing suite green |
| Evaluation suite | ≥300 labelled conversations with reported metrics |
| Safety | Separate classifier; adversarial cases; low false crisis on jokes/quotes |
| Uncertainty | High / Uncertain / Insufficient / Conflicting — companion may say “I may be reading this wrong” |
| Consent | Memory / EmotionHistory / Face / IoT default OFF |
| Baseline | No “significant deviation” claims below min samples + `baselineConfidence` |
| Trace | Every turn records Safety → Mode → Signals → Baseline → Action |
| CI | GitHub Actions runs `dotnet test` on PR |

## V2 (after v1.0.0) — personalization only

User-controlled memory → timeline → baseline deviation → preference learning → response personalization → small user study (10–20 people). **Not voice.**
