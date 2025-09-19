from __future__ import annotations

import os
from fastapi import FastAPI
from fastapi.middleware.cors import CORSMiddleware

# --- App profile: "presentation" | "full" (default: presentation) ---
PROFILE = os.getenv("CLOUDTAIL_PROFILE", "presentation").lower()

app = FastAPI(
    title="Cloudtail API",
    description="Memory → Emotion → Planet",
    version="1.0.0",
)

# --- CORS: keep wide-open for dev; tighten ALLOWED_ORIGINS in deployment ---
app.add_middleware(
    CORSMiddleware,
    allow_origins=os.getenv("ALLOWED_ORIGINS", "*").split(","),
    allow_credentials=True,
    allow_methods=["*"],
    allow_headers=["*"],
)

# --- Health check ---
@app.get("/healthz")
def healthz():
    return {"ok": True, "profile": PROFILE, "version": app.version}

# --- Always-on: recommendation adapter (no DB dependency) ---
# use relative import so editors (Pylance) and runtime agree on package resolution
from .routes.recommend_routes import router as recommend_router  # noqa: E402
app.include_router(recommend_router, prefix="/api")

# --- Conditionally enable full stack (DB / ritual / planet / crafting) ---
if PROFILE == "full":
    # Import inside the block to avoid import-time failures in presentation mode
    try:
        from .routes.memory_routes import router as memory_router  # noqa: E402
        from .routes import planet_routes  # noqa: E402
        from .routes import ritual_routes  # noqa: E402
        from .routes import crafting_routes  # noqa: E402

        app.include_router(memory_router, prefix="/api")
        app.include_router(planet_routes.router, prefix="/api")
        app.include_router(ritual_routes.router, prefix="/api")
        app.include_router(crafting_routes.router, prefix="/api")
    except Exception as e:
        # Keep adapter working even if full stack fails to import
        @app.get("/_full_profile_error")
        def full_profile_error():
            return {
                "error": str(e),
                "hint": "Check Mongo driver, ritual models, or circular imports."
            }
