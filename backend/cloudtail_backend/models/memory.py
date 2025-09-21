from __future__ import annotations

from typing import List, Optional
from pydantic import BaseModel, validator
from datetime import datetime

# Canonical four emotions used across the demo build
CANON = {"sadness", "guilt", "nostalgia", "gratitude"}
ALIASES = {
    # → sadness
    "grief": "sadness", "sorrow": "sadness", "melancholy": "sadness",
    # → guilt
    "regret": "guilt", "shame": "guilt", "anger": "guilt", "fear": "guilt", "frustration": "guilt",
    # → nostalgia
    "longing": "nostalgia",
    # → gratitude
    "joy": "gratitude", "love": "gratitude", "hope": "gratitude", "acceptance": "gratitude", "peace": "gratitude",
    "calm": "gratitude", "trust": "gratitude", "contentment": "gratitude", "empathy": "gratitude",
    "warmth": "gratitude", "pride": "gratitude", "compassion": "gratitude",
}

def _canon(label: Optional[str]) -> str:
    """Normalize any raw/alias label to the canonical four; default to 'gratitude'."""
    l = (label or "").lower()
    if l in CANON:
        return l
    return ALIASES.get(l, "gratitude")


# ──────────────────────────────────────────────────────────────────────────────
# Data models
# ──────────────────────────────────────────────────────────────────────────────

class MemoryEntry(BaseModel):
    """User-submitted memory item (stored in DB)."""
    id: str                                  # Unique identifier
    content: str                             # Memory content (text or audio ref)
    timestamp: datetime                      # Submission timestamp (UTC)
    detected_emotion: str                    # System-detected primary emotion
    manual_override: Optional[str] = None    # Optional user-adjusted emotion
    keywords: Optional[List[str]] = None     # Extracted or user-tagged keywords
    is_private: Optional[bool] = False       # Exclude from public inference if True

    # Normalize emotions to the four-category contract
    @validator("detected_emotion", pre=True, always=True)
    def _norm_detected(cls, v: Optional[str]) -> str:
        return _canon(v)

    @validator("manual_override", pre=True)
    def _norm_override(cls, v: Optional[str]) -> Optional[str]:
        return _canon(v) if v is not None else None


class EmotionEssence(BaseModel):
    """Symbolic material extracted from a memory (engine output)."""
    type: str                  # Canonical emotion: sadness | guilt | nostalgia | gratitude
    element: str               # Symbolic element, e.g. "CrystalShard"
    effect_tags: List[str]     # Usage hints, e.g. ["ritual","memory"] or ["healing"]
    value: float               # Intensity score (0.0–1.0)
