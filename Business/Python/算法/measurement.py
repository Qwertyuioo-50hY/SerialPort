import numpy as np


def vector(measurements, reference=None, mode="rel"):
    obj = np.asarray(measurements, dtype=float).reshape(-1)
    if obj.size != 64 or not np.isfinite(obj).all():
        raise ValueError("Measurements must contain 64 finite channel values")
    if reference is None:
        raise ValueError("Import or capture an empty-field reference before reconstruction")
    ref = np.asarray(reference, dtype=float).reshape(-1)
    if ref.size != obj.size or not np.isfinite(ref).all():
        raise ValueError("Reference must contain 64 finite channel values")
    difference = obj - ref
    if mode == "abs":
        return difference
    if mode != "rel":
        raise ValueError("Difference mode must be abs or rel")
    tiny = np.max(np.abs(ref)) * 1e-12 or 1.
    denominator = np.where(np.abs(ref) < tiny, np.where(ref < 0, -tiny, tiny), ref)
    return difference / denominator
