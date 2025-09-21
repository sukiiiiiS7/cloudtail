# Cloudtail Backend

> Memory → Emotion → Planet. A creative grief support backend powering the Unity demo.

This service turns free-text memories into **four canonical emotions** and maps them to **four planets** for the Unity visualization.
Ritual and Crafting are **poster-aligned stubs** in the demo build (return example JSON with `status:"planned"`).

---

## Demo Scope

* **Implemented**: memories (optional), emotion extraction, planet status, text-to-planet recommendation.
* **Stubs (poster-aligned)**: `/rituals/*` and `/craft/*` return example data with `status:"planned"`.
* **Canonical emotions**: `sadness`, `guilt`, `nostalgia`, `gratitude`
  (`peace / acceptance / hope / joy / love` → **merged** into `gratitude`).
* **Planets**: `rippled` (sadness), `spiral` (guilt), `woven` (nostalgia), `ambered` (gratitude).

---

## Project Structure

```
cloudtail-backend/
├── models/
│   ├── memory.py          # MemoryEntry, EmotionEssence (labels canonicalized to 4 emotions)
│   └── planet.py          # PlanetState (schema used by /planet)
├── engine/
│   ├── emotion_model.py   # HF pipeline wrapper (CPU/PyTorch)
│   ├── emotion_engine.py  # Raw → canonical mapping (→ four emotions)
│   └── planet_engine.py   # Recent emotions → planet state
├── routes/
│   ├── memory_routes.py   # (full profile) memory read/write
│   ├── planet_routes.py   # /planet, /planet/status
│   ├── recommend_routes.py# /api/recommend (no DB)
│   ├── ritual_routes.py   # (stub) poster-aligned examples
│   └── crafting_routes.py # (stub) poster-aligned examples
├── storage/
│   ├── data.json                  # tiny seed (optional)
│   └── emotion_engine_config.json # mapping/config (no 'peace' output)
├── utils/
│   └── logging_utils.py
├── legacy/               # Archived prototypes (not imported in demo build)
│   ├── crafting_engine.py
│   ├── emotion_extractor.py
│   └── ritual_generator.py
├── main.py
└── requirements.txt
```

**Notes on legacy prototypes**
`backend/cloudtail_backend/legacy/` keeps early prototypes that relied on external JSON templates/configs. They are **not imported** by the demo build.
The final artefact uses:

* `engine/` for emotion extraction (canonical four emotions), and
* stub routes for `/rituals/*` and `/craft/*` aligned with the poster.

---

## Run

```bash
# Presentation profile (no DB required)
# Windows PowerShell:
$env:CLOUDTAIL_PROFILE="presentation"
uvicorn backend.cloudtail_backend.main:app --reload
# Visit:
#   http://127.0.0.1:8000/healthz
#   http://127.0.0.1:8000/planet/status
```

Optional **full** profile (needs MongoDB & connection string) enables `/api/memories/*`.

---

## Endpoints

### GET `/healthz`

Health probe.

```json
{ "ok": true, "profile": "presentation", "version": "1.0.0-four-planets" }
```

### GET `/version`

Service version.

```json
{ "version": "1.0.0-four-planets" }
```

### GET `/planet/status`

Returns the **current planet state**. In presentation mode it reads a small local preview or a safe default; in full mode it derives from recent memories.

#### Response schema: `PlanetState`

| Field              | Type        | Description                                                               |       |           |              |
| ------------------ | ----------- | ------------------------------------------------------------------------- | ----- | --------- | ------------ |
| `state_tag`        | `str`       | High-level state (one of: \`sadness                                       | guilt | nostalgia | gratitude\`) |
| `dominant_emotion` | `str`       | Dominant canonical emotion among recent entries (same four)               |       |           |              |
| `emotion_history`  | `List[str]` | Recent canonicalized emotions (manual overrides preferred when available) |       |           |              |
| `color_palette`    | `List[str]` | Suggested hex colors                                                      |       |           |              |
| `visual_theme`     | `str`       | Client theme id (e.g., `mist`, `wind`, `clear`, `sepia_memory`)           |       |           |              |
| `last_updated`     | `datetime`  | UTC timestamp                                                             |       |           |              |

> Contract guard: any alias (e.g., `grief/sorrow → sadness`, `hope/acceptance/peace/joy/love → gratitude`) is **canonicalized** before returning.

---

### POST `/api/recommend`

Text → top emotions (canonical) → planet key.

**Request**

```json
{ "text": "I still remember the sunset when she left." }
```

**Response (example)**

```json
{
  "topEmotions": [
    { "label": "nostalgia", "score": 0.84 },
    { "label": "sadness",  "score": 0.61 }
  ],
  "planet_key": "woven"   // ambered|rippled|spiral|woven
}
```

**Mapping (aliases → planet)**

* **gratitude-family → ambered**
  `gratitude, joy, love, hope, acceptance, peace, calm, trust, contentment, empathy, warmth, pride, compassion`
* **sadness-family → rippled**
  `sadness, grief, sorrow, melancholy`
* **guilt-family → spiral**
  `guilt, regret, shame, anger, fear, frustration`
* **nostalgia-family → woven**
  `nostalgia, longing`

---

### (Full profile) `/api/memories/*`

Memory CRUD and manual overrides (optional feature for evaluation / debugging).

* `POST /api/memories/` – add a memory → triggers emotion extraction
* `GET /api/memories/` – list
* `PATCH /api/memories/{id}` – supports `manual_override`, `is_private`, `keywords`

**Override precedence**

```python
final = memory.manual_override or memory.detected_emotion
# The chosen label is then canonicalized to one of the four categories before returning.
```

`is_private = true` memories are excluded from planet inference.

---

### (Stubs) `/rituals/*` & `/craft/*`

Poster-aligned **stubs**. They don’t read external templates and don’t persist.

* `GET /rituals/perform` → one example ritual (status: `"planned"`)
* `GET /rituals/recommend` → list of example rituals (status: `"planned"`)
* `POST /craft/` → returns a symbolic item for a canonical emotion (status: `"planned"`)
* `GET /craft/preview` → preview items per emotion (status: `"planned"`)

These endpoints exist to match the poster’s architecture while avoiding over-claiming implementation.

---

## EmotionAlchemyEngine (summary)

* **Model**: `bhadresh-savani/distilbert-base-uncased-emotion` (PyTorch CPU)
* **Canonicalization**: raw labels → `sadness | guilt | nostalgia | gratitude`

  * `anger/fear → guilt`
  * `grief/sorrow/melancholy → sadness`
  * `hope/acceptance/peace/joy/love → gratitude`
* **Output (`EmotionEssence`)**

```json
{ "type": "nostalgia", "element": "EchoBloom", "effect_tags": ["ritual","memory"], "value": 0.84 }
```

* **Heuristics**: optional +0.1 bonus for certain keywords (demo-friendly).

---

## Implementation Notes

* `CLOUDTAIL_PROFILE=presentation` (default) keeps the demo deterministic and DB-free.
* `CLOUDTAIL_PROFILE=full` enables memory endpoints and live planet inference from Mongo.
* Logs: utilities in `utils/logging_utils.py` write compact traces for demo playback.

---

## Changelog (abridged)

* Align **all outputs** to four canonical emotions and four planets.
* Mark **Ritual/Crafting** as **stubs** to match the poster without over-claiming.
* Move early services/templates to **legacy/** to avoid ambiguity.
* Add `/healthz` and `/version` for basic ops hygiene.

---

**Licence**: academic/demo use.
**Acknowledgements**: datasets, models, and inspirations listed in the thesis document.
