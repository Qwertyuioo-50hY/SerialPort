namespace Tomography.Business.Models;

public sealed class AiConversation
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Title { get; set; } = "新对话";
    public List<AiMessage> Messages { get; set; } = new();
    public override string ToString() => Title;
}

public sealed record AiMessage(string Role, string Content);

public sealed class AiApiSettings
{
    public string BaseUrl { get; set; } = "";
    public string Model { get; set; } = "";
    public string PythonDll { get; set; } = "";
}
