from fastapi import APIRouter, HTTPException, Query
from cloudtail_backend.models.ritual import RitualTemplate
from cloudtail_backend.engine.planet_engine import infer_planet_state
from cloudtail_backend.utils.emotion import get_final_emotion
from cloudtail_backend.utils.logging_utils import log_ritual_action
from cloudtail_backend.services.ritual_generator import (
    select_best_template,
    ritual_templates,
    is_user_ready_for_transition,
)
from cloudtail_backend.database.mongodb import get_memory_collection

from datetime import datetime
from pathlib import Path
from typing import List, Optional

router = APIRouter()

# Base path for logging
BASE_DIR = Path(__file__).resolve().parent.parent


@router.get("/rituals/perform", response_model=RitualTemplate)
async def perform_ritual(
    user_override: bool = False,
    ritual_type: Optional[str] = Query(None),
    preferred_ritual: Optional[str] = Query(None)
):
    """
    Select and return the most suitable ritual template based on user emotion path.
    Includes ethical guardrails and logs the ritual action.
    """
    try:
        # Load recent memories from database
        collection = get_memory_collection()
        cursor = collection.find({})
        entries = await cursor.to_list(length=100)

        # Build recent emotion path
        emotion_path: List[str] = [
            get_final_emotion(e) for e in entries if get_final_emotion(e) != "unknown"
        ]
        recent_emotions = emotion_path[-5:]

        # Infer current planet state
        planet_state = infer_planet_state(recent_emotions).state_tag

        # Select best ritual
        template = select_best_template(
            emotion_path=recent_emotions,
            current_state=planet_state,
            ritual_type=ritual_type,
            user_override=user_override,
            preferred_ritual=preferred_ritual
        )

        if template is None:
            raise HTTPException(status_code=404, detail="No suitable ritual found.")

        # Log ritual usage
        log_ritual_action(
            ritual_id=template["ritual_id"],
            emotion_path=recent_emotions,
            planet_state=planet_state,
            user_override=user_override,
            base_dir=BASE_DIR
        )

        return template

    except Exception as e:
        raise HTTPException(status_code=500, detail=f"Ritual selection failed: {e}")


@router.get("/rituals/recommend", response_model=list[RitualTemplate])
async def recommend_rituals(
    emotion_path: List[str] = Query(...),
    planet_state: str = Query(...),
    ritual_type: Optional[str] = Query(None),
    user_override: bool = Query(False)
):
    """
    Recommend a list of suitable rituals based on emotion path and planet state.
    Optionally filter by ritual_type. Applies ethical filter on grief dominance.
    Results are sorted by emotional match score.
    """
    try:
        results = []
        for template in ritual_templates:
            required_emotions = template.get("emotion_path", [])
            required_state = template.get("required_state", "")
            template_type = template.get("ritual_type", None)

            if all(e in emotion_path for e in required_emotions) and required_state == planet_state:
                if ritual_type and template_type != ritual_type:
                    continue
                if required_state == "rebirth" and not is_user_ready_for_transition(emotion_path) and not user_override:
                    continue
                results.append(template)

        if not results:
            raise HTTPException(status_code=404, detail="No matching rituals found.")

        def ritual_score(template: dict, user_emotions: List[str]) -> int:
            return sum(1 for e in template.get("emotion_path", []) if e in user_emotions)

        results.sort(key=lambda r: ritual_score(r, emotion_path), reverse=True)

        return results

    except Exception as e:
        raise HTTPException(status_code=500, detail=f"Ritual recommendation failed: {e}")
