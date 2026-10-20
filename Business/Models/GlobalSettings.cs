namespace Tomography.Business.Models;

public sealed class GlobalSettings
{
    public AiApiSettings AI { get; set; } = new();
    public string Language { get; set; } = "zh";
    public Dictionary<string, string> Shortcuts { get; set; } = new();
    public string Theme { get; set; } = "Light";
    public List<string> RecentProjects { get; set; } = new();
}
