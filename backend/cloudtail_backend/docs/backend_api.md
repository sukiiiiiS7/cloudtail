# Cloudtail Backend API Reference

> This document provides a full reference of the backend API endpoints for the Cloudtail grief ritual engine.

---

## Overview

Cloudtail backend uses FastAPI to expose endpoints for:

- Submitting and retrieving grief memories  
- Generating emotional inferences and planetary evolution states  
- Crafting symbolic items from emotions  
- Generating ritual scripts  
- Providing simplified recommendation endpoints for presentation (adapter layer)

All responses are in JSON. Error responses follow standard HTTP status conventions.

---

## Memory Endpoints

### `POST /memories/` – Upload a new memory

**Request Body**:
```json
{
  "content": "She used to curl up by my feet every night."
}
```

**Response**:
Returns a `MemoryEntry` with extracted emotion.

```json
{
  "id": "uuid",
  "content": "...",
  "timestamp": "...",
  "detected_emotion": "nostalgia",
  "manual_override": null,
  "is_private": false,
  "keywords": []
}
```

---

### `GET /memories/` – Fetch all memories

**Query Parameters (future optional)**:
- `emotion`
- `keyword`
- `is_private`

Returns an array of `MemoryEntry` objects.

---

### `PATCH /memories/{memory_id}` – Update a memory

**Request Body (partial allowed)**:
```json
{
  "manual_override": "grief",
  "is_private": true,
  "keywords": ["pet", "ritual"]
}
```

**Note**: Follows FastAPI PATCH logic. Supports partial updates.

---

## Planetary Endpoints

### `GET /planet/status` – Retrieve grief planet state

Returns current planetary evolution status based on emotion history.

**Response**:
```json
{
  "state_tag": "grief",
  "dominant_emotion": "grief",
  "emotion_history": ["grief", "grief", "grief", "guilt", "grief"],
  "color_palette": ["#3E3E72", "#5B5B99"],
  "visual_theme": "ashen",
  "last_updated": "2025-07-18T14:41:57.537408"
}
```

Derived from latest 5 memory entries, prioritizing `manual_override` over `detected_emotion`.

---

## Crafting Endpoints

### `POST /craft/` – Generate emotional item from essence

Transforms a given emotional essence into a symbolic commemorative item, using config-defined mappings.

**Request Body**:
```json
{
  "emotion_type": "nostalgia"
}
```

**Response:**
```json
{
  "item_name": "Echo Lantern",
  "element": "EchoBloom",
  "materials_used": ["EchoBloom", "MemoryPetal"],
  "effect_tags": ["symbolic", "ritual", "nostalgia"],
  "description": "A symbolic item crafted from nostalgia."
}
```

If no match is found for the emotion, returns 404.

---

### `GET /craft/preview` – Preview all craftable items

Returns a preview list of craftable items, one for each emotion type defined in cloudtail_config.json.  
This is useful for UI designers or sandbox visualisation.

**Response**:
```json
[
  {
    "item_name": "Echo Lantern",
    "element": "EchoBloom",
    "materials_used": ["EchoBloom", "MemoryPetal"],
    "effect_tags": ["symbolic", "ritual", "nostalgia"],
    "description": "A symbolic item crafted from nostalgia."
  },
  {
    "item_name": "Phoenix Pearl",
    "element": "CrystalShard",
    "materials_used": ["CrystalShard", "AshDust"],
    "effect_tags": ["symbolic", "ritual", "grief"],
    "description": "A symbolic item crafted from grief."
  }
]
```

**Crafting Logic Summary**  
Crafted items are mapped based on:
- emotion_type: e.g. "nostalgia"
- element + materials: pulled from emotion_map in cloudtail_config.json
- Item selected if any ingredient matches recipe in crafted_items  
Handled in: `services/crafting_engine.py`

---

## Ritual Endpoints

### `POST /ritual/perform` – Select and return ritual script

Uses current planetary state + recent emotion path to choose appropriate ritual template.

**Response**:
```json
{
  "ritual_id": "echo_bloom",
  "script": [
    { "action": "plant", "object": "echo_seed", "line": "From memory, we root the bloom." },
    { "action": "water", "object": "memory_root", "line": "Each tear a seed of something new." },
    { "action": "grow", "object": "bloom_light", "line": "Let the past glow gently in the now." }
  ],
  "effect_tags": ["remembrance", "growth", "reflection"]
}
```

**Ethics Logic**  
Templates requiring “hope” or “rebirth” are blocked if user is still in heavy grief.  
Fallback mechanism prevents premature emotional transitions.

---

### `GET /rituals/recommend` – Recommend rituals based on emotion & planet state

**Query Parameters**:
- `emotion_path`: array of recent emotion strings
- `planet_state`: current planet state (e.g. "grief", "neutral")
- `ritual_type` (optional): filter by ritual type
- `user_override` (optional): override ethics filter

Returns a list of matching ritual templates (max 5 by default).

**Response**:
```json
[
  {
    "ritual_id": "echo_bloom",
    "effect_tags": ["remembrance", "growth", "reflection"],
    "script": [...]
  }
]
```

---

## Recommendation Endpoints (Presentation Adapter)

These endpoints wrap emotion inference and planet mapping into simplified calls for the Unity prototype.  
They are classified as **non-core** and are not part of the main research evaluation.

### `POST /api/recommend` – One-step planet recommendation

**Request Body**:
```json
{
  "content": "She felt warm and quiet like sunset."
}
```

**Response**:
```json
{
  "planet_index": 0,
  "planet_key": "ambered",
  "display_name": "Ambered Haven",
  "emotion": "warmth",
  "confidence": 0.82,
  "reason": "Detected warm or thankful tone; mapped to ambered planet.",
  "essence": {
    "valence": 0.6,
    "arousal": 0.3
  }
}
```

---

### `GET /api/planets` – List available planets

**Response**:
```json
[
  { "key": "ambered", "index": 0, "display_name": "Ambered Haven" },
  { "key": "rippled", "index": 1, "display_name": "Rippled Cove" },
  { "key": "spiral",  "index": 2, "display_name": "Spiral Nest" },
  { "key": "woven",   "index": 3, "display_name": "Woven Meadow" }
]
```

---

## MongoDB Integration Notes

- All memory data is stored in the `memories` collection.  
- Documents follow `MemoryEntry` schema: `id`, `content`, `timestamp`, `detected_emotion`, `manual_override`, `is_private`, `keywords`.  
- Planet state is computed dynamically, not persisted.  
- Rituals are selected from `ritual_templates.json` (local storage).

---

## Endpoint Index

- `POST /memories/` – Upload memory  
- `GET /memories/` – List memories  
- `PATCH /memories/{id}` – Update memory  
- `GET /planet/status` – Get planet state  
- `POST /craft/` – Craft item  
- `GET /craft/preview` – Preview craftable items  
- `POST /ritual/perform` – Perform ritual  
- `GET /rituals/recommend` – Recommend rituals  
- `POST /api/recommend` – Recommend planet (adapter)  
- `GET /api/planets` – List planets (adapter)  
