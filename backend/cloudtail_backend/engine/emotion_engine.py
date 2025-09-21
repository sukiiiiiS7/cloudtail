import os
os.environ["TRANSFORMERS_NO_TF"] = "1"  # Disable TensorFlow to avoid Keras 3 issues
import json
import logging
from typing import List
from ..models.memory import EmotionEssence
from .emotion_model import EmotionModelWrapper

# Configure logger
logging.basicConfig(level=logging.INFO)
logger = logging.getLogger(__name__)


class EmotionAlchemyEngine:
    """
    Symbolic emotion engine that maps raw model labels
    into Cloudtail's canonical four categories and elements.
    """

    def __init__(self, model_name: str = "bhadresh-savani/distilbert-base-uncased-emotion"):
        self.model = EmotionModelWrapper(model_name)

        # Map raw model labels → canonical four types
        self.label_mapping = {
            # Gratitude-family
            "joy": "gratitude",
            "love": "gratitude",
            "hope": "gratitude",
            "acceptance": "gratitude",
            "peace": "gratitude",

            # Sadness-family
            "sadness": "sadness",
            "grief": "sadness",
            "sorrow": "sadness",

            # Guilt-family
            "guilt": "guilt",
            "regret": "guilt",
            "shame": "guilt",
            "anger": "guilt",
            "fear": "guilt",

            # Nostalgia-family
            "nostalgia": "nostalgia",
            "longing": "nostalgia",
        }

        # Canonical emotion → element mapping
        self.element_table = {
            "sadness": "CrystalShard",
            "guilt": "RustIngot",
            "gratitude": "LightDust",
            "nostalgia": "EchoBloom",
        }

        # Bonus heuristics (demo only, optional expansion)
        self.bonus_rules = {
            "guilt": {"keywords": ["sorry"], "bonus": 0.1},
            "nostalgia": {"keywords": ["sunset", "home", "beach", "smell"], "bonus": 0.1},
        }

    def extract_emotion(self, text: str) -> EmotionEssence:
        """
        Analyze one text and return its structured emotional essence.
        """
        try:
            raw_label, confidence = self.model.predict(text)
        except Exception as e:
            logger.error(f"Emotion model failed on input: {text[:30]}... \n{e}")
            return EmotionEssence(type="error", element="Unknown", effect_tags=[], value=0.0)

        emotion_type = self.map_to_internal_type(raw_label)
        value = self.calculate_value(text, emotion_type, confidence)

        logger.info(f"Extracted [{raw_label}] → [{emotion_type}], confidence={confidence:.3f}")

        return EmotionEssence(
            type=emotion_type,
            element=self.get_element(emotion_type),
            effect_tags=self.tag_emotion(emotion_type),
            value=round(value, 3),
        )

    def extract_batch(self, texts: List[str]) -> List[EmotionEssence]:
        """
        Process a batch of texts into emotional essences.
        """
        logger.info(f"Processing batch of {len(texts)} entries.")
        return [self.extract_emotion(t) for t in texts]

    def map_to_internal_type(self, label: str) -> str:
        if label not in self.label_mapping:
            logger.warning(f"Unmapped label [{label}], defaulting to 'gratitude'")
        return self.label_mapping.get(label, "gratitude")

    def get_element(self, emotion_type: str) -> str:
        return self.element_table.get(emotion_type, "LightDust")

    def tag_emotion(self, emotion_type: str) -> List[str]:
        if emotion_type in ["sadness", "nostalgia"]:
            return ["ritual", "memory"]
        return ["healing", "ambient"]

    def calculate_value(self, text: str, emotion_type: str, confidence: float) -> float:
        """
        Estimate symbolic strength (0.0–1.0) of the detected emotion.
        """
        text = text.lower()
        bonus = 0.0

        if emotion_type in self.bonus_rules:
            rule = self.bonus_rules[emotion_type]
            if any(kw in text for kw in rule["keywords"]):
                bonus += rule["bonus"]

        return min(1.0, 0.2 + confidence + bonus)

    @classmethod
    def from_dict(cls, config: dict):
        """
        Allow future config-driven engine initialization.
        """
        engine = cls(config.get("model_name", "bhadresh-savani/distilbert-base-uncased-emotion"))
        engine.label_mapping = config.get("label_mapping", engine.label_mapping)
        engine.element_table = config.get("element_table", engine.element_table)
        engine.bonus_rules = config.get("bonus_rules", engine.bonus_rules)
        return engine


# Standalone test entry (demo only)
if __name__ == "__main__":
    with open("cloudtail_backend/storage/emotion_engine_config.json", "r") as f:
        config = json.load(f)

    engine = EmotionAlchemyEngine.from_dict(config)

    # Single test
    text1 = "He used to wait for me at the door every day."
    result1 = engine.extract_emotion(text1)
    print("🔹 Single Result:")
    print(json.dumps(result1.__dict__, indent=2))

    # Batch test
    batch = [
        "I still remember the sunset when she left.",
        "I miss the sound of her paws on the floor.",
        "I'm sorry I couldn't do more.",
    ]
    results = engine.extract_batch(batch)
    print("🔹 Batch Results:")
    for i, r in enumerate(results):
        print(f"  {i+1}.", json.dumps(r.__dict__, indent=2))

