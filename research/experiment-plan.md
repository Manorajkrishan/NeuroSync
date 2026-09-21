# NeuroSync HF Research Stage — Experiment Plan

**Working title:** A Comparative Evaluation of Classical and Transformer-Based Multi-Label Emotion Recognition for a Privacy-Aware Adaptive AI Companion

## Hard rule

**ML.NET is the baseline. Hugging Face is the experimental system.** Do not delete ML.NET.

## Research questions

- **RQ1:** Does a transformer-based emotion classifier outperform ML.NET on natural-language emotional signals?
- **RQ2:** Does multi-label prediction represent ambiguous/mixed messages better than single-label?
- **RQ3:** Do confidence-aware decision rules reduce inappropriate emotional assumptions?
- **RQ4:** Can the emotion service integrate into the real-time companion with acceptable latency and privacy?

## Hypotheses

- **H1:** Transformer macro-F1 > ML.NET macro-F1 on shared eval set.
- **H2:** Multi-label better represents mixed emotional expressions.
- **H3:** Per-label thresholds beat universal 0.5.
- **H4:** Transformer adds latency vs ML.NET but remains conversationally acceptable.

## Initial HF benchmark model

`SamLowe/roberta-base-go_emotions` — selected as an **initial multi-label benchmark** over GoEmotions taxonomy, not claimed “best”.

## Companion pipeline (product)

```text
Message → Safety → Intent → Emotion signals (INTERNAL) → Baseline → Mode → ResponsePolicy → Companion response
```

Emotion model = **sensor**. DecisionEngine / companion = **behaviour**.

## Stages

| ID | Deliverable |
|----|-------------|
| HF-01 | FastAPI `/health` + `/v1/emotion/analyse` |
| HF-02 | `IEmotionAiClient` in ASP.NET |
| HF-03 | ML.NET vs RoBERTa shared eval |
| HF-04 | evaluation runner (macro/micro F1, latency) |
| HF-05 | Per-label threshold optimisation |
| HF-06 | Error analysis |
| HF-07 | NeuroSync conversational eval set |
| HF-08 | Fine-tune only after failure analysis |
| HF-09 | Docker + CI |
| HF-10 | Research report |

## Ethics

NeuroSync does **not** diagnose, treat, or predict psychiatric conditions. Outputs are computational linguistic signals.
