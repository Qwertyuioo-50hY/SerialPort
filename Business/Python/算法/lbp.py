def reconstruct(u, sensitivity, xy, parameters):
    """Linear back projection: x = S.T @ U."""
    return sensitivity.T @ u
