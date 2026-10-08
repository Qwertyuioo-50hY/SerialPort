import numpy as np
from scipy.spatial import Delaunay
from scipy.sparse import coo_matrix
from scipy.linalg import cho_factor, cho_solve


def reconstruct(u, sensitivity, xy, parameters):
    """Graph TV via ADMM, following emt_tv_recon.m."""
    mu = float(parameters.get("TvMu", 0.05))
    iterations = int(parameters.get("Iterations", 50))
    if not np.isfinite(mu) or mu <= 0 or iterations < 1:
        raise ValueError("TV weight and iterations must be positive")
    triangles = Delaunay(xy).simplices
    edges = np.unique(np.sort(np.vstack((triangles[:, [0, 1]], triangles[:, [1, 2]], triangles[:, [2, 0]])), axis=1), axis=0)
    n_edges, n_pixels = len(edges), sensitivity.shape[1]
    rows = np.repeat(np.arange(n_edges), 2)
    gradient = coo_matrix((np.tile([1., -1.], n_edges), (rows, edges.ravel())), shape=(n_edges, n_pixels)).tocsr()
    rho = max(mu * 10, 0.1)
    system = sensitivity.T @ sensitivity + rho * (gradient.T @ gradient).toarray()
    # Small ridge makes the solve well-defined for rank-deficient calibrations.
    ridge = max(np.max(np.diag(system)), 1.) * 1e-12
    factor = cho_factor(system + ridge * np.eye(n_pixels), lower=True)
    rhs = sensitivity.T @ u
    x = cho_solve(factor, rhs)
    z = np.zeros(n_edges)
    dual = np.zeros(n_edges)
    for _ in range(iterations):
        x = cho_solve(factor, rhs + rho * gradient.T @ (z - dual))
        gx = gradient @ x
        value = gx + dual
        z = np.sign(value) * np.maximum(np.abs(value) - mu / rho, 0.)
        dual += gx - z
    return x
