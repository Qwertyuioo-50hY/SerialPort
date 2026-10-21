using System.Text.Json;
using Tomography.Business.Models;

namespace Tomography.Business.Services;

public static class GlobalSettingsStore
{
    public static GlobalSettings Load(string path, string? legacyAiPath = null)
    {
        if (File.Exists(path))
        {
            var settings = JsonSerializer.Deserialize<GlobalSettings>(File.ReadAllText(path))
                ?? throw new InvalidDataException("设置文件为空。");
            if (settings.AI is null || settings.Theme is not ("Default" or "Light" or "Dark"))
                throw new InvalidDataException("设置文件格式无效。");
            return settings;
        }
        var initial = new GlobalSettings();
        if (legacyAiPath is not null && File.Exists(legacyAiPath))
            initial.AI = JsonSerializer.Deserialize<AiApiSettings>(File.ReadAllText(legacyAiPath))
                ?? throw new InvalidDataException("原 AI 配置为空。");
        return initial;
    }

    public static void Save(string path, GlobalSettings settings)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        var document = JsonSerializer.SerializeToNode(settings)!.AsObject();
        document.Remove("AI"); // AI settings are stored only in the project config.toml.
        File.WriteAllText(path + ".tmp", document.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
        File.Move(path + ".tmp", path, true);
    }
}
