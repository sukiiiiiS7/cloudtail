from pathlib import Path
import json
from typing import List, Optional

# Load ritual templates from storage
TEMPLATE_PATH = Path(__file__).resolve().parent.parent / "storage" / "ritual_templates.json"
with open(TEMPLATE_PATH, "r", encoding="utf-8") as f:
    ritual_templates = json.load(f)


def is_user_ready_for_transition(emotion_path: List[str]) -> bool:
    """
    Return True if user shows signs of emotional recovery.
    Heuristic: grief must be < 50% in recent emotional path.
    """
    if not emotion_path:
        return False
    grief_ratio = emotion_path.count("grief") / len(emotion_path)
    return grief_ratio < 0.5


def select_best_template(
    emotion_path: List[str],
    current_state: str,
    ritual_type: Optional[str] = None,
    user_override: bool = False,
    preferred_ritual: Optional[str] = None
) -> Optional[dict]:
    """
    Select the most appropriate ritual template based on emotion path, planet state,
    and optional ritual_type or preferred_ritual. Includes ethics guard for transition.
    """
    # Step 1: Priority match if preferred_ritual is specified
    if preferred_ritual:
        for template in ritual_templates:
            if template.get("ritual_id") == preferred_ritual:
                return template

    # Step 2: Filter templates by ritual_type if provided
    candidates = []
    for template in ritual_templates:
        required_emotions = template.get("emotion_path", [])
        required_state = template.get("required_state", "")
        template_type = template.get("ritual_type", None)

        # Match emotional path & planet state
        if all(e in emotion_path for e in required_emotions) and required_state == current_state:
            if ritual_type and template_type != ritual_type:
                continue
            if required_state == "rebirth" and not is_user_ready_for_transition(emotion_path) and not user_override:
                continue
            candidates.append(template)

    # Step 3: Fallback to general match if no candidates found
    if not candidates and ritual_type:
        for template in ritual_templates:
            required_emotions = template.get("emotion_path", [])
            required_state = template.get("required_state", "")
            if all(e in emotion_path for e in required_emotions) and required_state == current_state:
                if required_state == "rebirth" and not is_user_ready_for_transition(emotion_path) and not user_override:
                    continue
                candidates.append(template)

    return candidates[0] if candidates else None
