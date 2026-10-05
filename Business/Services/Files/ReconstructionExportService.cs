using Tomography.Business.Models;
using Tomography.Data;

namespace Tomography.Business.Services;

public static class ReconstructionExportService
{
    public static void ExportCsv(ReconstructionImage image, string path)
    {
        image.Validate();
        CsvMatrixStorage.Write(path, image.Width, image.Height, image.Values);
    }
    public static void ExportBitmap(ReconstructionImage image, string path) =>
        BmpImageStorage.Write(path, image.Width, image.Height, MatrixPixelConverter.ToBgra(image));
}
