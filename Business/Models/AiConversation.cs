namespace Tomography.Business.Models;

public sealed class AiConversation
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Title { get; set; } = "新对话";
    public List<AiMessage> Messages { get; set; } = new();
    public override string ToString() => Title;
}

public sealed record AiMessage(string Role, string Content, string ReasoningSummary = "");

public sealed record AiChatReply(string Reply, string ReasoningSummary);

public sealed class AiApiSettings
{
    public bool UseCcSwitchConfig { get; set; }
    public string CcSwitchConfigPath { get; set; } = "";
    public string BaseUrl { get; set; } = "";
    public string Model { get; set; } = "";
    public string PythonDll { get; set; } = "";
    public List<string> Models { get; set; } = new();
    public string ApiMode { get; set; } = "";
    public string ReasoningMode { get; set; } = "";
    public string ReasoningEffort { get; set; } = "";
    public string ServiceTier { get; set; } = "";
    public string Verbosity { get; set; } = "";
    public string Tone { get; set; } = "";
    public string Language { get; set; } = "";
    public string OutputFormat { get; set; } = "";
    public string OutputInstructions { get; set; } = "";
    public string Instructions { get; set; } = "";
    public int MaxOutputTokens { get; set; }
    public int TimeoutSeconds { get; set; }
    public string ApiKeyEnvironmentVariable { get; set; } = "";
}
