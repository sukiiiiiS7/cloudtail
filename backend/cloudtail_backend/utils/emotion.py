# Returns the final emotion to use (manual override has priority)
def get_final_emotion(entry: dict) -> str:
    return entry.get("manual_override") or entry.get("detected_emotion", "unknown")
