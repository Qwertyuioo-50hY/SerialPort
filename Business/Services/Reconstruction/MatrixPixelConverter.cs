using Tomography.Business.Models;

namespace Tomography.Business.Services;

/// <summary>将二维数值矩阵映射为灰度 BGRA 像素，不修改原数值。</summary>
public static class MatrixPixelConverter
{
    public static byte[] ToBgra(ReconstructionImage image)
    {
        image.Validate();
        var minimum = image.Values.Min();
        var range = image.Values.Max() - minimum;
        var pixels = new byte[checked(image.Values.Length * 4)];
        for (var i = 0; i < image.Values.Length; i++)
        {
            var value = range > 0 ? (byte)Math.Round((image.Values[i] - minimum) / range * 255) : (byte)0;
            pixels[i * 4] = pixels[i * 4 + 1] = pixels[i * 4 + 2] = value;
            pixels[i * 4 + 3] = 255;
        }
        return pixels;
    }
}
