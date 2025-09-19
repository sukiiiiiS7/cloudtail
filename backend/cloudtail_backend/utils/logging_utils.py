from pathlib import Path
from datetime import datetime
def log_ritual_action(
    ritual_id: str,
    emotion_path: list[str],
    planet_state: str,
    user_override: bool,
    base_dir: Path,
    filename: str = "ritual_log.txt"
) -> None:
    """
    Log a ritual perform event to a specified log file.

    Args:
        ritual_id (str): Unique ritual identifier or name
        emotion_path (list[str]): Sequence of recent emotions
        planet_state (str): Current planet state tag (e.g. 'grief', 'hope')
        user_override (bool): Whether user manually overrode emotion
        base_dir (Path): Project base path
        filename (str): Log file name (default: ritual_log.txt)
    """
    try:
        timestamp = datetime.utcnow().isoformat()
        emotion_seq = " → ".join(emotion_path)
        override_flag = "YES" if user_override else "NO"

        log_entry = (
            f"[{timestamp}] Ritual: {ritual_id} | State: {planet_state} | "
            f"Path: [{emotion_seq}] | Override: {override_flag}\n"
        )

        log_path = base_dir / "storage" / filename
        log_path.parent.mkdir(parents=True, exist_ok=True)

        with open(log_path, "a", encoding="utf-8") as f:
            f.write(log_entry)
    except Exception as e:
        print(f"[LogRitual] Failed to log ritual: {e}")

def log_emotion_to_file(
    emotion_type: str,
    element: str,
    value: float,
    base_dir: Path,
    filename: str = "memory_log.txt"
) -> None:
    """
    Log an emotion extraction result to a specified log file.

    Args:
        emotion_type (str): Symbolic emotion type (e.g. 'grief', 'nostalgia')
        element (str): Associated material/element (e.g. 'CrystalShard')
        value (float): Symbolic intensity value (0.0–1.0)
        base_dir (Path): Project base path for locating /storage/
        filename (str): Name of the log file (default: memory_log.txt)
    """
    try:
        log_entry = f"[{datetime.utcnow().isoformat()}] {emotion_type} → {element} (value: {value:.3f})\n"
        log_path = base_dir / "storage" / filename

        # Ensure parent directory exists
        log_path.parent.mkdir(parents=True, exist_ok=True)

        with open(log_path, "a", encoding="utf-8") as f:
            f.write(log_entry)
    except Exception as e:
        print(f"[LogEmotion] Failed to log emotion: {e}")
