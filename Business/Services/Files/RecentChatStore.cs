using System.Text.Json;
using Tomography.Business.Models;

namespace Tomography.Business.Services;

public sealed record RecentChatEntry(string Project, string Id, string Title, DateTime Modified);

public static class RecentChatStore
{
    // Read-only index: never migrate or rewrite another project's history while rendering Home.
    public static IReadOnlyList<RecentChatEntry> Read(IEnumerable<string> projects)
    {
        var result = new List<RecentChatEntry>();
        foreach (var project in projects.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            foreach (var name in new[] { AiProjectStore.FolderName, "聊天" })
            {
                var root = Path.Combine(project, name);
                try
                {
                    if (!Directory.Exists(root)) continue;
                    foreach (var folder in Directory.EnumerateDirectories(root))
                    {
                        var id = Path.GetFileName(folder);
                        if (!Guid.TryParseExact(id, "N", out _)) continue;
                        var path = Path.Combine(folder, "conversation.json");
                        try
                        {
                            var chat = JsonSerializer.Deserialize<AiConversation>(File.ReadAllText(path));
                            if (chat?.Id == id && chat.Title is not null && chat.Messages is not null)
                                result.Add(new(project, id, chat.Title, File.GetLastWriteTimeUtc(path)));
                        }
                        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException) { }
                    }
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { }
            }
        }
        return result.OrderByDescending(x => x.Modified).DistinctBy(x => (x.Project.ToUpperInvariant(), x.Id)).Take(12).ToArray();
    }
}
