from fastapi import APIRouter, HTTPException
from cloudtail_backend.database.mongodb import get_memory_collection
from cloudtail_backend.engine.emotion_engine import EmotionAlchemyEngine
from cloudtail_backend.models.memory import MemoryEntry, EmotionEssence
from cloudtail_backend.utils.logging_utils import log_emotion_to_file
from uuid import uuid4
from datetime import datetime
from pydantic import BaseModel
from fastapi.encoders import jsonable_encoder
import json
from pathlib import Path
from bson import ObjectId

router = APIRouter()

# Emotion Engine Config
BASE_DIR = Path(__file__).resolve().parent.parent
CONFIG_PATH = BASE_DIR / "storage" / "emotion_engine_config.json"

with open(CONFIG_PATH, "r", encoding="utf-8") as f:
    config = json.load(f)

engine = EmotionAlchemyEngine.from_dict(config)


# Request schema for POST
class MemoryRequest(BaseModel):
    content: str


@router.post("/memories/", response_model=MemoryEntry)
async def upload_memory(request: MemoryRequest):
    """
    Upload a new memory and extract its emotional essence.

    This endpoint analyzes the submitted content using the EmotionAlchemyEngine,
    generates a MemoryEntry, stores it in MongoDB, and logs the emotional result.

    Returns:
        MemoryEntry: The created memory object with inferred emotion.
    """
    content = request.content.strip()
    if not content:
        raise HTTPException(status_code=400, detail="Content cannot be empty.")

    # Emotion Extraction
    essence: EmotionEssence = engine.extract_emotion(content)

    # Create MemoryEntry
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

    # Log emotion result
    log_emotion_to_file(essence.type, essence.element, essence.value, BASE_DIR)

    return entry


@router.get("/memories/", response_model=list[MemoryEntry])
async def get_memories():
    """
    Retrieve all stored memory entries from the database.

    Returns:
        List[MemoryEntry]: All memory entries, excluding MongoDB internal fields.
    """
    try:
        collection = get_memory_collection()
        cursor = collection.find({})
        results = await cursor.to_list(length=1000)

        # Remove MongoDB's _id field
        for r in results:
            r.pop("_id", None)

        return results
    except Exception as e:
        raise HTTPException(status_code=500, detail=f"DB read failed: {e}")


# PATCH
class MemoryUpdateRequest(BaseModel):
    manual_override: str | None = None
    is_private: bool | None = None
    keywords: list[str] | None = None


@router.patch("/memories/{memory_id}", response_model=MemoryEntry)
async def update_memory(memory_id: str, update: MemoryUpdateRequest):
    """
    Update an existing memory entry with manual override, privacy setting, or keywords.

    If a manual_override is provided, the override will be logged separately
    for transparency and traceability.

    Returns:
        MemoryEntry: The updated memory object.
    """
    collection = get_memory_collection()

    update_data = {k: v for k, v in update.dict().items() if v is not None}
    if not update_data:
        raise HTTPException(status_code=400, detail="No valid fields to update.")

    result = await collection.find_one_and_update(
        {"id": memory_id},
        {"$set": update_data},
        return_document=True
    )

    if not result:
        raise HTTPException(status_code=404, detail="Memory not found.")

    # Optional: log if user overrides the detected emotion
    if update.manual_override:
        log_emotion_to_file(
            emotion_type=update.manual_override,
            element="(manual override)",
            value=0.0,
            base_dir=BASE_DIR
        )

    return result
