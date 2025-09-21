# Cloudtail Backend

FastAPI-based backend for the Cloudtail demo and measurement builds. Text input is mapped to **four canonical emotions** and further to a **planet key** consumed by the Unity client.

---

## Profiles & Ports

| Profile      | Port | Routes available                                  | Primary use              |
|--------------|------|-----------------------------------------------------|--------------------------|
| presentation | 8020 | `/api/recommend`, `/planet/status` (+ demo stubs)  | Recording / live demo    |
| full         | 8010 | All of the above **+** `/api/memories/*`           | Measurement / probing    |

Health check: `GET /healthz` → `{"profile":"presentation"|"full"}`.

---

## Run

**Windows (PowerShell)**
```powershell
# Presentation (demo)
cd backend
$env:CLOUDTAIL_PROFILE = "presentation"
python -m uvicorn --app-dir . cloudtail_backend.main:app --host 127.0.0.1 --port 8020 --reload
```

```powershell
# Full (measurement)
cd backend
$env:CLOUDTAIL_PROFILE = "full"
python -m uvicorn --app-dir . cloudtail_backend.main:app --host 127.0.0.1 --port 8010 --reload
```

OpenAPI/Swagger:
- `http://127.0.0.1:8020/docs` (presentation)
- `http://127.0.0.1:8010/docs` (full)

Optional scripts: `backend/run_presentation.bat`, `backend/run_full.bat`.

---

## Canonical Emotions & Planets

- Canonical emotions: `sadness`, `guilt`, `nostalgia (longing)`, `gratitude`  
  (aliases normalized; e.g., `longing → nostalgia`, `acceptance/peace/hope/joy/love → gratitude`)
- Planet keys (design set): `rippled` (sadness), `spiral` (guilt), `woven` (nostalgia), `ambered` (gratitude)

> Current build may aggregate multiple emotions into the same `planet_key` (see **Reproducibility**).

---

## API Summary

### POST `/api/recommend`
**Purpose**: map free text to `(emotion, planet_key)`.

**Field semantics (request body)**
- `content`: user-provided raw text (free text).
- `text`: optionally pre-normalized text (e.g., whitespace collapse, punctuation compression, aliasing such as `miss u|missyou → miss you`).
- **Current build behavior**: either field is accepted; **one is sufficient**. Including **both** is valid and does not raise errors. Clients without client-side normalization may send only `content`.

**Example request**
```json
{ "content": "I still remember the sunset", "text": "I still remember the sunset" }
```

**Example response**
```json
{
  "planet_index": 0,
  "planet_key": "ambered",
  "display_name": "Ambered Haven",
  "emotion": "gratitude",
  "confidence": 0.93,
  "reason": "Engine -> ambered",
  "essence": {
    "internal": "gratitude",
    "element": "LightDust",
    "tags": ["healing", "ambient"],
    "raw_value": 0.93
  }
}
```

**Quick probe (PowerShell)**
```powershell
$u='http://127.0.0.1:8010/api/recommend'  # or 8020
$b=@{content='I still remember the sunset'; text='I still remember the sunset'} | ConvertTo-Json
Invoke-RestMethod -Method Post $u -ContentType 'application/json; charset=utf-8' -Body $b
```

### GET `/planet/status`
Returns the current planet state consumed by the Unity demo.

### GET `/healthz`
Returns minimal liveness information including the active `profile`.

### `/api/memories/*`  (full profile only)
CRUD routes for memory entries used by planet inference. Not exposed in the `presentation` profile.

---

## Poster-aligned demo stubs (non-persistent)

These routes mirror the poster architecture and de-risk UI integration. They are **demo-only**, **non-persistent**, and return **deterministic** payloads to support reproducibility. No external templates or storage are used.

| Method | Path                | Purpose                                       | Profile |
|-------:|---------------------|-----------------------------------------------|:-------:|
|  GET   | `/rituals/perform`  | Return a sample ritual descriptor              |  all    |
|  GET   | `/rituals/recommend`| Return a small list of sample rituals          |  all    |
|  POST  | `/craft/`           | Return a symbolic artifact for a given emotion |  all    |
|  GET   | `/craft/preview`    | Return preview artifacts per canonical emotion |  all    |

---

## Reproducibility

- Procedural notes and the ten-probe snapshot: `../docs/Reproducibility.md`  
- Ten-probe artifacts: `../docs/probe_results.md`, `../docs/probe_results.csv`

---

## Known Limitations (current build)

- **Emotion→Planet aggregation**: multiple emotions may map to `planet_key=ambered`.
- **Nostalgia (longing) absorption**: nostalgia/longing forms are often mapped into gratitude.
- **Longing vs. Gratitude phrasing**: English forms containing *long/yearn/peace/thank* may skew positive.
- **Edge/degraded input**: tokens like `missyou!!!` can trigger `guilt → rippled`.
- **Mixed language**: multilingual lines reduce confidence; single-language short inputs are more stable for demos.

**Near-term plan (post-submission)**
- Restore **one-to-one** emotion→planet mapping (e.g., `sadness → rippled`, `guilt → spiral`, `nostalgia → woven`, `gratitude → ambered`).  
- Strengthen nostalgia classification and alias set to reduce absorption into gratitude.  
- Ship a minimal client-side normalization toggle (whitespace collapse, punctuation compression, `miss u|missyou → miss you`).  
- Add a regression suite: 4×N probe grid + confusion matrix across profiles.

---

## Troubleshooting

- `/api/memories/*` not visible in Swagger → active profile is `presentation` (expected). Use `full` or open `:8010/docs`.
- All responses identical → request likely sent to the presentation port (`:8020`) or body field not read; include `"content"` and/or `"text"` in the JSON payload.
- Swagger not sending → **Try it out** must be clicked before **Execute**.
- Port/profile confusion → confirm via `GET /healthz`.

---

## Environment Fingerprint

```bash
python -V
git rev-parse --short HEAD
```
