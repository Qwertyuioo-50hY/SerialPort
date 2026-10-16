using Tomography.Business.Models;

namespace Tomography.Business.Services;

public static class ProjectStore
{
    public const string ConfigFileName = "project.json";
    public const string DataFolderName = "data";

    public static string Create(string parent, string name)
    {
        name = name.Trim();
        var stem = name.Split('.')[0];
        if (name.Length == 0 || name is "." or ".." || name.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 ||
            name.EndsWith('.') || stem.Equals("CON", StringComparison.OrdinalIgnoreCase) ||
            stem.Equals("PRN", StringComparison.OrdinalIgnoreCase) || stem.Equals("AUX", StringComparison.OrdinalIgnoreCase) ||
            stem.Equals("NUL", StringComparison.OrdinalIgnoreCase) ||
            (stem.Length == 4 && (stem.StartsWith("COM", StringComparison.OrdinalIgnoreCase) || stem.StartsWith("LPT", StringComparison.OrdinalIgnoreCase)) && stem[3] is >= '1' and <= '9'))
            throw new ArgumentException("项目名称无效。");
        if (!Directory.Exists(parent)) throw new DirectoryNotFoundException("保存位置不存在。");
        var path = Path.Combine(Path.GetFullPath(parent), name);
        if (Directory.Exists(path) || File.Exists(path)) throw new IOException("同名文件或文件夹已存在，请使用其他名称。");
        Directory.CreateDirectory(path);
        var result = new ConfigService().Save(new AppSettings(), Path.Combine(path, ConfigFileName));
        if (!result.IsSuccess) throw new IOException(result.Message, result.Exception);
        Directory.CreateDirectory(Path.Combine(path, DataFolderName));
        Directory.CreateDirectory(Path.Combine(path, AiProjectStore.FolderName));
        return path;
    }
}
