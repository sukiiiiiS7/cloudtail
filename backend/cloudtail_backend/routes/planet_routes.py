from fastapi import APIRouter, HTTPException
from cloudtail_backend.models.planet import PlanetState
from cloudtail_backend.engine.planet_engine import infer_planet_state
from cloudtail_backend.utils.emotion import get_final_emotion
from cloudtail_backend.database.mongodb import get_memory_collection

from datetime import datetime
from typing import List
import json
import os

router = APIRouter()

# Optional fallback file-based planet preview
DATA_FILE = "storage/planet.json"

def load_planet_state_from_file() -> PlanetState:
    """
    Load planet state from static JSON.
    Used for preview/fallback mode.
    """
    if not os.path.exists(DATA_FILE):
        return PlanetState(
            state_tag="neutral",
            dominant_emotion="none",
            emotion_history=[],
            color_palette=["#CCCCCC"],
            visual_theme="default",
            last_updated=datetime.utcnow()
        )
    with open(DATA_FILE, "r", encoding="utf-8") as f:
        data = json.load(f)
        return PlanetState(**data)


@router.get("/planet/", response_model=PlanetState)
async def get_planet_state_fallback():
    """
    Return a fallback version of the planet state (file-based preview).
    Can be used for offline rendering or debugging.
    """
    return load_planet_state_from_file()


@router.get("/planet/status", response_model=PlanetState)
async def get_planet_status():
    """
    Return the real-time evolved state of the user's grief planet.
    Based on recent emotional memory entries (manual override preferred).
    """
    try:
        collection = get_memory_collection()
        cursor = collection.find({})
        entries = await cursor.to_list(length=100)

        # Extract final emotion sequence (override if available)
        final_emotions: List[str] = [
            get_final_emotion(e) for e in entries if get_final_emotion(e) != "unknown"
        ]
        recent_emotions = final_emotions[-5:]

        # Infer PlanetState from recent emotions
        state = infer_planet_state(recent_emotions)
        return state

    except Exception as e:
        raise HTTPException(status_code=500, detail=f"Planet status failed: {e}")

# Development/Testing开发完记得删了，但是我怕我忘
@router.post("/debug/seed_memories")
async def seed_test_memories():
    """
    Seed 5 test memory entries for development purposes.
    All entries use "grief" as detected emotion.
    """
    from cloudtail_backend.models.memory import MemoryEntry
    from uuid import uuid4
    from datetime import datetime

    collection = get_memory_collection()
    test_contents = [
        "The house is so quiet without you.",
        "You were always there at the door.",
        "Now it feels like something is missing.",
        "I try to remember the good times.",
        "But sometimes it just hurts."
    ]
    inserted = 0
    for content in test_contents:
        entry = MemoryEntry(
            id=str(uuid4()),
            content=content,
            timestamp=datetime.utcnow(),
            detected_emotion="grief"
        )
        await collection.insert_one(entry.dict())
        inserted += 1

    return {"status": "ok", "inserted": inserted}
