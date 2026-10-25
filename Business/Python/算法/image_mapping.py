import numpy as np
from scipy.interpolate import griddata
from scipy.ndimage import gaussian_filter


def to_image(values, xy):
    values = np.asarray(values, dtype=float)
    if values.ndim == 2:
        if not np.isfinite(values).all():
            raise ValueError("Custom output image must be finite")
        return values
    values = values.reshape(-1)
    if len(values) != len(xy) or not np.isfinite(values).all():
        raise ValueError("Algorithm must return N node values or a finite 2D image")
    values = values - values.min()
    scale = values.max()
    if scale > 0:
        values /= scale
    axis = np.linspace(-50, 50, 257)
    xx, yy = np.meshgrid(axis, axis)
    # Extend the measured boundary for display instead of adding an artificial zero ring.
    # Acquisition values and the algorithm's node values remain unchanged.
    image = griddata(xy, values, (xx, yy), method="cubic", fill_value=np.nan)
    outside = ~np.isfinite(image)
    if outside.any():
        image[outside] = griddata(xy, values, (xx[outside], yy[outside]), method="nearest")
    return np.clip(gaussian_filter(image, sigma=1.5, mode="nearest"), 0., 1.)
