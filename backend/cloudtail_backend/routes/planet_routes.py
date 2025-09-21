from __future__ import annotations

import os
from datetime import datetime, timezone, timedelta
from typing import List, Dict, Any

from fastapi import APIRouter, HTTPException

from cloudtail_backend.database.mongodb import get_memory_collection
from cloudtail_backend.models.memory import MemoryEntry
from cloudtail_backend.models.planet import PlanetState  # expects fields below

router = APIRouter(tags=["planet"])
PROFILE = os.getenv("CLOUDTAIL_PROFILE", "presentation").lower()

# emotion -> planet & theming
EMOTION_TO_PLANET = {
    "gratitude": "ambered",
    "guilt":     "rippled",
    "anger":     "spiral",
    "nostalgia": "woven",
}

PLANET_THEME = {
    "ambered": {"visual_theme": "clear", "palette": ["#FAD7A0", "#F8C471"]},
    "rippled": {"visual_theme": "mist",  "palette": ["#95A5A6", "#BDC3C7"]},
    "spiral":  {"visual_theme": "storm", "palette": ["#C0392B", "#7F8C8D"]},
    "woven":   {"visual_theme": "dawn",  "palette": ["#B9A3D0", "#D3C7E6"]},
}


def _default_status() -> PlanetState:
    """Deterministic preview when there's no data."""
    return PlanetState(
        state_tag="steady",
        dominant_emotion="gratitude",
        emotion_history=["gratitude"],
        color_palette=PLANET_THEME["ambered"]["palette"],
        visual_theme=PLANET_THEME["ambered"]["visual_theme"],
        last_updated=datetime.now(timezone.utc).isoformat(),
    )


async def _recent_memories(hours: int = 24) -> List[MemoryEntry]:
    """Fetch recent memories (FULL only)."""
    coll = get_memory_collection()
    since = datetime.utcnow() - timedelta(hours=hours)
    cursor = coll.find({"timestamp": {"$gte": since}}).sort("timestamp", -1)
    docs: List[Dict[str, Any]] = await cursor.to_list(length=200)
    out: List[MemoryEntry] = []
    for d in docs:
        d.pop("_id", None)
        try:
            out.append(MemoryEntry(**d))
        except Exception:
            continue
    return out


def _dominant(emotions: List[str]) -> str:
    if not emotions:
        return "gratitude"
    counts: Dict[str, int] = {}
    for e in emotions:
        counts[e] = counts.get(e, 0) + 1
    # pick max with PLANET order tie-breaker
    ordered = sorted(counts.items(), key=lambda kv: (-kv[1], ["gratitude","guilt","anger","nostalgia"].index(kv[0]) if kv[0] in ["gratitude","guilt","anger","nostalgia"] else 99))
    return ordered[0][0]


@router.get("/", name="get_planet_preview")
async def get_planet_preview():
    """
    Presentation-friendly preview.
    In FULL profile you can still call this to get a stable non-DB example.
    """
    st = _default_status()
    return st


@router.get("/status", name="get_planet_status")
async def get_planet_status():
    """
    Planet live status.
    - FULL: derive from recent Mongo memories.
    - Presentation: fall back to deterministic preview (unless you keep temp demo on).
    """
    if PROFILE != "full":
        return _default_status()

    try:
        recents = await _recent_memories(hours=24)
    except Exception as e:
        # DB issue → safe preview
        return _default_status()

    if not recents:
        return _default_status()

    emo_hist = [m.detected_emotion for m in recents if m.detected_emotion]
    dom = _dominant(emo_hist)
    planet = EMOTION_TO_PLANET.get(dom, "ambered")
    theme = PLANET_THEME[planet]

    return PlanetState(
        state_tag=dom,
        dominant_emotion=dom,
        emotion_history=emo_hist[:12],  # short history
        color_palette=theme["palette"],
        visual_theme=theme["visual_theme"],
        last_updated=datetime.now(timezone.utc).isoformat(),
    )


@router.post("/debug/seed_memories", name="seed_test_memories")
async def seed_test_memories():
    """
    Debug helper: insert 4 labeled memories quickly (FULL only).
    """
    if PROFILE != "full":
        raise HTTPException(status_code=503, detail={"error": "Seeding available only in FULL profile."})

    coll = get_memory_collection()
    now = datetime.utcnow()
    docs = [
        {"id": "seed-1", "content": "Warm light over old streets", "timestamp": now, "detected_emotion": "nostalgia"},
        {"id": "seed-2", "content": "I messed up, I know",         "timestamp": now, "detected_emotion": "guilt"},
        {"id": "seed-3", "content": "thank you for staying",       "timestamp": now, "detected_emotion": "gratitude"},
        {"id": "seed-4", "content": "clenched fists, deep breath", "timestamp": now, "detected_emotion": "anger"},
    ]
    # upsert-like simple insert ignore duplicates
    inserted = 0
    for d in docs:
        try:
            await coll.insert_one(d)
            inserted += 1
        except Exception:
            pass
    return {"ok": True, "inserted": inserted}
