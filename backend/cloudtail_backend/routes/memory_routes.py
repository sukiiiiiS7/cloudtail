from __future__ import annotations

import os
import json
from uuid import uuid4
from datetime import datetime
from pathlib import Path
from typing import List, Optional

from fastapi import APIRouter, HTTPException
from fastapi.encoders import jsonable_encoder
from pydantic import BaseModel
from pymongo import ReturnDocument

# Mongo + models + audit log
from cloudtail_backend.database.mongodb import get_memory_collection
from cloudtail_backend.models.memory import MemoryEntry, EmotionEssence
from cloudtail_backend.utils.logging_utils import log_emotion_to_file

router = APIRouter(tags=["memories"])
PROFILE = os.getenv("CLOUDTAIL_PROFILE", "presentation").lower()

# Engine lazy init (first use); cache init error for diagnostics
BASE_DIR = Path(__file__).resolve().parent.parent
CONFIG_PATH = BASE_DIR / "storage" / "emotion_engine_config.json"
_engine = None
_engine_error: Optional[Exception] = None


def _get_engine():
    """Lazy-load EmotionAlchemyEngine with optional JSON config."""
    global _engine, _engine_error
    if _engine is not None or _engine_error is not None:
        return _engine
    try:
        from cloudtail_backend.engine.emotion_engine import EmotionAlchemyEngine
        config = {}
        if CONFIG_PATH.exists():
            try:
                config = json.loads(CONFIG_PATH.read_text(encoding="utf-8"))
            except Exception:
                config = {}
        _engine = EmotionAlchemyEngine.from_dict(config) if config else EmotionAlchemyEngine()
    except Exception as e:
        _engine_error = e
        _engine = None
    return _engine


# ---------- Schemas ----------

class MemoryRequest(BaseModel):
    content: str


class MemoryUpdateRequest(BaseModel):
    manual_override: Optional[str] = None
    is_private: Optional[bool] = None
    keywords: Optional[List[str]] = None


# ---------- Endpoints ----------

@router.post("/memories/", response_model=MemoryEntry, name="upload_memory")
async def upload_memory(request: MemoryRequest) -> MemoryEntry:
    """
    Create one memory:
      1) validate content,
      2) infer emotion via EmotionAlchemyEngine (FULL),
      3) insert into MongoDB,
      4) write audit log.
    """
    content = (request.content or "").strip()
    if not content:
        raise HTTPException(status_code=400, detail="Content cannot be empty.")

    if PROFILE != "full":
        raise HTTPException(status_code=503, detail={"error": "Memories API is available only in FULL profile."})

    engine = _get_engine()
    if engine is None:
        raise HTTPException(
            status_code=503,
            detail={
                "error": "Emotion engine unavailable",
                "hint": "Install torch/transformers and resolve DLL/runtime issues.",
                "engine_init_error": str(_engine_error) if _engine_error else None,
            },
        )

    # infer emotion
    essence: EmotionEssence = engine.extract_emotion(content)

    entry = MemoryEntry(
        id=str(uuid4()),
        content=content,
        timestamp=datetime.utcnow(),
        detected_emotion=essence.type,  # model validators normalize to 4 emotions
    )

    try:
        collection = get_memory_collection()
        await collection.insert_one(entry.model_dump())
    except Exception as e:
        raise HTTPException(status_code=500, detail=f"DB insert failed: {e}")

    try:
        log_emotion_to_file(
            text=content,
            label=essence.type,
            score=essence.value,
            path=BASE_DIR,
            element=essence.element,
        )
    except Exception:
        pass  # logging must not break API

    return entry


@router.get("/memories/", response_model=List[MemoryEntry], name="list_memories")
async def get_memories() -> List[MemoryEntry]:
    """List all memories (FULL only)."""
    if PROFILE != "full":
        raise HTTPException(status_code=503, detail={"error": "Memories API is available only in FULL profile."})

    try:
        collection = get_memory_collection()
        cursor = collection.find({})
        docs = await cursor.to_list(length=1000)
    except Exception as e:
        raise HTTPException(status_code=500, detail=f"DB read failed: {e}")

    out: List[MemoryEntry] = []
    for d in docs:
        d.pop("_id", None)
        out.append(MemoryEntry(**d))
    return out


@router.patch("/memories/{memory_id}", response_model=MemoryEntry, name="update_memory")
async def update_memory(memory_id: str, update: MemoryUpdateRequest) -> MemoryEntry:
    """Update one memory; write audit record if manual_override is provided."""
    if PROFILE != "full":
        raise HTTPException(status_code=503, detail={"error": "Memories API is available only in FULL profile."})

    update_data = {k: v for k, v in update.dict().items() if v is not None}
    if not update_data:
        raise HTTPException(status_code=400, detail="No valid fields to update.")

    collection = get_memory_collection()
    try:
        doc = await collection.find_one_and_update(
            {"id": memory_id},
            {"$set": update_data},
            return_document=ReturnDocument.AFTER,
        )
    except Exception as e:
        raise HTTPException(status_code=500, detail=f"DB update failed: {e}")

    if not doc:
        raise HTTPException(status_code=404, detail="Memory not found.")

    if update.manual_override:
        try:
            log_emotion_to_file(
                text=f"[override] {memory_id}",
                label=update.manual_override,
                score=0.0,
                path=BASE_DIR,
                element="(manual override)",
            )
        except Exception:
            pass

    doc.pop("_id", None)
    return MemoryEntry(**doc)


@router.delete("/memories/{memory_id}", name="delete_memory")
async def delete_memory(memory_id: str) -> dict:
    """Hard-delete a memory by its logical id. Returns {'ok': True, 'deleted': 0|1}."""
    if PROFILE != "full":
        raise HTTPException(status_code=503, detail={"error": "Memories API is available only in FULL profile."})
    try:
        collection = get_memory_collection()
        res = await collection.delete_one({"id": memory_id})
        return {"ok": True, "deleted": int(res.deleted_count)}
    except Exception as e:
        raise HTTPException(status_code=500, detail=f"DB delete failed: {e}")
