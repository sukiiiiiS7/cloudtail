from __future__ import annotations

import os
from functools import lru_cache
from typing import Optional

from motor.motor_asyncio import AsyncIOMotorClient
from pymongo.errors import ConfigurationError

# NOTE:
# Do NOT resolve DB/collections at import time. Read env & connect lazily,
# and raise clear errors if envs are missing.

def _get_env(name: str, default: Optional[str] = None) -> Optional[str]:
    v = os.getenv(name, default)
    # normalize empty string to None
    if isinstance(v, str) and v.strip() == "":
        return None
    return v

@lru_cache
def _client() -> AsyncIOMotorClient:
    uri = _get_env("CLOUDTAIL_MONGO_URI")
    if not uri:
        raise RuntimeError("CLOUDTAIL_MONGO_URI is not set")
    try:
        # short timeout so failures fail fast in demo
        return AsyncIOMotorClient(uri, serverSelectionTimeoutMS=3000)
    except ConfigurationError as e:
        raise RuntimeError(f"Mongo URI invalid: {e}") from e

def get_db_name() -> str:
    db = _get_env("CLOUDTAIL_MONGO_DB")
    if not db or not isinstance(db, str):
        raise RuntimeError("CLOUDTAIL_MONGO_DB is not set or invalid")
    return db

def get_db():
    return _client()[get_db_name()]

def get_memory_collection():
    return get_db()["memories"]
