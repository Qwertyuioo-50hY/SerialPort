import numpy as np
from scipy.interpolate import griddata


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
    return griddata(xy, values, (xx, yy), method="linear", fill_value=0.)
