from __future__ import annotations
from typing import List, Dict, Any, Literal
from pydantic import BaseModel

Emotion = Literal["sadness", "gratitude", "nostalgia", "guilt", "acceptance"]

class RitualTemplate(BaseModel):
    status: Literal["planned", "ok"] = "planned"
    ritual_id: str
    ritual_type: Literal["release", "honor", "seal", "reflect"]
    emotion_path: List[Emotion]
    required_planet: str
    script: List[Dict[str, Any]]  # [{action, object, line}]
    effect_tags: List[str]
