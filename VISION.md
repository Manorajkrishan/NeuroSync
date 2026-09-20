## V1 freeze (do this first)

See **`docs/V1_REQUIREMENTS.md`**. No voice / wearables / real IoT / new facial product features until `v1.0.0`.

**Positioning:** emotion-aware wellbeing companion / affective computing — **not** a mental-health AI product.

> **One-sentence pitch:** NeuroSync is a privacy-first, emotion-aware AI companion that learns an individual's emotional baseline from multimodal signals and safely adapts conversations, digital experiences and connected environments to support their wellbeing.

NeuroSync does **not** diagnose mental illness or replace therapists. It is a wellbeing support system: **AI + humans**, not AI instead of humans.

---

## Old prototype vs new vision vs what the code does now

| Idea | Old prototype | New vision | Current codebase |
|------|---------------|------------|------------------|
| Goal | Emotion label → auto IoT | Companion + baseline + safe environment | Companion-first; IoT only when asked |
| Output | Single label (`Sad`) | Uncertain multi-signal estimates | Primary + secondary + `SignalEstimates` |
| Response | Template + lights | Modes (listen / talk / calm / …) | Modes + best-friend layer |
| Memory | Little / session | User-controlled long-term | Profile + conversation DB + **privacy delete/view** |
| Baseline | None | Personal norms vs deviation | `EmotionalBaselineService` |
| Safety | Light keywords | Dedicated safety gate | `SafetyGateService` |
| Multimodal | Text (+ sim IoT) | Text → voice → face → wearables (phased) | Text solid; face partial; audio/bio stubs |
| Clinical “Human OS” scores | N/A | Explicitly **out of product framing** | APIs exist but wrapped with **not-a-diagnosis** disclaimer |
| Python / HF models | No | Later FastAPI emotion service | Still ML.NET only (planned V1+) |

### What the new vision removes (from the old pitch)

1. **Auto environment control on every emotion** — replaced by ask-first / “quiet mode?” consent.
2. **“You are Sad” as the product** — replaced by uncertain estimates + choiceful companion replies.
3. **Chatbot that pretends to be a therapist** — replaced by modes, boundaries, and crisis → human resources.
4. **Spying / always-on camera-mic** — vision requires explicit permission and local-first preference.
5. **Giant Big Bang build** — replaced by V1→V5 progressive roadmap.

### What we keep from the old foundation

ASP.NET Core, SignalR, ML.NET text emotion, DecisionEngine, IoT simulator (+ optional real devices), conversation memory, best-friend companion, facial wellbeing path, device sync, tests.

### What stays in code but is de-emphasized

Collapse / “depression-anxiety risk”, identity-purpose coaching, growth “maturity” scores — kept as experimental APIs with disclaimers until redesigned as non-clinical wellbeing timelines. Prefer **baseline + timeline + companion modes** for the public story.

---

## Roadmap

### V1 — Emotion-aware chat *(current focus)*
Text → emotion estimates → safety gate → context → companion modes → response. IoT only on request.

### V2 — Personalisation
Stronger baseline, opt-in memory UI, timeline, preference learning (already started via `/api/privacy/*`).

### V3 — Voice companion
STT + speech characteristics + TTS (ElevenLabs path exists; emotion-from-audio still heuristic).

### V4 — Smart environment
Real lights / DND / music with confirmation.

### V5 — Multimodal fusion
Text + voice + face + wearables → fused estimate (fusion service exists; sensors incomplete).

---

## Research question (Master’s-ready)

> Can a privacy-preserving multimodal AI learn an individual's emotional baseline over time and adapt its responses and environment safely?

Not: “Can AI detect emotions?”
