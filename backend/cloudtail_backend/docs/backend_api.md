# Cloudtail Backend API Reference

> FastAPI service for the Unity demo. Converts free-text memories to **four canonical emotions**, maps them to **four planets**, and exposes poster-aligned **stubs** for Ritual & Crafting.

All responses are JSON. Errors follow standard HTTP status semantics.

---

## Profiles

* **presentation** (default): no DB required. `/planet/status`, `/api/recommend`, `/rituals/*` (stub), `/craft/*` (stub).
* **full**: enables memory CRUD at `/api/memories/*` (requires MongoDB connection).

---

## Canonical Contract

* Emotions: `sadness`, `guilt`, `nostalgia`, `gratitude`
  (aliases like `grief/sorrow → sadness`, `hope/acceptance/peace/joy/love → gratitude` are **canonicalized** before returning)
* Planets: `rippled` (sadness), `spiral` (guilt), `woven` (nostalgia), `ambered` (gratitude)

---

## Memory Endpoints (full profile)

### `POST /api/memories/` — Upload a memory

**Body**

```json
{ "content": "She used to curl up by my feet every night." }
```

**Response (`MemoryEntry`)**

```json
{
  "id": "uuid",
  "content": "She used to curl up by my feet every night.",
  "timestamp": "2025-09-18T12:30:10Z",
  "detected_emotion": "nostalgia",
  "manual_override": null,
  "is_private": false,
  "keywords": []
}
```

Notes:

* Emotion is extracted by the model, then **canonicalized to the four**.
* A compact log line is written for demo playback.

---

### `GET /api/memories/` — List memories

Returns an array of `MemoryEntry` (Mongo `_id` stripped).

---

### `PATCH /api/memories/{memory_id}` — Update memory

**Body** (partial allowed)

```json
{
  "manual_override": "sadness",
  "is_private": true,
  "keywords": ["pet", "ritual"]
}
```

Effects:

* `manual_override` (if provided) **takes precedence** over the detected label.
* The stored/returned label is **canonicalized** to the four.

---

## Planet Endpoint

### `GET /planet/status` — Current planet state

Derives the planet from recent emotions (full) or returns a safe preview/default (presentation).

**Response (`PlanetState`)**

```json
{
  "state_tag": "nostalgia",
  "dominant_emotion": "nostalgia",
  "emotion_history": ["nostalgia", "sadness", "nostalgia"],
  "color_palette": ["#B9A3D0", "#D3C7E6"],
  "visual_theme": "mist",
  "last_updated": "2025-09-18T12:31:40Z"
}
```

Contract guard: any alias is canonicalized before returning.

---

## Recommendation (Presentation Adapter)

### `POST /api/recommend` — One-step planet recommendation

**Body**

```json
{ "text": "I still remember the sunset when she left." }
```

**Response**

```json
{
  "topEmotions": [
    { "label": "nostalgia", "score": 0.84 },
    { "label": "sadness",  "score": 0.61 }
  ],
  "planet_key": "woven"   // one of: ambered | rippled | spiral | woven
}
```

Mapping (aliases → planet):

* gratitude-family → **ambered**
* sadness-family → **rippled**
* guilt-family → **spiral**
* nostalgia-family → **woven**

---

## Crafting (Stub, poster-aligned)

### `POST /craft/` — Craft symbolic item (stub)

**Body**

```json
{ "emotion_type": "nostalgia" }
```

**Response (`CraftResponse`)**

```json
{
  "status": "planned",
  "item_name": "Echo Lantern",
  "element": "EchoBloom",
  "materials_used": ["MemoryPetal"],
  "effect_tags": ["symbolic", "nostalgia"],
  "description": "A symbolic item crafted from nostalgia (demo stub)."
}
```

Notes:

* No external templates/config files are read.
* Status is **"planned"** to reflect poster alignment.

---

### `GET /craft/preview` — Preview items (stub)

**Response**

```json
[
  {
    "status": "planned",
    "item_name": "Rain Echo Chime",
    "element": "CrystalShard",
    "materials_used": ["AshDust"],
    "effect_tags": ["symbolic", "sadness"],
    "description": "A symbolic item crafted from sadness (demo stub)."
  },
  {
    "status": "planned",
    "item_name": "Mirror of Regret",
    "element": "RustIngot",
    "materials_used": ["Tarnish"],
    "effect_tags": ["symbolic", "guilt"],
    "description": "A symbolic item crafted from guilt (demo stub)."
  },
  {
    "status": "planned",
    "item_name": "Echo Lantern",
    "element": "EchoBloom",
    "materials_used": ["MemoryPetal"],
    "effect_tags": ["symbolic", "nostalgia"],
    "description": "A symbolic item crafted from nostalgia (demo stub)."
  },
  {
    "status": "planned",
    "item_name": "Sun Thread Locket",
    "element": "LightDust",
    "materials_used": ["WarmGlow"],
    "effect_tags": ["symbolic", "gratitude"],
    "description": "A symbolic item crafted from gratitude (demo stub)."
  }
]
```

---

## Rituals (Stub, poster-aligned)

### `GET /rituals/perform` — Return one ritual (stub)

Query (optional): `ritual_type`, `preferred_ritual`

**Response (`RitualTemplate`)**

```json
{
  "status": "planned",
  "ritual_id": "ashes_to_light",
  "ritual_type": "release",
  "emotion_path": ["sadness", "nostalgia"],
  "required_planet": "rippled",
  "script": [
    {"action":"burn","object":"memory_shard","line":"Let it become ash."},
    {"action":"scatter","object":"ash","line":"To the winds of remembrance."},
    {"action":"ignite","object":"sky_flame","line":"Light the path forward."}
  ],
  "effect_tags": ["remembrance", "release"]
}
```

### `GET /rituals/recommend` — Recommend rituals (stub)

Query: `emotion` (one of four), `planet` (ambered/rippled/spiral/woven)

**Response**

```json
[
  {
    "status": "planned",
    "ritual_id": "thread_of_gratitude",
    "ritual_type": "honor",
    "emotion_path": ["gratitude"],
    "required_planet": "ambered",
    "script": [
      {"action":"weave","object":"light_thread","line":"Honor threads your memory."},
      {"action":"hang","object":"sun_locket","line":"Let it shine in the sky."}
    ],
    "effect_tags": ["honor", "warmth"]
  }
]
```

Notes:

* Stubs do **not** read external JSON templates.
* Status remains **"planned"** to match the poster.

---

## Health & Version

### `GET /healthz`

```json
{ "ok": true, "profile": "presentation", "version": "1.0.0-four-planets" }
```

### `GET /version`

```json
{ "version": "1.0.0-four-planets" }
```

---

## MongoDB Notes (full profile)

* Collection: `memories` (schema: `MemoryEntry`).
* Planet state is **computed**, not persisted.
* Private memories (`is_private = true`) are excluded from inference.

---

## Endpoint Index

* `POST /api/memories/` — Upload memory *(full)*
* `GET /api/memories/` — List memories *(full)*
* `PATCH /api/memories/{id}` — Update memory *(full)*
* `GET /planet/status` — Planet state
* `POST /api/recommend` — One-step recommendation
* `POST /craft/` — Craft item *(stub)*
* `GET /craft/preview` — Craftable preview *(stub)*
* `GET /rituals/perform` — One ritual *(stub)*
* `GET /rituals/recommend` — Ritual list *(stub)*

---

需要我把这份直接落到你仓库（替换 `backend_api.md`），或者你还有想保留的旧段落？如果你粘过去后发现哪一处和代码返回还不完全一致，丢个例子我再对到位。
