using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Tomography.Business.Models;

namespace Tomography.UI.SideView;

public sealed partial class AiSettingsView : UserControl
{
    public AppearanceSettingsView Appearance => AppearanceSettings;
    private bool _loading;
    private readonly DispatcherTimer _autoSave = new() { Interval = TimeSpan.FromMilliseconds(800) };
    private AiApiSettings _loadedSettings = new();
    public event EventHandler? SaveRequested;
    public AiSettingsView()
    {
        InitializeComponent();
        SettingsCategories.SelectedIndex = 0;
        _autoSave.Tick += (_, _) => { _autoSave.Stop(); try { ReadSettings(); } catch (ArgumentException) { return; } SaveRequested?.Invoke(this, EventArgs.Empty); };
        foreach (var box in new[] { BaseUrl, KeyEnvironment, Models, Instructions, OutputInstructions }) box.TextChanged += (_, _) => QueueAutoSave();
        ApiKey.PasswordChanged += (_, _) => QueueAutoSave();
        foreach (var combo in new ComboBox[] { Model, ApiMode, ReasoningMode, ReasoningEffort, ServiceTier, Verbosity, OutputFormat, Tone, ResponseLanguage })
        {
            combo.SelectionChanged += (_, _) => QueueAutoSave();
            combo.LostFocus += (_, _) => QueueAutoSave();
        }
        MaxOutputTokens.ValueChanged += (_, _) => QueueAutoSave();
        TimeoutSeconds.ValueChanged += (_, _) => QueueAutoSave();
        Unloaded += (_, _) => _autoSave.Stop();
    }
    private void QueueAutoSave()
    {
        if (_loading) return;
        _autoSave.Stop(); _autoSave.Start();
    }
    private void SettingsCategoryChanged(object sender, SelectionChangedEventArgs e)
    {
        if (AiSettingsContent is null || CategoryPlaceholder is null) return;
        var item = SettingsCategories.SelectedItem as ListViewItem;
        var ai = item?.Tag as string == "AI";
        var appearance = item?.Tag as string == "Appearance";
        AppearanceSettings.Visibility = appearance ? Visibility.Visible : Visibility.Collapsed;
        var profile = item?.Tag as string == "Profile";
        ProfileSettings.Visibility = profile ? Visibility.Visible : Visibility.Collapsed;
        var shortcuts = item?.Tag as string == "Shortcuts";
        ShortcutSettings.Visibility = shortcuts ? Visibility.Visible : Visibility.Collapsed;
        AiSettingsContent.Visibility = ai ? Visibility.Visible : Visibility.Collapsed;
        CategoryPlaceholder.Visibility = item is not null && !ai && !shortcuts && !profile && !appearance ? Visibility.Visible : Visibility.Collapsed;
        CategoryTitle.Text = item?.Content?.ToString() ?? "";
        DispatcherQueue.TryEnqueue(() => SetLanguage(_english));
    }
    private void SettingsSearchChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
    {
        if (SettingsCategories is null) return;
        var query = sender.Text.Trim();
        var visible = SettingsCategories.Items.OfType<ListViewItem>().Where(item =>
            item.Content.ToString()!.Contains(query, StringComparison.OrdinalIgnoreCase)).ToArray();
        foreach (var item in SettingsCategories.Items.OfType<ListViewItem>())
            item.Visibility = visible.Contains(item) ? Visibility.Visible : Visibility.Collapsed;
        if (!visible.Contains(SettingsCategories.SelectedItem)) SettingsCategories.SelectedItem = visible.FirstOrDefault();
        SettingsSearchEmpty.Visibility = visible.Length == 0 ? Visibility.Visible : Visibility.Collapsed;
    }
    private bool _english;
    public ProfileSettingsView Profile => ProfileSettings;
    public void ShowProfile()
    {
        SettingsSearch.Text = "";
        ProfileCategory.Visibility = Visibility.Visible;
        SettingsCategories.SelectedItem = ProfileCategory;
    }
    public ShortcutSettingsView Shortcuts => ShortcutSettings;
    public void SetLanguage(bool english)
    {
        _english = english;
        Tomography.UI.Controls.InterfaceLanguage.Apply(this, english);
        foreach (var item in SettingsCategories.Items.OfType<ListViewItem>())
            if (item.Content is string text) item.Content = Tomography.UI.Controls.InterfaceLanguage.Text(text, english);
        ShortcutSettings.SetLanguage(english);
        ProfileSettings.SetLanguage(english);
    }
    public string Key => ApiKey.Password.Trim();
    public void LoadSettings(AiApiSettings settings, string key)
    {
        _autoSave.Stop(); _loading = true;
        _loadedSettings = settings;
        BaseUrl.Text = settings.BaseUrl ?? "";
        settings.Models ??= new(); KeyEnvironment.Text = settings.ApiKeyEnvironmentVariable ?? ""; ApiKey.Password = key;
        Model.ItemsSource = settings.Models; Model.SelectedItem = settings.Model; Model.Text = settings.Model ?? "";
        Models.Text = string.Join(Environment.NewLine, settings.Models);
        SelectValue(ApiMode, settings.ApiMode); SelectValue(ReasoningMode, settings.ReasoningMode);
        SelectValue(ReasoningEffort, settings.ReasoningEffort); SelectValue(ServiceTier, settings.ServiceTier);
        SelectValue(Verbosity, settings.Verbosity); SelectValue(OutputFormat, settings.OutputFormat);
        Tone.Text = settings.Tone ?? "默认"; ResponseLanguage.Text = settings.Language ?? "跟随用户";
        Instructions.Text = settings.Instructions ?? ""; OutputInstructions.Text = settings.OutputInstructions ?? "";
        MaxOutputTokens.Value = settings.MaxOutputTokens == 0 ? double.NaN : settings.MaxOutputTokens; TimeoutSeconds.Value = settings.TimeoutSeconds == 0 ? double.NaN : settings.TimeoutSeconds;
        _loading = false;
    }
    private static string Value(ComboBox control) => (control.SelectedItem as ComboBoxItem)?.Tag as string ?? "";
    private static void SelectValue(ComboBox control, string value)
    { control.SelectedItem = control.Items.OfType<ComboBoxItem>().FirstOrDefault(item => (string)item.Tag == value) ; }
    public AiApiSettings ReadSettings()
    {
        var tokens = double.IsNaN(MaxOutputTokens.Value) ? 0 : MaxOutputTokens.Value;
        var timeout = double.IsNaN(TimeoutSeconds.Value) ? 120 : TimeoutSeconds.Value;
        if (!double.IsFinite(tokens) || tokens != Math.Truncate(tokens) || tokens < 0 || tokens > int.MaxValue ||
            !double.IsFinite(timeout) || timeout != Math.Truncate(timeout) || timeout is < 1 or > 3600)
            throw new ArgumentException("Token 上限和超时必须为允许范围内的整数。");
        if (!Uri.TryCreate(BaseUrl.Text.Trim(), UriKind.Absolute, out var uri) ||
            uri.Scheme is not ("http" or "https") || string.IsNullOrEmpty(uri.Host) ||
            uri.UserInfo.Length > 0 || uri.Query.Length > 0 || uri.Fragment.Length > 0)
            throw new ArgumentException("请填写有效的 HTTP/HTTPS API Base URL。");
        if (uri.AbsolutePath.TrimEnd('/').EndsWith("/messages") || uri.AbsolutePath.TrimEnd('/').EndsWith("/responses") || uri.AbsolutePath.TrimEnd('/').EndsWith("/chat/completions"))
            throw new ArgumentException("请填写 Base URL，不要包含 /messages、/responses 或 /chat/completions。");
        if (Value(ApiMode) == "chat_completions" && (Value(ReasoningMode) == "pro" || Value(ServiceTier) == "ultrafast"))
            throw new ArgumentException("Pro 模式和 Ultrafast 速度需要 Responses 接口。");
        if (string.IsNullOrWhiteSpace(Model.Text) || (string.IsNullOrWhiteSpace(Key) && string.IsNullOrWhiteSpace(KeyEnvironment.Text)))
            throw new ArgumentException("请填写模型名称，以及 API Key 或密钥环境变量。");
        if (!System.IO.File.Exists(_loadedSettings.PythonDll)) throw new ArgumentException("Python 运行时 DLL 不存在。");
        return new AiApiSettings {
            BaseUrl = BaseUrl.Text.Trim().TrimEnd('/'), Model = Model.Text.Trim(), PythonDll = _loadedSettings.PythonDll,
            Models = Models.Text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).Select(value => value.Trim()).Where(value => value.Length > 0).Append(Model.Text.Trim()).Distinct().ToList(),
            ApiMode = string.IsNullOrEmpty(Value(ApiMode)) ? "responses" : Value(ApiMode), ReasoningMode = string.IsNullOrEmpty(Value(ReasoningMode)) ? "standard" : Value(ReasoningMode), ReasoningEffort = string.IsNullOrEmpty(Value(ReasoningEffort)) ? "default" : Value(ReasoningEffort),
            ServiceTier = string.IsNullOrEmpty(Value(ServiceTier)) ? "default" : Value(ServiceTier), Verbosity = string.IsNullOrEmpty(Value(Verbosity)) ? "default" : Value(Verbosity), OutputFormat = string.IsNullOrEmpty(Value(OutputFormat)) ? "text" : Value(OutputFormat),
            Tone = Tone.Text.Trim(), Language = ResponseLanguage.Text.Trim(), ApiKeyEnvironmentVariable = KeyEnvironment.Text.Trim(), Instructions = Instructions.Text,
            OutputInstructions = OutputInstructions.Text, MaxOutputTokens = (int)tokens, TimeoutSeconds = (int)timeout
        };
    }
    public void SetStatus(string text) => SaveStatus.Text = text;
    private void SaveClicked(object sender, RoutedEventArgs e) => SaveRequested?.Invoke(this, EventArgs.Empty);
}
