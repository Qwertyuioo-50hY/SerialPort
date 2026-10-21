using System.Globalization;

namespace Tomography.Business.Services;

/// <summary>校验采样参数并生成设备采样指令。</summary>
public static class EmtSampleCommand
{
    public static string Build(int frequencyKhz, int coilCount)
    {
        if (frequencyKhz is < 1 or > 159) throw new ArgumentOutOfRangeException(nameof(frequencyKhz), "频率范围为 1–159 kHz。");
        if (coilCount is < 1 or > 15) throw new ArgumentOutOfRangeException(nameof(coilCount), "线圈数量范围为 1–15。");
        return frequencyKhz.ToString("D3", CultureInfo.InvariantCulture)
            + coilCount.ToString("D2", CultureInfo.InvariantCulture) + "Sample";
    }
}
