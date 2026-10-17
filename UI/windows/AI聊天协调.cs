using System.IO;
using Tomography.Business.Models;
using Tomography.Business.Services;
using Windows.Security.Credentials;

namespace Tomography.UI.windows;

public sealed partial class AppWindow
{
    private readonly PythonChatService _chatService = new();
    private AiApiSettings _aiSettings = new();
    private GlobalSettings _globalSettings = new();
    private string _aiKey = "";
    private List<AiConversation> _conversations = new();
    private AiConversation? _conversation;
    private AiConversation? _pendingConversation;
    private string _pendingReply = "";
    private string _pendingReasoning = "";
    private bool _aiBusy;
    private bool _aiClosed;
    private bool _historyReadable = true;
    private static string AiSettingsPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Tomography", "setting.json");
    private string LegacyAiHistoryPath => Path.Combine(Path.GetDirectoryName(LegacyProjectSettingsPath)!, "conversations.json");
    private void InitializeAi()
    {
        try
        {
            _globalSettings = GlobalSettingsStore.Load(AiSettingsPath, Path.Combine(Path.GetDirectoryName(AiSettingsPath)!, "ai-settings.json"));
            _aiSettings = _globalSettings.AI;
            Root.RequestedTheme = Enum.Parse<Microsoft.UI.Xaml.ElementTheme>(_globalSettings.Theme);
            SideRail.RestoreTheme(Root.RequestedTheme);
            if (string.IsNullOrWhiteSpace(_aiSettings.PythonDll) || !File.Exists(_aiSettings.PythonDll))
                _aiSettings.PythonDll = PythonRuntimeHost.FindPythonDll();
            try
            {
                if (!string.IsNullOrWhiteSpace(_aiSettings.BaseUrl))
                {
                    var credential = new PasswordVault().Retrieve("Tomography.AI", _aiSettings.BaseUrl);
                    credential.RetrievePassword(); _aiKey = credential.Password;
                }
            }
            catch (System.Runtime.InteropServices.COMException ex) when (ex.HResult == unchecked((int)0x80070490)) { }
        }
        catch (Exception) { AiSettingsPanel.SetStatus("配置读取失败，请重新填写并保存。"); }
        AiSettingsPanel.LoadSettings(_aiSettings, _aiKey);
        AiChatView.SetConfiguration(_aiSettings);
        AiSettingsPanel.SaveRequested += (_, _) => SaveAiSettings();
        AiChatView.SendRequested += async (_, message) => await SendAiMessageAsync(message);
        DirectoryPanel.NewChatRequested += (_, _) => NewAiConversation();
        AiChatView.NewConversationRequested += (_, _) => NewAiConversation();
        AiChatView.ConfigurationRequested += (_, selection) => ChangeComposerConfiguration(selection.Model, selection.Effort);
        DirectoryPanel.ProjectChanged += (_, _) => { if (!_aiBusy) LoadAiHistory(); };
        Closed += (_, _) => _aiClosed = true;
        LoadAiHistory();
    }
    private void ChangeComposerConfiguration(string model, string effort)
    {
        if (_aiSettings.UseCcSwitchConfig) { AiChatView.SetStatus("模型与思考强度由 CC Switch 的 config.toml 管理。"); return; }
        if (_aiBusy) { AiChatView.SetStatus("请等待当前回复完成后切换模型配置。"); return; }
        var oldModel = _aiSettings.Model; var oldEffort = _aiSettings.ReasoningEffort;
        try
        {
            _aiSettings.Model = model; _aiSettings.ReasoningEffort = effort;
            SaveGlobalSettings();
            AiSettingsPanel.LoadSettings(_aiSettings, _aiKey); AiChatView.SetConfiguration(_aiSettings);
        }
        catch (Exception)
        {
            _aiSettings.Model = oldModel; _aiSettings.ReasoningEffort = oldEffort;
            AiChatView.SetStatus("模型配置保存失败。");
        }
    }
    private void NewAiConversation()
    {
        if (!_historyReadable) { AiChatView.SetStatus("请先恢复项目对话记录文件，再新建对话。"); return; }
        _conversation = new AiConversation(); _conversations.Insert(0, _conversation);
        AiChatView.ClearInput(); ShowAiConversation(); SaveAiHistory();
    }
    private void ShowAiConversation()
    {
        ViewModel.Section = "AI"; ViewModel.ParametersVisible = false; SideRail.SetSection("AI");
        AiChatView.ShowConversation(_conversation);
        if (_pendingConversation == _conversation && _pendingConversation is not null) AiChatView.ShowPending(_pendingReasoning, _pendingReply);
    }
    private void LoadAiHistory()
    {
        _historyReadable = true;
        try
        {
            _conversations = AiProjectStore.Load(DirectoryPanel.DirectoryPath, LegacyAiHistoryPath);
            _conversation = _conversations.FirstOrDefault();
            AiChatView.ShowConversation(_conversation); AiChatView.SetStatus("");
        }
        catch (Exception)
        {
            _conversations = new(); _conversation = null; _historyReadable = false;
            AiChatView.ShowConversation(null); AiChatView.SetStatus("对话记录读取失败，原文件已保留。");
        }
    }
    private void SaveGlobalSettings()
    {
        _globalSettings.AI = _aiSettings;
        GlobalSettingsStore.Save(AiSettingsPath, _globalSettings);
    }
    private void SaveAiSettings()
    {
        if (_aiBusy) { AiSettingsPanel.SetStatus("请等待当前回复完成后保存配置。"); return; }
        try
        {
            var settings = AiSettingsPanel.ReadSettings();
            if (!settings.UseCcSwitchConfig && !string.IsNullOrWhiteSpace(AiSettingsPanel.Key)) new PasswordVault().Add(new PasswordCredential("Tomography.AI", settings.BaseUrl, AiSettingsPanel.Key));
            if (!settings.UseCcSwitchConfig && string.IsNullOrWhiteSpace(AiSettingsPanel.Key))
            {
                var vault = new PasswordVault();
                try { vault.Remove(vault.Retrieve("Tomography.AI", settings.BaseUrl)); }
                catch (System.Runtime.InteropServices.COMException ex) when (ex.HResult == unchecked((int)0x80070490)) { }
            }
            _globalSettings.AI = settings;
            GlobalSettingsStore.Save(AiSettingsPath, _globalSettings);
            _aiSettings = settings; _aiKey = AiSettingsPanel.Key;
            AiChatView.SetConfiguration(settings); AiSettingsPanel.SetStatus("已保存");
        }
        catch (Exception ex) { AiSettingsPanel.SetStatus(ex is ArgumentException ? ex.Message : "保存失败，请检查凭据和文件存储权限。"); }
    }
    private void SaveAiHistory()
    {
        if (!_historyReadable) return;
        try { AiProjectStore.Save(DirectoryPanel.DirectoryPath, _conversations); }
        catch (Exception) { AiChatView.SetStatus("对话保存失败，请检查文件存储权限。"); }
    }
    private async Task SendAiMessageAsync(string message)
    {
        if (_aiBusy) return;
        if (!_historyReadable) { AiChatView.SetStatus("项目对话记录读取失败，暂不能保存新消息。"); return; }
        if (!_aiSettings.UseCcSwitchConfig && (string.IsNullOrWhiteSpace(_aiSettings.Model) || string.IsNullOrWhiteSpace(_aiSettings.BaseUrl)))
        { AiChatView.SetStatus("请先在设置中配置 API 地址、密钥和模型。"); return; }
        var conversation = _conversation;
        if (conversation is null)
        {
            conversation = new AiConversation(); _conversations.Insert(0, conversation); _conversation = conversation;
        }
        var userMessage = new AiMessage("user", message);
        // A failed request keeps the input available for retry and does not add a duplicate user turn.
        if (conversation.Messages.LastOrDefault() is not { Role: "user" } last || last.Content != message)
            conversation.Messages.Add(userMessage);
        if (conversation.Title == "新对话") conversation.Title = message.Length > 24 ? message[..24] + "…" : message;
        _aiBusy = true; _pendingConversation = conversation; _pendingReply = ""; _pendingReasoning = "";
        AiChatView.SetBusy(true); AiChatView.SetStatus(""); AiChatView.ClearInput();
        AiChatView.ShowConversation(conversation); AiChatView.ShowPending("", ""); SaveAiHistory();
        try
        {
            var reply = await _chatService.SendAsync(AiSettingsPath, _aiSettings.PythonDll, _aiKey, conversation.Messages.ToArray(), (kind, delta) =>
                DispatcherQueue.TryEnqueue(() =>
                {
                    if (_aiClosed || _pendingConversation != conversation) return;
                    if (kind == "reasoning_summary") _pendingReasoning += delta;
                    else if (kind == "reply") _pendingReply += delta;
                    if (_conversation == conversation) AiChatView.ShowPending(_pendingReasoning, _pendingReply);
                }));
            if (_aiClosed) return;
            conversation.Messages.Add(new AiMessage("assistant", reply.Reply, reply.ReasoningSummary));
            AiChatView.SetStatus("");
            SaveAiHistory();
            if (_conversation == conversation) AiChatView.ShowConversation(conversation);
        }
        catch (Exception ex)
        {
            if (!_aiClosed)
            {
                if (_conversation == conversation) AiChatView.RestoreInput(message);
                AiChatView.SetStatus(ex is FileNotFoundException or ArgumentException or InvalidOperationException
                    ? ex.Message : "API 请求失败，请检查接口配置、模型名称和 Python 运行时。");
            }
        }
        finally { _pendingConversation = null; _aiBusy = false; if (!_aiClosed) AiChatView.SetBusy(false); }
    }
}
