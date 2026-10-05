using System.Globalization;
using System.Text;

namespace Tomography.Data;

/// <summary>无表头数值 CSV 的矩阵读写。</summary>
public static class CsvMatrixStorage
{
    public static double[][] Read(string path)
    {
        var rows = File.ReadLines(path).Where(line => !string.IsNullOrWhiteSpace(line))
            .Select(line => line.Split(',').Select(value => double.Parse(value.Trim(), CultureInfo.InvariantCulture)).ToArray()).ToArray();
        if (rows.Length == 0 || rows.Any(row => row.Length != rows[0].Length || row.Any(value => !double.IsFinite(value))))
            throw new FormatException("CSV 必须为无表头、等列数的有限数值矩阵。");
        return rows;
    }
    public static void Write(string path, int width, int height, double[] values)
    {
        if (values.Length != checked(width * height)) throw new ArgumentException("矩阵尺寸不匹配。");
        File.WriteAllLines(path, Enumerable.Range(0, height).Select(row =>
            string.Join(",", values.Skip(row * width).Take(width).Select(value => value.ToString("R", CultureInfo.InvariantCulture)))), new UTF8Encoding(false));
    }
}
