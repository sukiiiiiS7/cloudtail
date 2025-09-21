# Cloudtail — Reproducibility & Demo Notes (non-evaluative)

**Scope.** This document supports reproducibility of the prototype demo. It is **not** an evaluation or user study. It records how to start services, submit inputs, and observe outputs, together with known limitations.

---

## Environment Fingerprint

Record the following with the submission:
```bash
python -V
git rev-parse --short HEAD
```

**Target stack.**
- OS: Windows 10/11 (tested)
- Python: 3.10–3.12
- Unity: 2022+ (Input System, TextMeshPro)
- Backend: FastAPI (ASGI via Uvicorn)

---

## Profiles, Ports, Health

| Profile      | Port | Purpose                                    |
|--------------|------|--------------------------------------------|
| presentation | 8020 | Minimal demo set for recording             |
| full         | 8010 | Includes `/api/memories/*` for measurement |

Health check: `GET /healthz` → `{"profile":"presentation"|"full"}`.  
Swagger: `http://127.0.0.1:8020/docs` (presentation), `http://127.0.0.1:8010/docs` (full).

<!-- screenshot: swagger_two_profiles.png (side-by-side 8020 docs vs 8010 docs) -->

---

## Start Services (Windows / PowerShell)

```powershell
# Demo profile (presentation)
cd backend
$env:CLOUDTAIL_PROFILE = "presentation"
python -m uvicorn --app-dir . cloudtail_backend.main:app --host 127.0.0.1 --port 8020 --reload
```

```powershell
# Full profile (measurement)
cd backend
$env:CLOUDTAIL_PROFILE = "full"
python -m uvicorn --app-dir . cloudtail_backend.main:app --host 127.0.0.1 --port 8010 --reload
```

Unity front-end: open the demo scene (`Assets/Scenes/DemoScene.unity`) and run; the client posts text to `/api/recommend` and reflects `emotion/planet_key` in the theme.

<!-- screenshot: unity_demo_sequence.png (three frames showing theme change) -->

---

## Request Schema

For compatibility, either field may be used (including both is acceptable).

**Field semantics**
- `content`: user-provided raw text (free text).
- `text`: optionally pre-normalized text (whitespace collapse, punctuation compression, aliasing like `miss u|missyou → miss you`).  
- **Current build behavior**: the backend treats the two fields equivalently by default; **one is sufficient**, and including **both** is valid.

```json
{ "content": "I still remember the sunset", "text": "I still remember the sunset" }
```

Endpoint: `POST /api/recommend` (JSON).  
Response fields: `planet_index`, `planet_key`, `display_name`, `emotion`, `confidence`, `reason`, `essence{...}`.

<!-- screenshot: swagger_recommend_request.png (body with content+text), swagger_recommend_response.png (example JSON) -->

---

## Safe Inputs (for recording)

These inputs produce stable, visually distinct behavior in the current build:
- “Thank you for the evenings” → **gratitude** → `ambered`
- “It still hurts sometimes” → **sadness** → `ambered`
- “missyou!!!” → **guilt** → `rippled` (edge-case demonstration)

> Note. The current configuration aggregates multiple emotions to `planet_key = ambered`. One-to-one mapping is planned post-submission with regression tests.

---

## Probe Snapshot (10 inputs, full mode @ `http://127.0.0.1:8010`)

**Method.** Requests sent to `POST /api/recommend` with both `"content"` and `"text"` fields populated to avoid client/SDK field-name mismatch.

| text                                        | emotion   | planet_key | confidence |
|---------------------------------------------|-----------|------------|------------|
| I long for you every day                    | gratitude | ambered    | 0.983      |
| I yearn for you so much                     | gratitude | ambered    | 1.000      |
| My heart aches and I am crying today        | sadness   | ambered    | 1.000      |
| Grief hits me hard tonight                  | sadness   | ambered    | 1.000      |
| I accept your passing and feel at peace     | gratitude | ambered    | 1.000      |
| I am more at peace with your memory now     | gratitude | ambered    | 1.000      |
| Thank you for all the gentle years          | gratitude | ambered    | 1.000      |
| I am deeply grateful for your companionship | gratitude | ambered    | 0.946      |
| missyou!!!                                  | guilt     | rippled    | 0.956      |
| 我还是很难过，但在慢慢接受                       | gratitude | ambered    | 0.583      |

Artifacts recommended for inclusion: `docs/probe_results.md`, `docs/probe_results.csv`.

---

## Reproduction Script (PowerShell)

```powershell
$URL = 'http://127.0.0.1:8010/api/recommend'   # full profile
$probe = @(
  'I miss you every night','missyou!!!','Thank you for the evenings',
  'It still hurts sometimes','I’m calmer now, thank you','Crying again when I saw the photo',
  'It feels lighter these days','miss u by the window','I still remember the sunset','谢谢你陪我到最后'
)
$rows = foreach ($t in $probe) {
  $b = @{ content = $t; text = $t } | ConvertTo-Json
  try {
    $r = Invoke-RestMethod -Method Post $URL -ContentType 'application/json; charset=utf-8' -Body $b
    [pscustomobject]@{ text=$t; emotion=$r.emotion; planet=$r.planet_key; confidence=$r.confidence; reason=$r.reason }
  } catch {
    [pscustomobject]@{ text=$t; emotion='ERR'; planet='ERR'; confidence=''; reason=$_.Exception.Message }
  }
}
$rows | Export-Csv -NoTypeInformation -Encoding UTF8 .\docs\probe_results.csv
```

---

## Known Failure Modes → Mitigations

- **Emotion→Planet aggregation.** Multiple emotions map to `planet_key=ambered` in the current configuration.  
  *Mitigation*: treat as configuration of the demo build; restore one-to-one mapping and run regression (4×N probes + confusion matrix) post-submission.

- **Nostalgia (longing) absorption.** Nostalgia/longing forms are often mapped into gratitude in this build.  
  *Mitigation*: document as a demo limitation; restore one-to-one mapping post-submission with regression.

- **Longing vs. Gratitude phrasing.** English forms containing *long/yearn/peace/thank* may skew positive.  
  *Mitigation*: avoid such forms in recordings; document as a known limitation in demo builds.

- **Edge/degraded input.** Tokens such as `missyou!!!` may trigger `guilt → rippled`.  
  *Mitigation*: client-side normalization for demo stability (whitespace collapse, punctuation compression, alias `miss u|missyou → miss you`).

- **Mixed-language input.** Multilingual lines reduce confidence.  
  *Mitigation*: use single-language short inputs for recordings; split multilingual content into separate requests.

**Near-term plan (post-submission)**
- Restore **one-to-one** emotion→planet mapping (e.g., `sadness → rippled`, `guilt → spiral`, `nostalgia → woven`, `gratitude → ambered`).  
- Strengthen nostalgia classification and alias set to reduce absorption into gratitude.  
- Ship a minimal client-side normalization toggle (whitespace collapse, punctuation compression, `miss u|missyou → miss you`).  
- Add a regression suite: 4×N probe grid + confusion matrix across profiles.

---

## Data Handling & Ethics

This prototype offers symbolic reflection and is not intended for diagnosis or therapy. In the presentation profile, personal input is not persisted. Limitations and potential biases are disclosed in documentation and demos.
