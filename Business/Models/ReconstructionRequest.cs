namespace Tomography.Business.Models;

public sealed record ReconstructionRequest(
    string Algorithm, string SensitivityPath, double[] Measurements, double[]? Reference = null,
    string DifferenceMode = "rel", string? CoordinatesPath = null, string? CustomAlgorithmPath = null,
    double Lambda = 0.1, double Alpha = 0.1, int Iterations = 50, double Lambda0 = 0.06, double TvMu = 0.05);
