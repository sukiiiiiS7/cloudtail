from __future__ import annotations

from typing import List
from fastapi import APIRouter
from ..models.crafting import CraftRequest, CraftResponse, DemoEmotion

router = APIRouter(tags=["crafting (stub)"])

# Demo recipes (poster-aligned). No external deps.
_DEMO_RECIPES = {
    "sadness":   {"item": "Rain Echo Chime",   "element": "CrystalShard", "mats": ["AshDust"]},
    "guilt":     {"item": "Mirror of Regret",  "element": "RustIngot",    "mats": ["Tarnish"]},
    "nostalgia": {"item": "Echo Lantern",      "element": "EchoBloom",    "mats": ["MemoryPetal"]},
    "gratitude": {"item": "Sun Thread Locket", "element": "LightDust",    "mats": ["WarmGlow"]},
}

@router.post("/", response_model=CraftResponse)
def craft_item(request: CraftRequest) -> CraftResponse:
    """
    Stub endpoint: craft a symbolic item from a canonical emotion.
    Returns status="planned" to indicate this is a poster-aligned stub.
    """
    r = _DEMO_RECIPES[request.emotion_type]
    return CraftResponse(
        status="planned",
        item_name=r["item"],
        element=r["element"],
        materials_used=r["mats"],
        effect_tags=["symbolic", request.emotion_type],
        description=f"A symbolic item crafted from {request.emotion_type} (demo stub).",
    )

@router.get("/preview", response_model=List[CraftResponse])
def preview_craftables() -> List[CraftResponse]:
    """
    Stub endpoint: preview one example item per canonical emotion.
    """
    out: List[CraftResponse] = []
    for emo, r in _DEMO_RECIPES.items():
        out.append(CraftResponse(
            status="planned",
            item_name=r["item"],
            element=r["element"],
            materials_used=r["mats"],
            effect_tags=["symbolic", emo],
            description=f"A symbolic item crafted from {emo} (demo stub).",
        ))
    return out
