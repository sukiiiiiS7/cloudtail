# Returns the final emotion to use (manual override has priority)
# Canonicalize to the four demo emotions and default to 'gratitude'.
from typing import Optional

_CANON = {"sadness", "guilt", "nostalgia", "gratitude"}
_ALIASES = {
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
    l = (label or "").lower()
    if l in _CANON:
        return l
    return _ALIASES.get(l, "gratitude")

def get_final_emotion(entry: dict) -> str:
    """
    Pick the final emotion: manual override takes precedence over detected.
    The result is canonicalized to: sadness | guilt | nostalgia | gratitude.
    """
    raw = entry.get("manual_override") or entry.get("detected_emotion")
    return _canon(raw)
