from pathlib import Path
import numpy as np
from scipy.io import loadmat


def load(path, coordinates_path=None):
    if Path(path).suffix.lower() == ".mat":
        data = loadmat(path)
        if "sen" not in data or "xy" not in data:
            raise ValueError("MAT must contain sen and xy, as in senxy.mat")
        sensitivity, xy = data["sen"], data["xy"]
    else:
        if not coordinates_path:
            raise ValueError("CSV sensitivity also requires an xy coordinate CSV")
        sensitivity = np.loadtxt(path, delimiter=",", ndmin=2)
        xy = np.loadtxt(coordinates_path, delimiter=",", ndmin=2)
    sensitivity = np.asarray(np.real(sensitivity), dtype=float)
    xy = np.asarray(xy, dtype=float)
    if xy.ndim != 2 or xy.shape[1] != 2 or not np.isfinite(xy).all() or not np.isfinite(sensitivity).all():
        raise ValueError("Sensitivity and xy must be finite; xy must be N x 2")
    if sensitivity.shape == (len(xy), 64):
        sensitivity = sensitivity.T
    if sensitivity.shape != (64, len(xy)):
        raise ValueError("Sensitivity must be 64 x N or N x 64 with N coordinate nodes")
    return sensitivity, xy
