from __future__ import annotations

from pathlib import Path
from datetime import datetime
from typing import Iterable, Optional

# Canonical four emotions used across the demo build
CANONICAL = {"sadness", "guilt", "nostalgia", "gratitude"}


def _ts() -> str:
    """UTC timestamp for compact logs."""
    return datetime.utcnow().strftime("%Y-%m-%dT%H:%M:%SZ")


def ensure_dir(path: Path) -> None:
    """Create parent directory if it does not exist."""
    path.parent.mkdir(parents=True, exist_ok=True)


def log_emotion_to_file(
    text: str,
    label: str,
    score: float,
    path: Path | str,
    element: Optional[str] = None,
) -> None:
    """
    Append an emotion extraction record for demo playback and traceability.

    Args:
        text: Source text (will be truncated for one-line logs).
        label: Canonical emotion label ('sadness' | 'guilt' | 'nostalgia' | 'gratitude').
        score: Confidence or symbolic value (0.0–1.0).
        path: Target log file path.
        element: Optional symbolic element (e.g., 'CrystalShard').

    Behavior:
        - Non-canonical labels are normalized to 'gratitude' to keep the log contract simple.
        - Newlines in text are collapsed into spaces to keep one record per line.
    """
    lbl = label if label in CANONICAL else "gratitude"
    msg = text.replace("\n", " ").strip()
    if len(msg) > 200:
        msg = msg[:200] + "…"

    p = Path(path)
    ensure_dir(p)
    line = f"{_ts()}\t{lbl}\t{score:.3f}\t{element or '-'}\t{msg}\n"
    with p.open("a", encoding="utf-8") as f:
        f.write(line)


def log_planet_status(
    planet_key: str,
    mood: str,
    color_hex: str,
    weather: str,
    path: Path | str,
) -> None:
    """
    Log a snapshot of the planet status.

    Args:
        planet_key: Planet identifier ('ambered' | 'rippled' | 'spiral' | 'woven').
        mood: Canonical emotion ('sadness' | 'guilt' | 'nostalgia' | 'gratitude').
        color_hex: Hex color string, e.g. '#F4C96A'.
        weather: Short descriptor, e.g. 'clear', 'rain', 'mist'.
        path: Target log file path.
    """
    mood = mood if mood in CANONICAL else "gratitude"
    p = Path(path)
    ensure_dir(p)
    line = f"{_ts()}\t{planet_key}\t{mood}\t{color_hex}\t{weather}\n"
    with p.open("a", encoding="utf-8") as f:
        f.write(line)


def log_ritual_action(
    ritual_id: str,
    emotion_path: Iterable[str],
    planet_state: str,
    user_override: bool,
    path: Path | str,
    filename: str = "ritual_log.txt",
) -> None:
    """
    Log a ritual-related event (used by stub endpoints or future implementation).

    Args:
        ritual_id: Unique ritual identifier, e.g. 'ashes_to_light'.
        emotion_path: Sequence of canonical emotions (e.g. ['sadness', 'gratitude']).
        planet_state: Planet key ('ambered' | 'rippled' | 'spiral' | 'woven').
        user_override: Whether the user manually overrode an emotion/state.
        path: Base directory to store the log file.
        filename: Log file name (default: 'ritual_log.txt').

    Notes:
        - This utility does not enforce persistence. It simply appends a textual record.
        - Safe to call from stub routes; it does not assume any DB connection.
    """
    # Normalize emotion path to canonical labels
    normalized = [e if e in CANONICAL else "gratitude" for e in emotion_path]
    p = Path(path) / filename
    ensure_dir(p)
    line = f"{_ts()}\t{ritual_id}\t{','.join(normalized)}\t{planet_state}\toverride={user_override}\n"
    with p.open("a", encoding="utf-8") as f:
        f.write(line)