using System.Globalization;

namespace Tomography.Business.Services;

/// <summary>校验采样参数并生成设备采样指令。</summary>
public static class EmtSampleCommand
{
    public static string Build(int frequencyKhz, int coilCount, string arrayType = "R", string excitationMode = "V")
    {
        if (frequencyKhz is < 1 or > 159) throw new ArgumentOutOfRangeException(nameof(frequencyKhz), "频率范围为 1–159 kHz。");
        if (coilCount is < 1 or > 15) throw new ArgumentOutOfRangeException(nameof(coilCount), "线圈数量范围为 1–15。");
        if (arrayType is not ("R" or "S")) throw new ArgumentException("阵列类型必须为 R（环形）或 S（方形）。", nameof(arrayType));
        if (excitationMode is not ("V" or "I")) throw new ArgumentException("激励方式必须为 V（恒压）或 I（恒流）。", nameof(excitationMode));
        return frequencyKhz.ToString("D3", CultureInfo.InvariantCulture)
            + coilCount.ToString("D2", CultureInfo.InvariantCulture) + arrayType + excitationMode + "Sample";
    }
}
