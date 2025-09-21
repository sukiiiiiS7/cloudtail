from __future__ import annotations
from typing import List, Literal
from pydantic import BaseModel

Emotion = Literal["sadness", "gratitude", "nostalgia", "guilt", "acceptance"]

class CraftRequest(BaseModel):
    emotion_type: Emotion = "nostalgia"

class CraftResponse(BaseModel):
    status: Literal["planned", "ok"] = "planned"
    item_name: str
    element: str
    materials_used: List[str]
    effect_tags: List[str]
    description: str
