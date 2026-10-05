namespace Tomography.Data;

/// <summary>将 BGRA 像素写入标准 32 位 BMP，无额外图像库。</summary>
public static class BmpImageStorage
{
    public static void Write(string path, int width, int height, byte[] pixels)
    {
        if (pixels.Length != checked(width * height * 4)) throw new ArgumentException("像素尺寸不匹配。");
        using var writer = new BinaryWriter(File.Create(path));
        writer.Write((ushort)0x4D42); writer.Write(54 + pixels.Length);
        writer.Write(0); writer.Write(54); writer.Write(40);
        writer.Write(width); writer.Write(-height); // Top-down rows.
        writer.Write((ushort)1); writer.Write((ushort)32); writer.Write(0);
        writer.Write(pixels.Length); writer.Write(2835); writer.Write(2835); writer.Write(0); writer.Write(0);
        writer.Write(pixels);
    }
}
