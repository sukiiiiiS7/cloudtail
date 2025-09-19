from fastapi import APIRouter, HTTPException
from pydantic import BaseModel
from typing import Literal
from cloudtail_backend.services.crafting_engine import (
    craft_item_from_emotion,
    preview_all_craftables
)

router = APIRouter()

#  Request and Response Models


class CraftRequest(BaseModel):
    emotion_type: Literal[
        "grief", "nostalgia", "guilt", "peace", "hope", "sadness", "gratitude"
    ]


class CraftResponse(BaseModel):
    item_name: str
    element: str
    materials_used: list[str]
    effect_tags: list[str]
    description: str

# POST /craft/

@router.post("/craft/", response_model=CraftResponse)
def craft_item(request: CraftRequest):
    """
    Given an emotion_type, return the first available craftable item
    synthesized from that emotional element and material set.
    """
    result = craft_item_from_emotion(request.emotion_type)
    if result is None:
        raise HTTPException(status_code=404, detail="No craftable item found for this emotion.")
    return result

# ----------------------------------------
# GET /craft/preview


@router.get("/craft/preview", response_model=list[CraftResponse])
def preview_craftables():
    """
    Preview all available craftable items, one for each emotion type.
    Useful for front-end UI, sandbox display, or debugging the config setup.
    """
    return preview_all_craftables()
