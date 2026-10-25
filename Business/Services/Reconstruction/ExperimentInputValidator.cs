using Python.Runtime;
using Tomography.Data;

namespace Tomography.Business.Services;

public static class ExperimentInputValidator
{
    public static (int Rows, int Columns) ValidateSensitivity(string path, string pythonDll)
    {
        if (!Path.GetExtension(path).Equals(".mat", StringComparison.OrdinalIgnoreCase))
        {
            var matrix = CsvMatrixStorage.Read(path);
            if (matrix.Length != 64 && matrix[0].Length != 64)
                throw new FormatException("灵敏度矩阵必须为 64×N 或 N×64；64 是测量通道数。");
            return (matrix.Length, matrix[0].Length);
        }
        PythonRuntimeHost.Initialize(pythonDll);
        using var gil = Py.GIL(); using var scope = Py.CreateScope();
        scope.Set("path", path);
        // MAT 灵敏度可为复数；保留原始文件，重建时沿用 sensitivity_io 的实部处理。
        scope.Exec("import numpy as np\nfrom scipy.io import loadmat\ndata = loadmat(path)\nif 'sen' not in data or 'xy' not in data: raise ValueError('MAT requires sen and xy')\nsen, xy = np.asarray(data['sen']), np.asarray(data['xy'])\nif np.iscomplexobj(xy): raise ValueError('xy must contain real coordinates')\nif xy.ndim != 2 or xy.shape[1] != 2 or len(xy) == 0 or not np.isfinite(xy).all(): raise ValueError('xy must be finite N x 2')\nif sen.shape not in [(64, len(xy)), (len(xy), 64)] or not np.isfinite(sen).all(): raise ValueError('sen must be finite 64 x N or N x 64')\nrows, columns = int(sen.shape[0]), int(sen.shape[1])");
        using var rows = scope.Get("rows"); using var columns = scope.Get("columns");
        return (rows.As<int>(), columns.As<int>());
    }
    public static double[] ReadChannels(string path)
    {
        var values = CsvMatrixStorage.Read(path).SelectMany(row => row).ToArray();
        if (values.Length != 64) throw new FormatException("输入必须包含 64 个有限通道值。");
        return values;
    }
    public static int ValidateCoordinates(string path, string? sensitivity)
    {
        var xy = CsvMatrixStorage.Read(path);
        if (xy[0].Length != 2) throw new FormatException("节点坐标必须为 N×2 的有限数值矩阵。");
        if (sensitivity is not null && !Path.GetExtension(sensitivity).Equals(".mat", StringComparison.OrdinalIgnoreCase))
        {
            var matrix = CsvMatrixStorage.Read(sensitivity);
            if (!((matrix.Length == 64 && matrix[0].Length == xy.Length) || (matrix[0].Length == 64 && matrix.Length == xy.Length)))
                throw new FormatException("节点坐标数量与灵敏度矩阵节点维度不匹配。");
        }
        return xy.Length;
    }
}
