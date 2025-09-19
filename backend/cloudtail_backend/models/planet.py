from pydantic import BaseModel
from datetime import datetime
from typing import List

class PlanetState(BaseModel):
    state_tag: str                # High-level label for planet mood/state
    dominant_emotion: str         # Most frequent emotion in recent history
    emotion_history: List[str]    # Recent sequence of final emotions
    color_palette: List[str]      # Suggested colors for visual feedback (hex)
    visual_theme: str             # Frontend theme identifier (e.g. "rebirth_glow")
    last_updated: datetime        # Timestamp of last state generation
