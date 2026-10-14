using System.Text.Json;
using Tomography.Business.Models;

namespace Tomography.Business.Services;

public static class AiProjectStore
{
    public const string FolderName = "聊天";
    public static List<AiConversation> Load(string project, string? legacyPath = null)
    {
        var root = Path.Combine(project, FolderName);
        var items = new List<AiConversation>();
        if (Directory.Exists(root))
            foreach (var folder in Directory.EnumerateDirectories(root))
            {
                if (!Guid.TryParseExact(Path.GetFileName(folder), "N", out _)) continue;
                var path = Path.Combine(folder, "conversation.json");
                if (!File.Exists(path)) continue;
                var item = JsonSerializer.Deserialize<AiConversation>(File.ReadAllText(path)) ?? throw new InvalidDataException("对话文件为空。");
                Validate(item);
                if (item.Id != Path.GetFileName(folder)) throw new InvalidDataException("对话 ID 与目录不一致。");
                items.Add(item);
            }
        if (legacyPath is not null && File.Exists(legacyPath))
        {
            var legacy = JsonSerializer.Deserialize<List<AiConversation>>(File.ReadAllText(legacyPath)) ?? new();
            foreach (var item in legacy)
            {
                Validate(item);
                if (items.Any(existing => existing.Id == item.Id)) continue;
                Save(project, new[] { item }); items.Add(item);
            }
        }
        return items.OrderByDescending(item => File.GetLastWriteTimeUtc(Path.Combine(root, item.Id, "conversation.json"))).ToList();
    }
    public static void Save(string project, IEnumerable<AiConversation> items)
    {
        foreach (var item in items)
        {
            Validate(item);
            var folder = Path.Combine(project, FolderName, item.Id);
            Directory.CreateDirectory(folder);
            var path = Path.Combine(folder, "conversation.json");
            var text = JsonSerializer.Serialize(item, new JsonSerializerOptions { WriteIndented = true });
            if (File.Exists(path) && File.ReadAllText(path) == text) continue;
            File.WriteAllText(path + ".tmp", text);
            File.Move(path + ".tmp", path, true);
        }
    }
    private static void Validate(AiConversation item)
    {
        if (item is null || !Guid.TryParseExact(item.Id, "N", out _) || item.Title is null || item.Messages is null ||
            item.Messages.Any(message => message is null || message.Role is not ("user" or "assistant") || message.Content is null))
            throw new InvalidDataException("对话记录格式无效。");
    }
}
