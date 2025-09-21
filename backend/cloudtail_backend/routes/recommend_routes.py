from __future__ import annotations

import os
from typing import Optional

from fastapi import APIRouter, HTTPException
from pydantic import BaseModel

# shape hint only
from cloudtail_backend.models.memory import EmotionEssence

router = APIRouter(tags=["recommend"])
PROFILE = os.getenv("CLOUDTAIL_PROFILE", "presentation").lower()
ALLOW_FALLBACK = os.getenv("ALLOW_FALLBACK", "0") == "1"

# ---------- Planet mapping (keep in sync with frontend) ----------
PLANET_ORDER = ["ambered", "rippled", "spiral", "woven"]

DISPLAY_NAMES = {
    "ambered": "Ambered Haven",
    "rippled": "Rippled Cove",
    "spiral":  "Spiral Vale",
    "woven":   "Woven Garden",
}

# canonical emotion -> planet key
EMOTION_TO_PLANET = {
    "gratitude": "ambered",
    "guilt":     "rippled",
    "anger":     "spiral",
    "nostalgia": "woven",
}

# ---------- Lazy engine ----------
_engine = None
_engine_error: Optional[Exception] = None

def _get_engine():
    """Import and construct EmotionAlchemyEngine on first use."""
    global _engine, _engine_error
    if _engine is not None or _engine_error is not None:
        return _engine
    try:
        from cloudtail_backend.engine.emotion_engine import EmotionAlchemyEngine
        _engine = EmotionAlchemyEngine()
    except Exception as e:
        _engine_error = e
        _engine = None
    return _engine


# ---------- Schemas ----------
class RecommendRequest(BaseModel):
    content: str


# ---------- Endpoint ----------
@router.post("/recommend", name="recommend")
def recommend(req: RecommendRequest):
    """
    Recommend a planet based on the text's emotion.

    FULL: must use EmotionAlchemyEngine (no fallback).
    PRESENTATION: if ALLOW_FALLBACK=1, use a deterministic mapping.
    """
    text = (req.content or "").strip()
    if not text:
        raise HTTPException(status_code=400, detail="content is required")

    # FULL profile: real engine path
    if PROFILE == "full":
        engine = _get_engine()
        if engine is None:
            raise HTTPException(
                status_code=503,
                detail={
                    "error": "Emotion engine unavailable",
                    "hint": "Install torch/transformers and resolve DLL/runtime issues",
                    "engine_init_error": str(_engine_error) if _engine_error else None,
                },
            )

        # use extract_emotion (NOT infer)
        ess: EmotionEssence = engine.extract_emotion(text)
        emotion = ess.type
        key = EMOTION_TO_PLANET.get(emotion, "ambered")
        idx = PLANET_ORDER.index(key)

        return {
            "planet_index": idx,
            "planet_key": key,
            "display_name": DISPLAY_NAMES[key],
            "emotion": emotion,
            "confidence": round(float(ess.value), 3),
            "reason": f"Engine -> {key}",
            "essence": {
                "internal": emotion,
                "element": getattr(ess, "element", None),
                "tags": list(getattr(ess, "tags", []) or []),
                "raw_value": float(getattr(ess, "value", 0.0)),
            },

        }

    # Presentation (demo) path
    if not ALLOW_FALLBACK:
        raise HTTPException(status_code=503, detail="Presentation mode without fallback is disabled")

    # toy heuristic: contains 'sunset' -> nostalgia, else gratitude
    emo = "nostalgia" if "sunset" in text.lower() else "gratitude"
    key = EMOTION_TO_PLANET[emo]
    idx = PLANET_ORDER.index(key)

    return {
        "planet_index": idx,
        "planet_key": key,
        "display_name": DISPLAY_NAMES[key],
        "emotion": emo,
        "confidence": 1.0,
        "reason": f"Fallback mapped '{emo}' -> {key}",
        "essence": {
            "internal": emo,
            "element": "LightDust",
            "tags": ["ambient"],
            "raw_value": 1.0,
        },
    }
