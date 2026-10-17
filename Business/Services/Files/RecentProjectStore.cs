using Tomography.Business.Models;

namespace Tomography.Business.Services;

public static class RecentProjectStore
{
    public static void Remember(GlobalSettings settings, string path)
    {
        var fullPath = Path.GetFullPath(path);
        settings.RecentProjects = new[] { fullPath }.Concat(settings.RecentProjects ?? new())
            .Distinct(StringComparer.OrdinalIgnoreCase).Take(8).ToList();
    }
}
