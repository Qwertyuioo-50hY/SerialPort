namespace Tomography.Business.Models;

/// <summary>Python 返回的二维矩阵，Values 按行排列，保留实际数值。</summary>
public sealed record ReconstructionImage(int Width, int Height, double[] Values)
{
    public void Validate()
    {
        if (Width is < 1 or > 2048 || Height is < 1 or > 2048 || Values.Length != checked(Width * Height)
            || Values.Any(value => !double.IsFinite(value))) throw new ArgumentException("Python 输出矩阵的尺寸或数值无效。");
    }
}
