from __future__ import annotations
from typing import List, Literal
from pydantic import BaseModel

# Canonical four emotions (demo contract)
DemoEmotion = Literal["sadness", "guilt", "nostalgia", "gratitude"]

class CraftRequest(BaseModel):
    """Request body for crafting (stub)."""
    emotion_type: DemoEmotion

class CraftResponse(BaseModel):
    """Response for crafting (stub)."""
    status: str                # always "planned" in the demo stub
    item_name: str
    element: str
    materials_used: List[str]
    effect_tags: List[str]
    description: str
