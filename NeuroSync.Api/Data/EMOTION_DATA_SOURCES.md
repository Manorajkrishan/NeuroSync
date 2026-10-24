# Emotion model data sources

## Local curated set

`NeuroSync.Api/Data/emotions.csv` contains NeuroSync's small, manually/synthetically curated seed set.
The training pipeline deduplicates normalized text and drops examples whose normalized text has conflicting labels.

## Google Research GoEmotions

The P2 training workflow downloads the official simplified GoEmotions train/dev/test splits at build time from:

- https://github.com/google-research/google-research/tree/master/goemotions
- Dataset license: CC BY 4.0

Raw GoEmotions data is **not committed** to this repository.

NeuroSync keeps only examples whose annotations map cleanly to the current coarse taxonomy:

| GoEmotions label | NeuroSync label |
| --- | --- |
| anger | angry |
| annoyance | frustrated |
| fear, nervousness | anxious |
| sadness, grief, remorse, disappointment | sad |
| joy, gratitude, amusement, love | happy |
| excitement | excited |
| relief | calm |
| neutral | neutral |

Unmapped labels and examples that would map to more than one NeuroSync class are dropped.

This mapping is intentionally conservative. GoEmotions is English Reddit data and is not representative of every population, culture, dialect, or wellbeing context. It is one training source, not a clinical dataset.


## Current P2 evaluated model

The P2 retraining workflow compares the candidate model with the current production model on the same
leakage-filtered GoEmotions held-out set before replacing the model artifact.

Latest evaluated run (2026-10-24 UTC):

- Held-out examples: 2,714
- Production baseline accuracy: 20.97%
- Production baseline macro-F1: 13.42%
- P2 candidate accuracy: 65.66%
- P2 candidate macro-F1: 51.39%
- Macro-F1 improvement: +37.96 percentage points
- Robotic fallback product gate on clear wellbeing messages: <=5%

Per-class results remain part of the generated `emotion-model.metrics.json`. The GoEmotions-to-`calm`
mapping is especially data-sparse (only eight held-out examples in the current filtered test split), so
that class must not be treated as statistically settled. NeuroSync's semantic understanding layer also
handles explicit calm/fatigue/stress language independently of the raw classifier.
