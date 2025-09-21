from __future__ import annotations

from fastapi import APIRouter, HTTPException
from uuid import uuid4
from datetime import datetime
from pathlib import Path
from typing import List, Optional

from pymongo import ReturnDocument
from fastapi.encoders import jsonable_encoder
from pydantic import BaseModel

from cloudtail_backend.database.mongodb import get_memory_collection
from cloudtail_backend.engine.emotion_engine import EmotionAlchemyEngine
from cloudtail_backend.models.memory import MemoryEntry, EmotionEssence
from cloudtail_backend.utils.logging_utils import log_emotion_to_file

import json

router = APIRouter(tags=["memories"])

# Engine config
BASE_DIR = Path(__file__).resolve().parent.parent
CONFIG_PATH = BASE_DIR / "storage" / "emotion_engine_config.json"
with open(CONFIG_PATH, "r", encoding="utf-8") as f:
    config = json.load(f)
engine = EmotionAlchemyEngine.from_dict(config)


# Request schema
class MemoryRequest(BaseModel):
    content: str


@router.post("/memories/", response_model=MemoryEntry)
async def upload_memory(request: MemoryRequest) -> MemoryEntry:
    """
    Upload a new memory, extract its emotion, persist to DB, and log the result.
    """
    content = (request.content or "").strip()
    if not content:
        raise HTTPException(status_code=400, detail="Content cannot be empty.")

    # Emotion extraction
    essence: EmotionEssence = engine.extract_emotion(content)

    # Create entry (validators in model will canonicalize labels to the four)
    entry = MemoryEntry(
        id=str(uuid4()),
        content=content,
        timestamp=datetime.utcnow(),
        detected_emotion=essence.type,
    )

    # Store in MongoDB
    try:
        collection = get_memory_collection()
        await collection.insert_one(jsonable_encoder(entry))
    except Exception as e:
        raise HTTPException(status_code=500, detail=f"DB insert failed: {e}")

    # Log (use correct signature: text, label, score, path, element=None)
    log_emotion_to_file(
        text=content,
        label=essence.type,
        score=essence.value,
        path=BASE_DIR,
        element=essence.element,
    )

    return entry


@router.get("/memories/", response_model=List[MemoryEntry])
async def get_memories() -> List[MemoryEntry]:
    """
    Retrieve all stored memory entries (Mongo internal fields removed).
    """
    try:
        collection = get_memory_collection()
        cursor = collection.find({})
        docs = await cursor.to_list(length=1000)

        out: List[MemoryEntry] = []
        for d in docs:
            d.pop("_id", None)
            out.append(MemoryEntry(**d))
        return out
    except Exception as e:
        raise HTTPException(status_code=500, detail=f"DB read failed: {e}")


class MemoryUpdateRequest(BaseModel):
    manual_override: Optional[str] = None
    is_private: Optional[bool] = None
    keywords: Optional[List[str]] = None


@router.patch("/memories/{memory_id}", response_model=MemoryEntry)
async def update_memory(memory_id: str, update: MemoryUpdateRequest) -> MemoryEntry:
    """
    Update an existing memory entry with manual override, privacy flag, or keywords.
    When manual_override is provided, log an override record for auditability.
    """
    collection = get_memory_collection()

    update_data = {k: v for k, v in update.dict().items() if v is not None}
    if not update_data:
        raise HTTPException(status_code=400, detail="No valid fields to update.")

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

    # Log override (if any) using the correct log signature
    if update.manual_override:
        log_emotion_to_file(
            text=f"[override] {memory_id}",
            label=update.manual_override,
            score=0.0,
            path=BASE_DIR,
            element="(manual override)",
        )

    doc.pop("_id", None)
    return MemoryEntry(**doc)
