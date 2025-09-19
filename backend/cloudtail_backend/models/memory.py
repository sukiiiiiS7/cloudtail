from typing import List, Optional
from pydantic import BaseModel
from datetime import datetime

# MemoryEntry represents a user-submitted memory item
class MemoryEntry(BaseModel):
    id: str                       # Unique identifier
    content: str                  # Memory content (text or audio reference)
    timestamp: datetime           # Submission timestamp
    detected_emotion: str         # System-detected primary emotion
    manual_override: Optional[str] = None  # Optional user-adjusted emotion
    keywords: Optional[List[str]] = None   # Extracted or user-tagged keywords
    is_private: Optional[bool] = False     # Controls participation in public rituals

# EmotionEssence is the symbolic material extracted from a memory
class EmotionEssence(BaseModel):
    type: str                     # Emotion category (e.g., sadness, gratitude)
    element: str                  # Game representation (e.g., "crystal shard")
    effect_tags: List[str]        # Tags that determine usage (e.g., "ritual", "healing")
    value: float                  # Emotion intensity score
