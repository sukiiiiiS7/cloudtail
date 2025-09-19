from __future__ import annotations

import os
import logging
from typing import Dict, List, Optional
from fastapi import APIRouter
from pydantic import BaseModel

# Use a per-module logger
logger = logging.getLogger(__name__)

# When enabled, surface the real exception message to the client for debugging.
DEBUG_RECOMMEND = os.getenv("DEBUG_RECOMMEND", "0") == "1"

# Relative import so editors/runtime agree on resolution.
from ..engine.emotion_engine import EmotionAlchemyEngine

router = APIRouter(tags=["recommend"])

# ---- Planet catalog (order must match Unity's PlanetSwitcher.planets) ----
PLANET_ORDER: List[str] = ["ambered", "rippled", "spiral", "woven"]

PLANET_DISPLAY: Dict[str, str] = {
    "ambered": "Ambered Haven",
    "rippled": "Rippled Cove",
    "spiral":  "Spiral Nest",
    "woven":   "Woven Meadow",
}

# ---- Mapping from emotion labels to planet keys ----
# Covers both raw model labels (joy, love, anger...) and internal types (gratitude, nostalgia...).
EMOTION_TO_PLANET: Dict[str, str] = {
    # Positive / warm → ambered
    "warmth": "ambered",
    "gratitude": "ambered",
    "joy": "ambered",
    "love": "ambered",
    "hope": "ambered",
    "compassion": "ambered",
    "pride": "ambered",

    # Heavy / turbulent → rippled
    "sadness": "rippled",
    "grief": "rippled",
    "guilt": "rippled",
    "fear": "rippled",
    "anger": "rippled",
    "frustration": "rippled",
    "shame": "rippled",

    # Calm / understanding → spiral
    "calm": "spiral",
    "acceptance": "spiral",
    "trust": "spiral",
    "contentment": "spiral",
    "empathy": "spiral",

    # Longing / nostalgic → woven
    "longing": "woven",
    "nostalgia": "woven",
    "melancholy": "woven",
}

# ---- Request/Response models ----
class RecommendReq(BaseModel):
    content: str

class RecommendResp(BaseModel):
    planet_index: int
    planet_key: str
    display_name: str
    emotion: str
    confidence: float
    reason: str
    essence: Optional[dict] = None  # optional extra payload for debugging/UI

# Single engine instance (stateless usage here is fine)
_engine = EmotionAlchemyEngine()

def _planet_index(planet_key: str) -> int:
    try:
        return PLANET_ORDER.index(planet_key)
    except ValueError:
        return len(PLANET_ORDER) - 1  # fallback to last (woven)

@router.post("/recommend", response_model=RecommendResp)
def recommend(req: RecommendReq) -> RecommendResp:
    """
    One-shot recommendation: text -> emotion -> planet.
    Uses EmotionAlchemyEngine (HF pipeline inside) and a simple mapping table.
    """
    try:
        # Extract internal essence using the engine (may map raw 'joy' -> 'gratitude', etc.)
        essence = _engine.extract_emotion(req.content)
        # essence.type is the engine's internal emotion label (lowercase)
        internal = (essence.type or "").lower().strip()

        # Prefer internal label; if not mapped, try raw HF label by re-predicting via wrapper (optional)
        target_key = None
        if internal in EMOTION_TO_PLANET:
            target_key = EMOTION_TO_PLANET[internal]
            chosen_label = internal
            confidence = float(getattr(essence, "value", 0.0))  # your engine packs a value in [0,1]
        else:
            # Optional: read raw label via the engine's model wrapper for coverage
            try:
                raw_label, raw_conf = _engine.model.predict(req.content)  # (label, score)
                raw_label = (raw_label or "").lower().strip()
                if raw_label in EMOTION_TO_PLANET:
                    target_key = EMOTION_TO_PLANET[raw_label]
                    chosen_label = raw_label
                    confidence = float(raw_conf)
            except Exception as inner:
                # Log but do not fail the whole request; we can still fallback.
                logger.exception("Raw label probe failed: %s", inner)
                chosen_label = "unknown"
                confidence = 0.0

        if not target_key:
            # Final fallback (unmapped emotions)
            target_key = "woven"
            chosen_label = internal or "unknown"
            confidence = float(getattr(essence, "value", 0.0) or 0.0)

        idx = _planet_index(target_key)
        return RecommendResp(
            planet_index=idx,
            planet_key=target_key,
            display_name=PLANET_DISPLAY.get(target_key, target_key.title()),
            emotion=chosen_label,
            confidence=round(confidence, 3),
            reason=f"Mapped '{chosen_label}' → {target_key}",
            essence={
                "internal": internal,
                "element": getattr(essence, "element", None),
                "tags": getattr(essence, "effect_tags", []),
                "raw_value": getattr(essence, "value", None),
            },
        )

    except Exception as e:
        # Make errors visible in dev; keep adapter alive under failures.
        logger.exception("recommend failed")
        reason = f"Engine error: {type(e).__name__}: {e}" if DEBUG_RECOMMEND else "Engine error; fallback applied."
        return RecommendResp(
            planet_index=_planet_index("woven"),
            planet_key="woven",
            display_name=PLANET_DISPLAY["woven"],
            emotion="unknown",
            confidence=0.0,
            reason=reason,
            essence=None,
        )
