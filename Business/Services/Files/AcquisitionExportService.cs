using Tomography.Business.Models;
using Tomography.Data;

namespace Tomography.Business.Services;

public static class AcquisitionExportService
{
    public static void ExportCsv(AcquisitionSnapshot snapshot, string directory)
    {
        Directory.CreateDirectory(directory);
        CsvMatrixStorage.Write(Path.Combine(directory, "Vref.csv"), 64, snapshot.ReferenceFrames.Length, snapshot.ReferenceFrames.SelectMany(row => row).ToArray());
        CsvMatrixStorage.Write(Path.Combine(directory, "Vobj.csv"), 64, snapshot.ObjectFrames.Length, snapshot.ObjectFrames.SelectMany(row => row).ToArray());
        CsvMatrixStorage.Write(Path.Combine(directory, "Vref_mean.csv"), 64, snapshot.ReferenceMean is null ? 0 : 1, snapshot.ReferenceMean ?? Array.Empty<double>());
    }
}
