from typing import List, Optional
import json
from pathlib import Path
from cloudtail_backend.models.crafting import CraftResponse


# Load config data
BASE_DIR = Path(__file__).resolve().parent.parent
CONFIG_PATH = BASE_DIR / "storage" / "cloudtail_config.json"
with open(CONFIG_PATH, "r", encoding="utf-8") as f:
    config_data = json.load(f)

emotion_map = config_data.get("emotion_map", {})
crafted_items = config_data.get("crafted_items", {})


def get_craftable_items(emotion_type: str) -> List[str]:
    """
    Return a list of items that can be crafted from a given emotion type.
    """
    if emotion_type not in emotion_map:
        return []

    element = emotion_map[emotion_type]["element"]
    materials = set(emotion_map[emotion_type]["materials"])
    craftables = []

    for item, recipe in crafted_items.items():
        if element in recipe or materials.intersection(recipe):
            craftables.append(item)

    return craftables


def get_item_recipe(item_name: str) -> Optional[List[str]]:
    """
    Return the ingredient list for a given item, if it exists.
    """
    return crafted_items.get(item_name)


def craft_item_from_emotion(emotion_type: str) -> Optional[dict]:
    """
    Try to craft an item based on user's current emotional type.
    Returns first matching crafted item.
    """
    available_items = get_craftable_items(emotion_type)

    if not available_items:
        return None

    item_name = available_items[0]
    recipe = get_item_recipe(item_name)

    return {
        "item_name": item_name,
        "element": emotion_map[emotion_type]["element"],
        "materials_used": recipe,
        "effect_tags": ["symbolic", "ritual", emotion_type],
        "description": f"A symbolic item crafted from {emotion_type}."
    }
# -------------------------------
# Preview all craftable items
# -------------------------------

def preview_all_craftables() -> list[CraftResponse]:
    """
    Return a list of all possible craftable items,
    one per emotion type defined in config.
    """
    results = []
    for emotion, config in config_data["emotion_map"].items():
        result = craft_item_from_emotion(emotion)
        if result:
            results.append(result)
    return results
