import numpy as np
from .tikhonov import reconstruct as tikhonov


def reconstruct(u, sensitivity, xy, parameters):
    """Tikhonov warm start followed by stable Landweber iterations."""
    alpha = float(parameters.get("Alpha", 0.1))
    iterations = int(parameters.get("Iterations", 50))
    norm_squared = np.linalg.norm(sensitivity, 2) ** 2
    if not np.isfinite(alpha) or alpha <= 0 or iterations < 1:
        raise ValueError("Landweber step and iterations must be positive")
    if norm_squared == 0:
        return np.zeros(sensitivity.shape[1])
    # Bound the supplied step so 0 < alpha < 2/||S||^2.
    step = min(alpha, 1.9 / norm_squared)
    x = tikhonov(u, sensitivity, xy, {"Lambda": parameters.get("Lambda0", 0.06)})
    for _ in range(iterations):
        x += step * sensitivity.T @ (u - sensitivity @ x)
    return x
