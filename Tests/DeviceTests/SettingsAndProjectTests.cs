using System.Text.Json;
using Tomography.Business.Models;
using Tomography.Business.Services;

public static class SettingsAndProjectTests
{
    public static void Run()
    {
        var root = Path.Combine(AppContext.BaseDirectory, "obj", "settings-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var path = ProjectStore.Create(root, "自定义项目");
            if (!Directory.Exists(Path.Combine(path, "data")) || !Directory.Exists(Path.Combine(path, "chats")) ||
                !new ConfigService().Load(Path.Combine(path, "project.json")).IsSuccess) throw new Exception("project layout");
            try { ProjectStore.Create(root, "自定义项目"); throw new Exception("duplicate accepted"); } catch (IOException) { }
            foreach (var name in new[] { "..", "../escape", "CON", "LPT1", "" })
            {
                try { ProjectStore.Create(root, name); throw new Exception("invalid name accepted"); } catch (ArgumentException) { }
            }
            var global = Path.Combine(root, "setting.json");
            var legacy = Path.Combine(root, "ai-settings.json");
            File.WriteAllText(legacy, JsonSerializer.Serialize(new AiApiSettings { Model = "claude-sonnet-5", ApiMode = "anthropic_messages" }));
            var settings = GlobalSettingsStore.Load(global, legacy);
            if (settings.AI.Model != "claude-sonnet-5" || !File.Exists(legacy)) throw new Exception("AI migration");
            settings.Theme = "Dark";
            GlobalSettingsStore.Save(global, settings);
            if (GlobalSettingsStore.Load(global).Theme != "Dark" || GlobalSettingsStore.Load(global).AI.ApiMode != "anthropic_messages")
                throw new Exception("global settings roundtrip");
            File.WriteAllText(global, "{broken");
            try { GlobalSettingsStore.Load(global, legacy); throw new Exception("broken settings accepted"); } catch (JsonException) { }
            if (File.ReadAllText(global) != "{broken") throw new Exception("broken settings overwritten");
            Console.WriteLine("PASS: named project layout, duplicate/name validation, global settings migration, roundtrip and corrupt-file preservation.");
        }
        finally
        {
            var resolved = Path.GetFullPath(root);
            if (!resolved.StartsWith(Path.GetFullPath(AppContext.BaseDirectory).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("test cleanup outside workspace");
            Directory.Delete(resolved, true);
        }
    }
}
