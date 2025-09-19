from __future__ import annotations
import os, logging
from typing import Tuple
from transformers import pipeline

logger = logging.getLogger(__name__)
os.environ.setdefault("TRANSFORMERS_NO_TF", "1")  # disable TF globally

DEFAULT_MODEL = "bhadresh-savani/distilbert-base-uncased-emotion"

class EmotionModelWrapper:
    def __init__(self, model_name: str = DEFAULT_MODEL) -> None:
        self.model_name = model_name
        try:
            self._clf = pipeline(
                "text-classification",
                model=self.model_name,
                top_k=1,
                framework="pt",   # force PyTorch
                device=-1         # CPU
            )
            logger.info("Emotion pipeline ready: %s", self.model_name)
        except Exception as e:
            self._clf = None
            logger.exception("Failed to init emotion pipeline: %s", e)

    def predict(self, text: str) -> Tuple[str, float]:
        """
        Returns (label_lowercase, confidence_0_1). Raises RuntimeError if unavailable.
        """
        if self._clf is None:
            raise RuntimeError("classifier_not_available")

        out = self._clf(text)
        # HF returns either [ {label,score} ] or [ [ {label,score} ] ]
        top = out[0][0] if isinstance(out[0], list) else out[0]
        label = str(top["label"]).lower()
        score = float(top["score"])
        return label, score
