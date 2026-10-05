import json
import numpy as np
from scipy.io import savemat


def save(path, snapshot_json):
    data = json.loads(snapshot_json)
    savemat(path, {
        "Vref": np.asarray(data["ReferenceFrames"], dtype=float).reshape(-1, 64),
        "Vobj": np.asarray(data["ObjectFrames"], dtype=float).reshape(-1, 64),
        "Vref_mean": np.asarray(data["ReferenceMean"] or [], dtype=float).reshape(1, -1),
    }, appendmat=False)
