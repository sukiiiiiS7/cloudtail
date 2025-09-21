# Release Notes — v1.0.0-demo

## What’s in this build
- Unity front-end + FastAPI backend
- Two profiles:
  - `presentation` (port 8020): minimal, non-persistent
  - `full` (port 8010): includes `/api/memories/*`
- API focus: `POST /api/recommend`, `GET /planet/status`, `GET /healthz`
- Poster-aligned demo stubs: `/rituals/*`, `/craft/*` (deterministic, non-persistent)
- Docs:
  - Root README (with visuals)
  - `backend/cloudtail_backend/docs/backend_api.md`
  - `backend/cloudtail_backend/docs/Reproducibility.md` (+ probe artifacts)

## Known limitations (snapshot)
- Nostalgia (longing) may be absorbed into gratitude in this build
- Multiple emotions may map to `planet_key=ambered`

## Reproducibility
- Start commands for both profiles
- 10-probe script (PowerShell)
- Probe artifacts in `backend/cloudtail_backend/docs/`

## Next steps
- Restore 1:1 emotion→planet mapping
- Strengthen nostalgia classification/aliases
- Add regression suite (4×N probes + confusion matrix)
