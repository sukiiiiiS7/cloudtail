from typing import Tuple
from models.memory import EmotionEssence
import random

# Predefined emotion mappings for simple rule-based extraction
EMOTION_KEYWORDS = {
    "sadness": ["loss", "cry", "empty", "lonely", "miss", "grief"],
    "guilt": ["sorry", "regret", "should", "blame", "fault"],
    "gratitude": ["thank", "grateful", "appreciate", "blessing"],
    "nostalgia": ["remember", "childhood", "once", "old", "used to"],
    "peace": ["calm", "quiet", "still", "accept", "release"]
}

# Map emotion types to symbolic elements and effect tags
EMOTION_MATERIALS = {
    "sadness":    ("crystal shard", ["ritual", "water"]),
    "guilt":      ("rusted ingot", ["ritual", "repair"]),
    "gratitude":  ("lightdust", ["unlock", "blessing"]),
    "nostalgia":  ("echo bloom", ["memory", "vision"]),
    "peace":      ("soft orb", ["closure", "ambient"])
}

def extract_emotion(text: str) -> EmotionEssence:
    lowered = text.lower()
    scores = {e: 0 for e in EMOTION_KEYWORDS}

    # Simple keyword frequency matching
    for emotion, keywords in EMOTION_KEYWORDS.items():
        scores[emotion] = sum(lowered.count(k) for k in keywords)

    # Choose the dominant emotion
    dominant = max(scores, key=scores.get)
    score = scores[dominant]

    # If no keyword matched, fallback to peace
    if score == 0:
        dominant = "peace"
        score = 1

    element, tags = EMOTION_MATERIALS[dominant]
    intensity = min(1.0, 0.2 + 0.1 * score + random.uniform(0.0, 0.2))

    return EmotionEssence(
        type=dominant,
        element=element,
        effect_tags=tags,
        value=round(intensity, 3)
    )
