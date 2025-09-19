
# Cloudtail Backend

> A grief ritual engine for memory alchemy, pet loss, and planetary evolution.

This is the backend service for **Cloudtail**, a creative system that transforms memories into emotional essences and rituals, enabling users to build their own evolving grief planets.

---

## Project Structure

```
cloudtail-backend/
├── models/                # Core data models
│   ├── memory.py
│   ├── planet.py
│   ├── crafting.py
│   └── ritual.py
├── routes/                # API endpoints
│   ├── memory_routes.py
│   ├── crafting_routes.py
│   ├── planet_routes.py
│   └── ritual_routes.py
├── services/              # Core logic modules
│   ├── emotion_model.py
│   ├── emotion_engine.py
│   ├── emotion_extractor.py
│   ├── crafting_engine.py
│   └── ritual_generator.py
├── storage/               # Local data (JSON-based)
│   ├── data.json
│   ├── cloudtail_config.json
│   ├── emotion_engine_config.json
│   └── ritual_templates.json
├── main.py
├── requirements.txt
└── README.md
```


---
## Module Overview

### models/
| File | Purpose |
|------|---------|
| `memory.py` | Defines `MemoryEntry` and emotion structure for grief inputs. |
| `planet.py` | Holds the `PlanetState` schema and evolution conditions. |
| `crafting.py` | Describes `EmotionEssence` and crafting logic schemas. |
| `ritual.py` | Defines schema for ritual request/response payloads. |

### routes/
| File | Purpose |
|------|---------|
| `memory_routes.py` | API endpoint to submit memories and extract emotions. |
| `crafting_routes.py` | Accepts emotion elements or keywords and returns crafted commemorative items. |
| `planet_routes.py` | Returns and updates current planet status based on user interaction. |
| `ritual_routes.py` | Generates ritual scripts and performances based on grief state. |
| `crafting_routes.py` | Accepts emotion types and returns crafted symbolic items or previews available combinations. |


### services/
| File | Purpose |
|------|---------|
| `emotion_model.py` | Wraps the HuggingFace model and ensures config alignment. |
| `emotion_engine.py` | Core inference logic with mapping + heuristic bonus logic. |
| `emotion_extractor.py` | Parses and sanitizes text into usable inputs. |
| `crafting_engine.py` | Matches crafting inputs with predefined or generated outputs. |
| `ritual_generator.py` | Generates ritual scripts based on emotional context and history. |

### storage/
| File | Purpose |
|------|---------|
| `data.json` | Local simulation of database for storing user memory and state. |
| `cloudtail_config.json` | Mapping table for emotion→element→tag logic. |
| `emotion_engine_config.json` | Internal config file for symbolic emotion mapping. |
| `ritual_templates.json` | Predefined ritual templates with emotion paths and state conditions. |

---

## GET `/planet/status`

Returns the **current grief planet state** based on recent memory emotions.  
The state is inferred from the last 5 memory entries, using `manual_override` if available, otherwise `detected_emotion`.

---

### Response Schema: `PlanetState`

| Field              | Type         | Description                                                             |
|--------------------|--------------|-------------------------------------------------------------------------|
| `state_tag`        | `str`        | High-level planet state (e.g. "grief", "hope", "nostalgia")             |
| `dominant_emotion` | `str`        | Most frequent emotion in the recent memory path                         |
| `emotion_history`  | `List[str]`  | Final emotions of the last 5 memory entries (manual override preferred) |
| `color_palette`    | `List[str]`  | Suggested hex color palette for visual rendering                        |
| `visual_theme`     | `str`        | Front-end theme identifier (e.g. "ashen", "rebirth_glow")               |
| `last_updated`     | `datetime`   | Timestamp of last generation                                            |

---

### Request Body

```json
{
  "emotion_path": ["grief", "nostalgia", "hope"],
  "current_state": "rebirth"
}
```

### Response Body

```json
{
  "ritual_id": "ashes_to_light",
  "script": [
    {
      "action": "burn",
      "object": "memory_shard",
      "line": "Let it become ash."
    },
    {
      "action": "scatter",
      "object": "ash",
      "line": "To the winds of remembrance."
    },
    {
      "action": "ignite",
      "object": "sky_flame",
      "line": "Light the path forward."
    }
  ],
  "effect_tags": ["cleansing", "transition", "rebirth"]
}
```

---

###  Ethics Layer: Soft Guard

- A built-in safety check ensures we **don’t trigger “hopeful” rituals** if the user is still deep in grief.
- Logic:
  - If last 2 emotions are `"grief"` and ritual requires `"hope"`: ❌ skip.
  - Fallback to a more neutral ritual or return `None`.

```python
def is_user_ready_for_transition(emotion_path: list[str]) -> bool:
    grief_ratio = emotion_path.count("grief") / len(emotion_path)
    return grief_ratio < 0.5
```

Used in `ritual_generator.py` to **filter templates** before rendering.


---

## EmotionAlchemyEngine

`emotion_engine.py` defines a symbolic emotion inference module that transforms user memory texts into internal types used across crafting, rituals, and planetary responses.

### Model

- **Model**: [`bhadresh-savani/distilbert-base-uncased-emotion`](https://huggingface.co/bhadresh-savani/distilbert-base-uncased-emotion)
- **Task**: `text-classification`
- **Label Mapping**:

| Raw Label | Mapped Type |
|-----------|-------------|
| `joy`     | `gratitude` |
| `sadness` | `sadness`   |
| `anger`   | `guilt`     |
| `love`    | `nostalgia` |
| `fear`    | `guilt`     |
| _other_   | `peace`     |

Model wrapper and pipeline setup are handled in `emotion_model.py`.

### Output: `EmotionEssence`

Each extracted result returns a symbolic essence object like:

```json
{
  "type": "nostalgia",
  "element": "EchoBloom",
  "effect_tags": ["ritual", "memory"],
  "value": 0.84
}
```

- `type`: internal emotion type for Cloudtail logic  
- `element`: material used in crafting (e.g., `RustIngot`, `LightDust`, `CrystalShard`)  
- `effect_tags`: tags for ritual logic or item behavior  
- `value`: symbolic emotion intensity, adjusted from model confidence + heuristics  

### Heuristics

- Value is computed as `0.2 + confidence + bonus`, capped at `1.0`
- Bonuses:
  - `+0.1` if `"sorry"` in guilt-related memory
  - `+0.1` if nostalgic keywords like `"sunset"`, `"home"`, `"beach"`, `"smell"` appear

### Batch Testing (Optional)

To test the engine standalone:

```bash
python -m cloudtail_backend.engine.emotion_engine
```

Example Output:

```
INFO:__main__:Extracted [sadness] → [sadness], confidence=0.855
🔹 Batch Results:
  1. sadness → CrystalShard (value: 1.0)
  2. guilt → RustIngot (value: 0.688)
  3. sadness → CrystalShard (value: 1.0)
```

### Notes

- Must disable TensorFlow backend to avoid Keras 3 issues:
  ```python
  os.environ["TRANSFORMERS_NO_TF"] = "1"
  ```
- Located in: `cloudtail_backend/services/emotion_engine.py`
- Used by: `memory_routes.py`, `crafting_engine.py`, and future ritual modules

---
## Emotion Override Logic (`manual_override`)

In the Cloudtail system, each memory (`MemoryEntry`) includes two emotion fields:

| Field              | Source             | Usage                                     |
|-------------------|--------------------|-------------------------------------------|
| `detected_emotion` | Inferred by model   | Default for planetary evolution, rituals  |
| `manual_override`  | User-specified (optional) | **Takes priority** over detected_emotion |

---

### Emotion Priority Logic

In all downstream logic that consumes emotion, the following logic should be used:

```python
emotion = memory.manual_override or memory.detected_emotion
```

If the user provides `manual_override`, it will override the model's prediction.

---

### Example: Planetary Evolution

```python
for memory in all_memories:
    emotion = memory.manual_override or memory.detected_emotion
    update_planet_state(emotion)
```

---

### PATCH Endpoint Summary

The `PATCH /memories/{memory_id}` endpoint supports updating the following fields:

| Field             | Type              | Purpose                                  |
|------------------|-------------------|------------------------------------------|
| `manual_override` | `str | None`      | Override the model-inferred emotion type |
| `is_private`       | `bool | None`     | Marks memory as excluded from public evolution |
| `keywords`         | `list[str] | None` | Tags for filtering, searching, or narrative replay |

---

✅ **Note**: If a memory has `is_private = true`, it should be excluded from planetary evolution logic.

Use: `if not memory.is_private:` in relevant loops.

## ✅ Development Log

### 2025-07-17

- ✅ Designed and implemented memory upload flow: `POST /memories/`
- ✅ Built emotion extraction and alchemy mapping (`EmotionEssence`)
- ✅ Implemented simple crafting engine: `POST /craft/`
- ✅ Implemented planetary state system: `GET /planet/`
- ✅ Created config file `cloudtail_config.json` for emotion-element mapping
- ✅ Added README and project structure tracking
- ✅ Emotion model and engine refactored to support symbolic mapping

> Next steps:  
> 🔜 Ritual scripting module  
> 🔜 Planet visual evolution logic  
> 🔜 User/pet profile endpoints  

---

## 🛠 Development Notes

### Memory Storage Format

The file `cloudtail_backend/storage/data.json` stores all uploaded memory entries.

- It must be a valid **JSON array** (`[]`) at all times.
- Each entry is a `dict` with keys like `id`, `content`, `timestamp`, etc.
- If this file gets corrupted or accidentally edited as a single object (i.e. `{...}`), the `/memories/` POST route will raise a `500 Internal Server Error` due to `json.decoder.JSONDecodeError`.

✅ **To fix:**
Reset `data.json` to `[]` and restart the server.

---
## GET `/planet/status`

Returns the **current grief planet state** based on recent memory emotions.  
The state is inferred from the last 5 memory entries, using `manual_override` if available, otherwise `detected_emotion`.

This interface powers the **planet evolution system**, enabling emotional visualization and ritual feedback.

---

### ✅ Response Schema: `PlanetState`

| Field              | Type         | Description                                                             |
|--------------------|--------------|-------------------------------------------------------------------------|
| `state_tag`        | `str`        | High-level planet state (e.g. "grief", "hope", "nostalgia")             |
| `dominant_emotion` | `str`        | Most frequent emotion in the recent memory path                         |
| `emotion_history`  | `List[str]`  | Final emotions of the last 5 memory entries (manual override preferred) |
| `color_palette`    | `List[str]`  | Suggested hex color palette for visual rendering                        |
| `visual_theme`     | `str`        | Front-end theme identifier (e.g. "ashen", "rebirth_glow")               |
| `last_updated`     | `datetime`   | Timestamp of last generation                                            |

---

### Example Response

```json
{
  "state_tag": "grief",
  "dominant_emotion": "grief",
  "emotion_history": ["grief", "grief", "grief", "grief", "grief"],
  "color_palette": ["#3E3E72", "#5B5B99"],
  "visual_theme": "ashen",
  "last_updated": "2025-07-18T14:41:57.537408"
}
```
---

###  Inference Logic

The system considers the 5 most recent memory entries.

If a memory has manual_override, it takes precedence over detected_emotion.

state_tag and visual_theme are derived from the dominant emotion.

Planet evolution logic is powered by planet_engine.py.

### Related Endpoints

- [`POST /memories/`](#post-memories-upload-memory): Upload a new memory  
- [`PATCH /memories/{id}`](#patch-memoriesmemory_id-update-memory): Override emotion manually  
- [`POST /ritual/perform`](#post-ritualperform-upcoming): Trigger a ritual based on current state *(upcoming)*
**Used by**: front-end visual rendering layer, ritual engine, memory threading system.

---
# Methodology Notes

## 1. Emotion Inference (Engine)

- Based on symbolic mapping (configurable)
- Uses manual override if available
- Final emotion used for downstream logic (planet, rituals, threading)

## 2. Planet State Inference

- Aggregates recent 5 final emotions
- Strategy: majority voting → dominant → visual mapping
- Inspired by symbolic ritual transformation + collective mood

## 3. Ritual Generation (Planned)

- Inputs: emotion path + current state
- Output: script (action + dialogue), effect_tags
- Will use predefined templates + soft variation logic

...


## Notes

- This project prioritises **symbolic ritual**, **emotional pacing**, and **non-linear grief**.  
- Architecture follows a modular and extensible design for creative expansion (procedural animation, emotion-based simulation, etc.)

---
### Emotion → Ritual Flow
graph TD
    A[User Memory Input] --> B[EmotionAlchemyEngine]
    B --> C[EmotionEssence]
    C --> D[Planet State Inference]
    C --> E[Ritual Generator]
    D --> F[Ritual Template Match]
    E --> F
    F --> G[Ritual Script + Effect Tags]

---
### Module Responsibility Map
graph TD
    A[Routes Layer] --> B[Services Layer]
    B --> C1[emotion_engine.py]
    B --> C2[planet_engine.py]
    B --> C3[ritual_generator.py]
    C1 --> D[EmotionAlchemyEngine]
    A --> E[Models + Storage]

## Crafting Preview Endpoint

### GET `/craft/preview`

Returns a list of all possible items that can be crafted from each defined emotion type.

#### ✅ Example Response:

```json
[
  {
    "item_name": "Echo Lantern",
    "element": "EchoBloom",
    "materials_used": ["EchoBloom", "MemoryPetal"],
    "effect_tags": ["symbolic", "ritual", "nostalgia"],
    "description": "A symbolic item crafted from nostalgia."
  },
  ...
]
```
Used in front-end UI to show what players could craft, even if they haven't submitted memories yet.


### 🕯 Memory Visibility Setting: Public vs Private

This option appears only after the sealing ritual is complete.

- **Public**: For users who wish to share their memory as a gentle, outward gesture.
  > “I want the world to know how lovely they were.”

- **Private**: For those who prefer a quiet, internal space to hold grief.

Note: This is not a social sharing feature. It reflects grief pacing — some users seek silent remembrance, others long for shared witnessing.

Made with fireflies and memory dust

