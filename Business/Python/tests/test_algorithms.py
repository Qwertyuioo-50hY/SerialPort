import unittest
import sys
from pathlib import Path
import numpy as np
sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
from algorithms import lbp, tikhonov, landweber, tv
from measurement import vector
from sensitivity_io import load


class AlgorithmTests(unittest.TestCase):
    def setUp(self):
        self.xy = np.array([[0., 0.], [1., 0.], [0., 1.], [1., 1.]])
        self.s = np.eye(4)
        self.u = np.array([0., 1., 1., 0.])

    def test_lbp(self):
        np.testing.assert_allclose(lbp.reconstruct(self.u, self.s, self.xy, {}), self.u)

    def test_tikhonov_matches_primal_system(self):
        rng = np.random.default_rng(42)
        s = rng.normal(size=(3, 4))
        u = rng.normal(size=3)
        expected = np.linalg.solve(s.T @ s + 0.1 * np.eye(4), s.T @ u)
        np.testing.assert_allclose(tikhonov.reconstruct(u, s, self.xy, {}), expected, atol=1e-12)

    def test_landweber_step_reduces_residual(self):
        x = landweber.reconstruct(self.u, self.s * 100, self.xy, {"Alpha": 10., "Iterations": 50})
        self.assertLess(np.linalg.norm(self.s * 100 @ x - self.u), 1e-6)

    def test_tv_constant_has_zero_gradient(self):
        x = tv.reconstruct(np.ones(4), self.s, self.xy, {"TvMu": 0.05, "Iterations": 50})
        np.testing.assert_allclose(x, np.ones(4), atol=1e-8)

    def test_tv_suppresses_node_differences(self):
        x = tv.reconstruct(self.u, self.s, self.xy, {"TvMu": 0.1, "Iterations": 200})
        self.assertLess(np.ptp(x), np.ptp(self.u))
        self.assertTrue(np.isfinite(x).all())

    def test_relative_and_absolute_difference(self):
        ref = np.ones(64) * 100
        obj = ref + 1
        np.testing.assert_allclose(vector(obj, ref, "abs"), 1)
        np.testing.assert_allclose(vector(obj, ref, "rel"), 0.01)

    def test_zero_reference_does_not_produce_nan(self):
        self.assertTrue(np.isfinite(vector(np.ones(64), np.zeros(64))).all())

    def test_reference_required(self):
        with self.assertRaises(ValueError): vector(np.ones(64))

    def test_invalid_regularization(self):
        with self.assertRaises(ValueError): tikhonov.reconstruct(self.u, self.s, self.xy, {"Lambda": 0})


if __name__ == "__main__":
    unittest.main()
