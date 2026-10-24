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
