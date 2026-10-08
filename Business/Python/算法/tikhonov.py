import numpy as np
from scipy.linalg import solve


def reconstruct(u, sensitivity, xy, parameters):
    """Equivalent dual solve avoids an nPixel x nPixel dense system."""
    regularization = float(parameters.get("Lambda", 0.1))
    if not np.isfinite(regularization) or regularization <= 0:
        raise ValueError("Tikhonov lambda must be positive")
    gram = sensitivity @ sensitivity.T
    return sensitivity.T @ solve(gram + regularization * np.eye(len(u)), u, assume_a="pos")
