from __future__ import annotations
import os
from fastapi import FastAPI
from fastapi.middleware.cors import CORSMiddleware

# --- App profile: "presentation" | "full" (default: presentation) ---
PROFILE = os.getenv("CLOUDTAIL_PROFILE", "presentation").lower()

app = FastAPI(
    title="Cloudtail API",
    description="Memory → Emotion → Planet",
    version="1.0.0-four-planets",
)

# --- CORS: wide-open for demo; tighten ALLOWED_ORIGINS in deployment ---
app.add_middleware(
    CORSMiddleware,
    allow_origins=os.getenv("ALLOWED_ORIGINS", "*").split(","),
    allow_credentials=True,
    allow_methods=["*"],
    allow_headers=["*"],
)

# --- Health & version ---
@app.get("/healthz")
def healthz():
    return {"ok": True, "profile": PROFILE, "version": app.version}

@app.get("/version")
def version():
    return {"version": app.version}

# --- Always-on (works in both presentation & full profiles) ---
# 1) Recommendation adapter (no DB dependency)
from .routes.recommend_routes import router as recommend_router  # noqa: E402
app.include_router(recommend_router, prefix="/api")

# 2) Planet status (Unity relies on this; should not require DB)
from .routes.planet_routes import router as planet_router        # noqa: E402
app.include_router(planet_router, prefix="/planet")

# 3) Poster-aligned stubs (no external templates/DB)
from .routes.ritual_routes import router as ritual_router        # noqa: E402
from .routes.crafting_routes import router as craft_router       # noqa: E402
app.include_router(ritual_router,  prefix="/rituals")
app.include_router(craft_router,    prefix="/craft")

# --- Full profile only: endpoints that require DB (e.g., memories) ---
if PROFILE == "full":
    try:
        from .routes.memory_routes import router as memory_router  # noqa: E402
        app.include_router(memory_router, prefix="/api")
    except Exception as e:
        @app.get("/_full_profile_error")
        def full_profile_error():
            return {
                "error": str(e),
                "hint": "Check Mongo driver, connection string, or circular imports."
            }
