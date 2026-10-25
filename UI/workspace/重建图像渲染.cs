using Tomography.Business.Models;

namespace Tomography.UI.workspace;

/// <summary>显示层双线性插值和伪彩色映射；不修改重建数值及导出结果。</summary>
internal static class ReconstructionRasterizer
{
    public static byte[] Render(ReconstructionImage image, bool circular, int size = 512)
    {
        image.Validate();
        if (size < 2 || size > 2048) throw new ArgumentOutOfRangeException(nameof(size));
        var minimum = image.Values.Min(); var range = image.Values.Max() - minimum;
        var pixels = new byte[checked(size * size * 4)];
        for (var y = 0; y < size; y++)
        for (var x = 0; x < size; x++)
        {
            var nx = 2d * x / (size - 1) - 1; var ny = 2d * y / (size - 1) - 1;
            if (circular && nx * nx + ny * ny > 1) continue;
            var sx = (double)x / (size - 1) * (image.Width - 1);
            var sy = (double)y / (size - 1) * (image.Height - 1);
            var x0 = (int)sx; var y0 = (int)sy;
            var x1 = Math.Min(x0 + 1, image.Width - 1); var y1 = Math.Min(y0 + 1, image.Height - 1);
            var tx = sx - x0; var ty = sy - y0;
            var top = image.Values[y0 * image.Width + x0] * (1 - tx) + image.Values[y0 * image.Width + x1] * tx;
            var bottom = image.Values[y1 * image.Width + x0] * (1 - tx) + image.Values[y1 * image.Width + x1] * tx;
            var value = top * (1 - ty) + bottom * ty;
            var t = 1 - (range > 0 ? Math.Clamp((value - minimum) / range, 0, 1) : 0);
            var index = (y * size + x) * 4;
            pixels[index] = Color(1.5 - Math.Abs(4 * t - 1));
            pixels[index + 1] = Color(1.5 - Math.Abs(4 * t - 2));
            pixels[index + 2] = Color(1.5 - Math.Abs(4 * t - 3));
            pixels[index + 3] = 255;
        }
        return pixels;
    }
    private static byte Color(double value) => (byte)Math.Round(Math.Clamp(value, 0, 1) * 255);
}
