from datetime import datetime
from collections import Counter
from cloudtail_backend.models.planet import PlanetState

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

    # Pick the most frequent emotion as dominant
    dominant = Counter(emotions).most_common(1)[0][0]

    # Define visual styles based on emotion type
    palette_map = {
        "grief": ["#3E3E72", "#5B5B99"],
        "nostalgia": ["#AACFCF", "#DDB0A9"],
        "hope": ["#EFD1D1", "#F8EFD4"],
        "anger": ["#B00020", "#FF8A80"],
        "joy": ["#FFF176", "#FFD54F"],
    }

    theme_map = {
        "grief": "ashen",
        "nostalgia": "sepia_memory",
        "hope": "rebirth_glow",
        "anger": "storm",
        "joy": "lightburst"
    }

    return PlanetState(
        state_tag=dominant,
        dominant_emotion=dominant,
        emotion_history=emotions,
        color_palette=palette_map.get(dominant, ["#AAAAAA"]),
        visual_theme=theme_map.get(dominant, "default"),
        last_updated=datetime.utcnow()
    )
