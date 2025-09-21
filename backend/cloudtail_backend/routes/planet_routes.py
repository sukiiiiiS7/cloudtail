from __future__ import annotations

import os
import json
from pathlib import Path
from datetime import datetime
from uuid import uuid4
from typing import Optional

from fastapi import APIRouter
from pydantic import BaseModel

# Internal models / engines (soft imports to avoid hard failures in presentation mode)
try:
    from ..models.planet import PlanetState  # your Pydantic model
except Exception:
    # Minimal fallback to keep typing ok if the import path differs in your tree
    class PlanetState(BaseModel):  # type: ignore
        planetId: str
        planet_key: str   # ambered | rippled | spiral | woven
        mood: str         # sadness | guilt | nostalgia | gratitude
        colorHex: str
        weather: str

router = APIRouter(tags=["planet"])

# Optional file-based preview (kept for offline demo/dev)
DATA_FILE = Path(__file__).resolve().parents[2] / "storage" / "planet.json"

# Canonical four emotions & four planets (demo contract)
CANONICAL_EMOTIONS = {"sadness", "guilt", "nostalgia", "gratitude"}
PLANETS = {"ambered", "rippled", "spiral", "woven"}


def _default_status() -> PlanetState:
    """
    Safe default so the endpoint never breaks during a presentation.
    Ambered (gratitude) → warm palette & clear weather.
    """
    return PlanetState(
        planetId="cloudtail-001",
        planet_key="ambered",
        mood="gratitude",
        colorHex="#F4C96A",
        weather="clear",
    )


def _load_planet_state_from_file() -> PlanetState:
    """
    Load a static planet preview from storage/planet.json if present.
    Otherwise, return a safe default.
    """
    if DATA_FILE.exists():
        try:
            data = json.loads(DATA_FILE.read_text(encoding="utf-8"))
            # Minimal sanity checks
            mood = str(data.get("mood", "gratitude")).lower()
            planet_key = str(data.get("planet_key", "ambered")).lower()
            if mood not in CANONICAL_EMOTIONS:
                mood = "gratitude"
            if planet_key not in PLANETS:
                planet_key = "ambered"
            data["mood"] = mood
            data["planet_key"] = planet_key
            return PlanetState(**data)
        except Exception:
            pass
    return _default_status()


def _derive_status_from_recent_memories() -> PlanetState:
    """
    Try to derive a live planet state from recent memories in MongoDB.
    If anything fails (no DB / no collection / no data), fall back gracefully.
    """
    try:
        # Soft imports to avoid import-time errors in presentation profile
        from ..database.mongodb import get_database  # type: ignore
        from ..models.memory import MemoryEntry      # type: ignore
        from ..engine.planet_engine import infer_planet_state  # type: ignore

        db = get_database()
        coll = db.get_collection("memories")
        # Pull latest 20 entries as a simple window
        cursor = coll.find({}, sort=[("timestamp", -1)], limit=20)
        docs = await_or_sync_list(cursor)

        # Map docs → MemoryEntry (be tolerant to missing fields)
        entries = []
        for d in docs:
            try:
                entries.append(MemoryEntry(**d))
            except Exception:
                # minimal tolerance for legacy docs
                entries.append(
                    MemoryEntry(
                        id=str(d.get("_id", uuid4())),
                        content=str(d.get("content", "")),
                        timestamp=d.get("timestamp", datetime.utcnow()),
                        detected_emotion=str(d.get("detected_emotion", "gratitude")).lower(),
                    )
                )

        status_dict = infer_planet_state(entries)
        # Sanity: enforce canonical labels
        mood = str(status_dict.get("mood", "gratitude")).lower()
        planet_key = str(status_dict.get("planet_key", "ambered")).lower()
        if mood not in CANONICAL_EMOTIONS:
            mood = "gratitude"
        if planet_key not in PLANETS:
            planet_key = "ambered"
        status_dict["mood"] = mood
        status_dict["planet_key"] = planet_key
        return PlanetState(**status_dict)
    except Exception:
        # Any failure → safe default
        return _default_status()


def await_or_sync_list(cursor) -> list:
    """
    Helper that returns a list from a Motor cursor in both async and sync contexts.
    Your original project may already have utilities for this; this is a defensive fallback.
    """
    try:
        # Motor async cursor
        return [doc async for doc in cursor]  # type: ignore
    except TypeError:
        # Synchronous cursor or already materialized
        return list(cursor)


# -----------------------
# Public endpoints
# -----------------------

@router.get("/", response_model=PlanetState)
async def get_planet_preview() -> PlanetState:
    """
    File-based preview for offline demo/debug.
    Host path: /planet/  (prefix is added in main.py)
    """
    return _load_planet_state_from_file()


@router.get("/status", response_model=PlanetState)
async def get_planet_status() -> PlanetState:
    """
    Live planet status derived from recent emotions when DB is available.
    Falls back to a safe default if DB or inference is unavailable.
    Host path: /planet/status  (prefix is added in main.py)
    """
    # If FULL profile is explicitly requested, try DB first.
    profile = os.getenv("CLOUDTAIL_PROFILE", "presentation").lower()
    if profile == "full":
        return _derive_status_from_recent_memories()
    # Presentation profile: keep it deterministic
    return _load_planet_state_from_file()


# -----------------------
# Dev-only seed endpoint
# -----------------------

@router.post("/debug/seed_memories")
async def seed_test_memories() -> dict:
    """
    Seed a few test memories for development.
    Uses canonical 'sadness' to stay consistent with the 4-category contract.
    This endpoint is for development only; DO NOT expose in production.
    """
    try:
        from ..database.mongodb import get_database  # type: ignore
        from ..models.memory import MemoryEntry      # type: ignore

        db = get_database()
        coll = db.get_collection("memories")

        test_contents = [
            "I still remember the last walk at the beach.",
            "I miss the way the bell rang when she came home.",
            "The mirror still has the paw prints.",
            "Footprints on the sand fade too quickly.",
            "The collar feels heavier at night.",
        ]

        inserted = 0
        for content in test_contents:
            entry = MemoryEntry(
                id=str(uuid4()),
                content=content,
                timestamp=datetime.utcnow(),
                detected_emotion="sadness",   # canonical demo choice
            )
            await coll.insert_one(entry.dict())  # motor async driver
            inserted += 1

        return {"ok": True, "inserted": inserted}
    except Exception as e:
        # Fall back to a no-op response if DB is unavailable
        return {
            "ok": False,
            "inserted": 0,
            "error": str(e),
            "hint": "This endpoint requires MongoDB (FULL profile).",
        }
