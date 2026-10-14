using System.Text.Json;
using Tomography.Business.Models;
using Tomography.Business.Services;

public static class AiProjectStoreTests
{
    public static void Run()
    {
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "ai-project-tests-" + Guid.NewGuid().ToString("N")));
        var project = Path.Combine(root, "Project");
        Directory.CreateDirectory(project);
        try
        {
            var chat = new AiConversation { Title = "项目聊天", Messages = new() { new("user", "你好") } };
            AiProjectStore.Save(project, new[] { chat });
            var path = Path.Combine(project, "聊天", chat.Id, "conversation.json");
            if (!File.Exists(path) || AiProjectStore.Load(project).Single().Messages.Single().Content != "你好") throw new Exception("project chat roundtrip");
            chat.Messages.Add(new("assistant", "回复", "接口返回的摘要"));
            AiProjectStore.Save(project, new[] { chat });
            if (AiProjectStore.Load(project).Single().Messages.Count != 2 || AiProjectStore.Load(project).Single().Messages[1].ReasoningSummary != "接口返回的摘要") throw new Exception("chat update");
            var old = new AiConversation { Title = "旧聊天" };
            var legacy = Path.Combine(root, "conversations.json");
            File.WriteAllText(legacy, JsonSerializer.Serialize(new[] { old, new AiConversation { Id = chat.Id, Title = "旧版本" } }));
            var migrated = AiProjectStore.Load(project, legacy);
            if (migrated.Count != 2 || !File.Exists(legacy) || migrated.Single(item => item.Id == chat.Id).Messages.Count != 2) throw new Exception("legacy migration overwrote current chat or source");
            if (AiProjectStore.Load(project, legacy).Count != 2) throw new Exception("migration duplicates");
            try { AiProjectStore.Save(project, new[] { new AiConversation { Id = "../escape" } }); throw new Exception("unsafe ID accepted"); }
            catch (InvalidDataException) { }
            File.WriteAllText(path, "{broken");
            try { AiProjectStore.Load(project); throw new Exception("broken chat accepted"); }
            catch (JsonException) { }
            if (File.ReadAllText(path) != "{broken") throw new Exception("broken original modified");
            Console.WriteLine("PASS: project chat folders, update, legacy migration, duplicate prevention, path validation and corrupted-file preservation.");
        }
        finally
        {
            if (!root.StartsWith(Path.GetFullPath(AppContext.BaseDirectory), StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("test cleanup outside workspace");
            Directory.Delete(root, true);
        }
    }
}
