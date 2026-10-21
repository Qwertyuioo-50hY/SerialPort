using System.Text.Json;
using Tomlyn;
using Tomlyn.Model;
using Tomography.Business.Models;

namespace Tomography.Business.Services;

public static class AiTomlStore
{
    public static string ConfigPath
    {
        get
        {
            for (var folder = new DirectoryInfo(AppContext.BaseDirectory); folder is not null; folder = folder.Parent)
                if (File.Exists(Path.Combine(folder.FullName, "Tomography.sln"))) return Path.Combine(folder.FullName, "config.toml");
            return Path.Combine(AppContext.BaseDirectory, "config.toml");
        }
    }
    public static (AiApiSettings Settings, string Key) Load(string path)
    {
        var table = Toml.ToModel(File.ReadAllText(path));
        if (!table.TryGetValue("AI", out var value) || value is not TomlTable ai) throw new InvalidDataException("config.toml 缺少 AI 配置。");
        var json = JsonSerializer.Serialize(ai);
        var settings = JsonSerializer.Deserialize<AiApiSettings>(json) ?? throw new InvalidDataException("AI 配置为空。");
        return (settings, ai.TryGetValue("ApiKey", out var key) ? key as string ?? "" : "");
    }
    private static TomlArray ToArray(JsonElement value)
    {
        var result = new TomlArray();
        foreach (var item in value.EnumerateArray()) result.Add(item.GetString()!);
        return result;
    }
    public static void Save(string path, AiApiSettings settings, string key)
    {
        var table = File.Exists(path) ? Toml.ToModel(File.ReadAllText(path)) : new TomlTable();
        var ai = new TomlTable();
        foreach (var property in JsonSerializer.SerializeToElement(settings).EnumerateObject())
            ai[property.Name] = property.Value.ValueKind switch
            {
                JsonValueKind.String => property.Value.GetString()!,
                JsonValueKind.Number => property.Value.GetInt64(),
                JsonValueKind.Array => ToArray(property.Value),
                _ => throw new InvalidDataException("不支持的 AI 配置类型。")
            };
        ai["ApiKey"] = key; table["AI"] = ai;
        var temporary = path + ".tmp";
        try { File.WriteAllText(temporary, Toml.FromModel(table)); File.Move(temporary, path, true); }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
    }
}
