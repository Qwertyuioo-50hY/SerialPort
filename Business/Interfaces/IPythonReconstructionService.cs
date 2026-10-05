using Tomography.Business.Models;

namespace Tomography.Business.Interfaces;

public interface IPythonReconstructionService
{
    Task<ReconstructionImage> ReconstructAsync(ReconstructionRequest request, string pythonDll);
    Task ExportAcquisitionMatAsync(AcquisitionSnapshot snapshot, string path, string pythonDll);
    Task CheckSyntaxAsync(string sourcePath, string pythonDll);
}
