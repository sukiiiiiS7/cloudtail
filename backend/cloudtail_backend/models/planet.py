from pydantic import BaseModel
from datetime import datetime
from typing import List

class PlanetState(BaseModel):
    state_tag: str                # One of: sadness | guilt | nostalgia | gratitude
    dominant_emotion: str         # Mode of recent canonical emotions (same four)
    emotion_history: List[str]    # Recent sequence (already canonicalized)
    color_palette: List[str]      # Suggested HEX colors for this state
    visual_theme: str             # Frontend theme key, e.g. "mist"/"wind"/"clear"/"sepia_memory"
    last_updated: datetime        # UTC timestamp
