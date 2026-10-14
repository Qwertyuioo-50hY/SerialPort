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
    public List<string> Models { get; set; } = new() { "gpt-6.1-sol" };
    public string ApiMode { get; set; } = "responses";
    public string ReasoningMode { get; set; } = "standard";
    public string ReasoningEffort { get; set; } = "medium";
    public string ServiceTier { get; set; } = "default";
    public string Verbosity { get; set; } = "default";
    public string Tone { get; set; } = "默认";
    public string Language { get; set; } = "跟随用户";
    public string OutputFormat { get; set; } = "text";
    public string OutputInstructions { get; set; } = "";
    public string Instructions { get; set; } = "";
    public int MaxOutputTokens { get; set; }
    public int TimeoutSeconds { get; set; } = 120;
    public string ApiKeyEnvironmentVariable { get; set; } = "OPENAI_API_KEY";
}
