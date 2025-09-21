from fastapi import APIRouter, Query
from typing import Optional, List
from ..models.ritual import RitualTemplate, DemoEmotion, DemoPlanet

router = APIRouter(tags=["rituals (stub)"])

# Demo ritual templates (static stub)
_DEMO_RITUALS: List[RitualTemplate] = [
    RitualTemplate(
        status="planned",
        ritual_id="ashes_to_light",
        ritual_type="release",
        emotion_path=["sadness", "nostalgia"],
        required_planet="rippled",
        script=[
            {"action": "burn", "object": "memory_shard", "line": "Let it become ash."},
            {"action": "scatter", "object": "ash", "line": "To the winds of remembrance."},
            {"action": "ignite", "object": "sky_flame", "line": "Light the path forward."},
        ],
        effect_tags=["remembrance", "release"],
    ),
    RitualTemplate(
        status="planned",
        ritual_id="thread_of_gratitude",
        ritual_type="honor",
        emotion_path=["gratitude"],
        required_planet="ambered",
        script=[
            {"action": "weave", "object": "light_thread", "line": "Honor threads your memory."},
            {"action": "hang", "object": "sun_locket", "line": "Let it shine in the sky."},
        ],
        effect_tags=["honor", "warmth"],
    ),
]

@router.get("/perform", response_model=RitualTemplate)
def perform_ritual(
    ritual_type: Optional[str] = Query(None),
    preferred_ritual: Optional[str] = Query(None),
) -> RitualTemplate:
    """Return the first ritual matching criteria, else fallback to default."""
    for r in _DEMO_RITUALS:
        if preferred_ritual and r.ritual_id == preferred_ritual:
            return r
        if ritual_type and r.ritual_type == ritual_type:
            return r
    return _DEMO_RITUALS[0]

@router.get("/recommend", response_model=List[RitualTemplate])
def recommend_ritual(
    emotion: DemoEmotion,
    planet: DemoPlanet,
) -> List[RitualTemplate]:
    """Return all rituals that match emotion or planet; fallback to the first one."""
    matches = [r for r in _DEMO_RITUALS if planet == r.required_planet or emotion in r.emotion_path]
    return matches or [_DEMO_RITUALS[0]]
