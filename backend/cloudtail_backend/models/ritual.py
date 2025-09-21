from __future__ import annotations
from typing import List, Optional, Literal
from pydantic import BaseModel

# Canonical four emotions and planet keys (demo contract)
DemoEmotion = Literal["sadness", "guilt", "nostalgia", "gratitude"]
DemoPlanet  = Literal["ambered", "rippled", "spiral", "woven"]

class RitualTemplate(BaseModel):
    """Ritual template returned by ritual stub endpoints."""
    status: str                 # always "planned" in the demo stub
    ritual_id: str
    ritual_type: Optional[str] = None
    emotion_path: List[DemoEmotion]
    required_planet: DemoPlanet
    script: List[dict]
    effect_tags: List[str]
