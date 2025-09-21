from __future__ import annotations

from typing import List, Dict
from fastapi import APIRouter

# Import models with minimal surface; avoid extra dependencies.
try:
    from ..models.crafting import CraftRequest, CraftResponse
except Exception:
    # Minimal fallback to keep the API importable if model path differs.
    from pydantic import BaseModel
    class CraftRequest(BaseModel):  # type: ignore
        emotion_type: str
    class CraftResponse(BaseModel):  # type: ignore
        status: str
        item_name: str
        element: str
        materials_used: List[str]
        effect_tags: List[str]
        description: str

router = APIRouter(tags=["crafting (stub)"])

# Demo recipes aligned with the poster. No external dependencies.
_DEMO_RECIPES: Dict[str, Dict[str, object]] = {
    "sadness":   {"item": "Rain Echo Chime",   "element": "CrystalShard", "mats": ["AshDust"]},
    "guilt":     {"item": "Mirror of Regret",  "element": "RustIngot",    "mats": ["Tarnish"]},
    "nostalgia": {"item": "Echo Lantern",      "element": "EchoBloom",    "mats": ["MemoryPetal"]},
    "gratitude": {"item": "Sun Thread Locket", "element": "LightDust",    "mats": ["WarmGlow"]},
}

_VALID = set(_DEMO_RECIPES.keys())

@router.post("/", response_model=CraftResponse)
def craft_item(request: CraftRequest) -> CraftResponse:
    """
    Stub endpoint: returns a planned symbolic item for a canonical emotion.
    """
    emo = str(request.emotion_type).lower()
    if emo not in _VALID:
        # Fallback to a stable default to avoid demo-time failures.
        emo = "gratitude"
    r = _DEMO_RECIPES[emo]
    return CraftResponse(
        status="planned",
        item_name=str(r["item"]),
        element=str(r["element"]),
        materials_used=list(r["mats"]),  # type: ignore[list-item]
        effect_tags=["symbolic", emo],
        description=f"A symbolic item crafted from {emo} (demo stub).",
    )

@router.get("/preview", response_model=List[CraftResponse])
def preview_craftables() -> List[CraftResponse]:
    """
    Stub endpoint: previews one example item per canonical emotion.
    """
    out: List[CraftResponse] = []
    for emo, r in _DEMO_RECIPES.items():
        out.append(CraftResponse(
            status="planned",
            item_name=str(r["item"]),
            element=str(r["element"]),
            materials_used=list(r["mats"]),  # type: ignore[list-item]
            effect_tags=["symbolic", emo],
            description=f"A symbolic item crafted from {emo} (demo stub).",
        ))
    return out
