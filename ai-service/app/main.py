"""
NeuroSync AI microservice — HF-01 scaffold.
ML.NET remains the ASP.NET baseline. This service is the experimental transformer path.
"""
from __future__ import annotations

import time
from typing import Any

from fastapi import FastAPI
from pydantic import BaseModel, Field

app = FastAPI(
    title="NeuroSync AI Service",
    version="0.1.0",
    description="Experimental Hugging Face emotion signals. Does not diagnose.",
)

# Lazy-loaded so /health works without GPU/model download during CI smoke checks
_emotion_model = None
MODEL_NAME = "SamLowe/roberta-base-go_emotions"


class EmotionRequest(BaseModel):
    text: str = Field(..., min_length=1, max_length=4000)


def get_emotion_model():
    global _emotion_model
    if _emotion_model is None:
        import torch
        from transformers import AutoModelForSequenceClassification, AutoTokenizer

        class EmotionModel:
            def __init__(self) -> None:
                self.tokenizer = AutoTokenizer.from_pretrained(MODEL_NAME)
                self.model = AutoModelForSequenceClassification.from_pretrained(MODEL_NAME)
                self.model.eval()
                self.id2label = self.model.config.id2label

            def predict(self, text: str) -> dict[str, float]:
                encoded = self.tokenizer(
                    text, return_tensors="pt", truncation=True, padding=True, max_length=256
                )
                with torch.no_grad():
                    output = self.model(**encoded)
                probs = torch.sigmoid(output.logits)[0]
                return {self.id2label[i]: float(probs[i]) for i in range(len(probs))}

        _emotion_model = EmotionModel()
    return _emotion_model


@app.get("/health")
def health() -> dict[str, str]:
    return {"status": "healthy", "role": "experimental-hf", "baseline": "mlnet-in-aspnet"}


@app.post("/v1/emotion/analyse")
def analyse_emotion(request: EmotionRequest) -> dict[str, Any]:
    """
    Returns multi-label signal strengths (model scores), NOT clinical facts.
    Uses sigmoid — multiple labels may be active simultaneously.
    """
    started = time.perf_counter()
    model = get_emotion_model()
    signals = model.predict(request.text)
    sorted_signals = sorted(signals.items(), key=lambda item: item[1], reverse=True)
    elapsed_ms = int((time.perf_counter() - started) * 1000)

    top = sorted_signals[0] if sorted_signals else ("neutral", 0.0)
    # Decision confidence is NOT the same as model score — keep humble for short text
    words = len(request.text.split())
    confidence_level = "low" if words <= 2 else ("moderate" if top[1] < 0.55 else "moderate")

    return {
        "model": MODEL_NAME,
        "modelVersion": "hf-01-scaffold",
        "primarySignal": top[0],
        "signals": [{"label": label, "score": score} for label, score in sorted_signals],
        "confidence": {"level": confidence_level, "note": "Model score ≠ psychological probability"},
        "metadata": {"processingMs": elapsed_ms},
        "disclaimer": "Computational linguistic signals only. Not a diagnosis.",
    }
