from __future__ import annotations

import os
from datetime import datetime, timezone
from typing import Optional

from fastapi import FastAPI
from fastapi.middleware.cors import CORSMiddleware
from fastapi.responses import JSONResponse

PROFILE = os.getenv("CLOUDTAIL_PROFILE", "presentation").lower()

app = FastAPI(
    title="Cloudtail API",
    description="Memory → Emotion → Planet",
    version="1.0.0-four-planets",
)

# ---------------- CORS ----------------
app.add_middleware(
    CORSMiddleware,
    allow_origins=os.getenv("ALLOWED_ORIGINS", "*").split(","),
    allow_credentials=True,
    allow_methods=["*"],
    allow_headers=["*"],
)

# ------------- Helper: robust import with fallback -------------
def _import_router(*module_paths: str):
    """
    Attempts to import `router` from possible module paths.
    Returns the router if any path succeeds, otherwise None.
    """
    errors = []
    for path in module_paths:
        try:
            mod = __import__(path, fromlist=["router"])
            return getattr(mod, "router")
        except Exception as e:
            errors.append(f"{path}: {e}")
    if errors:
        print("[warn] router import failed:\n  " + "\n  ".join(errors))
    return None


def _include_router_safe(router, prefix: str, name: str):
    """
    Includes a router into the app if not None; prints concise log.
    """
    if router is not None:
        app.include_router(router, prefix=prefix)
        print(f"[ok] mounted {name:>10s} → {prefix}")
    else:
        print(f"[skip] {name:>10s} not mounted")


# ------------- Dev/Diagnostics -------------
@app.get("/__routes")
def __routes():
    return JSONResponse(
        [
            {
                "path": r.path,
                "methods": sorted(list(getattr(r, "methods", []) or [])),
                "name": getattr(r, "name", None),
            }
            for r in app.routes
        ]
    )


@app.get("/healthz")
def healthz():
    return {"ok": True, "profile": PROFILE, "version": app.version}


@app.get("/version")
def version():
    return {"version": app.version}


@app.get("/")
def index():
    return {
        "service": "Cloudtail API",
        "docs": "/docs",
        "routes": "/__routes",
        "profile": PROFILE,
        "version": app.version,
    }


# --- TEMP demo endpoints (guarded by flag) ---
from datetime import datetime, timezone

ENABLE_TEMP = os.getenv("ENABLE_TEMP_DEMO", "0") == "1"

if ENABLE_TEMP:
    @app.get("/planet/status")
    def _planet_status_tmp():
        return {
            "state_tag": "nostalgia",
            "dominant_emotion": "nostalgia",
            "emotion_history": ["nostalgia","sadness","nostalgia"],
            "color_palette": ["#B9A3D0","#D3C7E6"],
            "visual_theme": "mist",
            "last_updated": datetime.now(timezone.utc).isoformat()
        }

    @app.get("/craft/preview")
    def _craft_preview_tmp():
        return [
            {"status":"planned","item_name":"Rain Echo Chime","element":"CrystalShard",
             "materials_used":["AshDust"],"effect_tags":["symbolic","sadness"]},
            {"status":"planned","item_name":"Mirror of Regret","element":"RustIngot",
             "materials_used":["Tarnish"],"effect_tags":["symbolic","guilt"]},
            {"status":"planned","item_name":"Echo Lantern","element":"EchoBloom",
             "materials_used":["MemoryPetal"],"effect_tags":["symbolic","nostalgia"]},
            {"status":"planned","item_name":"Sun Thread Locket","element":"LightDust",
             "materials_used":["WarmGlow"],"effect_tags":["symbolic","gratitude"]},
        ]
# --- END TEMP ---

# ------------- Mount routers with dual-path fallback -------------
print(">>> before include:", len(app.routes))

# Recommend (Unity): POST /api/recommend
recommend_router = _import_router(
    "cloudtail_backend.routes.recommend_routes",
    "cloudtail_backend.recommend_routes",
)
_include_router_safe = _include_router_safe  # local alias for compact calls
_include_router_safe(recommend_router, "/api", "recommend")

# Planet: GET /planet/status  (real impl)
planet_router = _import_router(
    "cloudtail_backend.routes.planet_routes",
    "cloudtail_backend.planet_routes",
)
_include_router_safe(planet_router, "/planet", "planet")

# Rituals: GET /rituals/recommend  (demo/real per file)
rituals_router = _import_router(
    "cloudtail_backend.routes.ritual_routes",
    "cloudtail_backend.ritual_routes",
)
_include_router_safe(rituals_router, "/rituals", "rituals")

# Crafting: GET /craft/preview  (real impl or demo)
craft_router = _import_router(
    "cloudtail_backend.routes.crafting_routes",
    "cloudtail_backend.crafting_routes",
)
_include_router_safe(craft_router, "/craft", "craft")

# Full profile: Memories / Mongo-backed endpoints
if PROFILE == "full":
    mem_router = _import_router(
        "cloudtail_backend.routes.memory_routes",
        "cloudtail_backend.memory_routes",
    )
    _include_router_safe(mem_router, "/api", "memories")

print(">>> after include:", len(app.routes))
