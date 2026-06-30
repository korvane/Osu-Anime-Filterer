import keras
import numpy as np
import pickle
from pathlib import Path
import sys
import json

# predict batch
def is_filtered(image_paths):
    full_paths = [Path("imgs") / p for p in image_paths]

    images = []
    valid_paths = []

    for path in full_paths:
        try:
            img = keras.utils.load_img(path, target_size=IMG_SIZE)
            img = keras.utils.img_to_array(img)
            img = keras.applications.mobilenet_v2.preprocess_input(img)

            images.append(img)
            valid_paths.append(str(path))

        except Exception:
            continue

    if len(images) == 0:
        return []

    images = np.stack(images)

    # Extract MobileNet features
    feats = base_model.predict(images, verbose=0)

    # Probability that each image is anime
    probabilities = classifier.predict_proba(feats)[:, 1]
    predictions = probabilities > 0.5

    results = []

    for path, pred, prob in zip(valid_paths, predictions, probabilities):
        results.append({"Path": path, "Prediction": bool(pred), "Probability": float(prob)})

    return results


IMG_SIZE = (160, 160)
BASE_DIR = Path(__file__).resolve().parent

base_model = keras.models.load_model(BASE_DIR / "ML" / "feature_extractor.keras")
with open(BASE_DIR / "ML" / "anime_classifier.pkl", "rb") as f:
    classifier = pickle.load(f)

image_paths = json.loads(sys.stdin.readline())
image_paths = image_paths["images"]
result = is_filtered(image_paths)
print(json.dumps(result))