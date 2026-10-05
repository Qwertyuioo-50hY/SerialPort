namespace Tomography.Business.Models;

public sealed record AcquisitionSnapshot(double[][] ReferenceFrames, double[][] ObjectFrames, double[]? ReferenceMean);
