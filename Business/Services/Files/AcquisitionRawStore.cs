using System.Globalization;
using System.Text;

namespace Tomography.Business.Services;

/// <summary>保存原始微伏数值：固定文件保存最新值，时间戳文件保留每次采集结果。</summary>
public static class AcquisitionRawStore
{
    public static void Save(string projectDirectory, bool reference, double[] values)
    {
        if (values.Length != 64 || values.Any(value => !double.IsFinite(value)))
            throw new ArgumentException("保存数据必须包含 64 个有限通道值。");
        var directory = Path.Combine(Path.GetFullPath(projectDirectory), ProjectStore.DataFolderName);
        Directory.CreateDirectory(directory);
        var name = reference ? "vvRefRaw" : "vvAvgRaw";
        var stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss_fffffff", CultureInfo.InvariantCulture);
        var text = string.Join(Environment.NewLine, values.Select(value => value.ToString("G17", CultureInfo.InvariantCulture))) + Environment.NewLine;
        var archive = Path.Combine(directory, $"{name}_{stamp}.txt");
        for (var suffix = 1; File.Exists(archive); suffix++) archive = Path.Combine(directory, $"{name}_{stamp}_{suffix}.txt");
        using (var stream = new FileStream(archive, FileMode.CreateNew, FileAccess.Write, FileShare.Read))
        using (var writer = new StreamWriter(stream, new UTF8Encoding(false))) writer.Write(text);
        var target = Path.Combine(directory, name + ".txt");
        var temporary = target + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try { File.WriteAllText(temporary, text, new UTF8Encoding(false)); File.Move(temporary, target, true); }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
    }
}
