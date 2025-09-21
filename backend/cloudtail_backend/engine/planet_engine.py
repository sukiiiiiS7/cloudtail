from datetime import datetime
from collections import Counter
from cloudtail_backend.models.planet import PlanetState

# Canonical mapping: collapse many raw labels into 4 categories
def _canonicalize(label: str) -> str:
    l = (label or "").lower()
    if l in ("sadness", "grief", "sorrow", "melancholy"):
        return "sadness"
    if l in ("guilt", "regret", "shame", "anger", "fear", "frustration"):
        return "guilt"
    if l in ("nostalgia", "longing"):
        return "nostalgia"
    if l in ("gratitude", "joy", "love", "hope", "acceptance", "peace", "calm",
             "trust", "contentment", "empathy", "warmth", "pride", "compassion"):
        return "gratitude"
    return "gratitude"

# Visual presets per canonical emotion
PALETTE = {
    "sadness":   ["#3E3E72", "#5B5B99"],   # cool blues / mist
    "nostalgia": ["#AACFCF", "#DDB0A9"],   # faded teal / warm memory
    "guilt":     ["#B00020", "#FF8A80"],   # red / cracks
    "gratitude": ["#FFF176", "#FFD54F"],   # golden light
}
THEME = {
    "sadness": "ashen",
    "nostalgia": "sepia_memory",
    "guilt": "storm",
    "gratitude": "lightburst",
}

# Infers a planet state based on recent emotion sequence
def infer_planet_state(emotions: list[str]) -> PlanetState:
    if not emotions:
        return PlanetState(
            state_tag="neutral",
            dominant_emotion="none",
            emotion_history=[],
            color_palette=["#CCCCCC"],
            visual_theme="default",
            last_updated=datetime.utcnow()
        )

    # Canonicalize before counting
    canon = [_canonicalize(e) for e in emotions]
    dominant = Counter(canon).most_common(1)[0][0]

    return PlanetState(
        state_tag=dominant,
        dominant_emotion=dominant,
        emotion_history=canon,
        color_palette=PALETTE.get(dominant, ["#AAAAAA"]),
        visual_theme=THEME.get(dominant, "default"),
        last_updated=datetime.utcnow()
    )
