using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Tomography.Business.Models;

namespace Tomography.UI.SideView;

public sealed partial class AiSettingsView : UserControl
{
    public event EventHandler? SaveRequested;
    public AiSettingsView()
    {
        InitializeComponent();
        SettingsCategories.SelectedIndex = 2;
    }
    private void SettingsCategoryChanged(object sender, SelectionChangedEventArgs e)
    {
        if (AiSettingsContent is null || CategoryPlaceholder is null) return;
        var item = SettingsCategories.SelectedItem as ListViewItem;
        var ai = item?.Tag as string == "AI";
        AiSettingsContent.Visibility = ai ? Visibility.Visible : Visibility.Collapsed;
        CategoryPlaceholder.Visibility = item is not null && !ai ? Visibility.Visible : Visibility.Collapsed;
        CategoryTitle.Text = item?.Content?.ToString() ?? "";
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
    public string Key => ApiKey.Password.Trim();
    public void LoadSettings(AiApiSettings settings, string key)
    {
        BaseUrl.Text = settings.BaseUrl ?? ""; PythonDll.Text = settings.PythonDll ?? "";
        settings.Models ??= new(); KeyEnvironment.Text = settings.ApiKeyEnvironmentVariable ?? ""; ApiKey.Password = key;
        Model.ItemsSource = settings.Models; Model.SelectedItem = settings.Model; Model.Text = settings.Model ?? "";
        Models.Text = string.Join(Environment.NewLine, settings.Models);
        SelectValue(ApiMode, settings.ApiMode); SelectValue(ReasoningMode, settings.ReasoningMode);
        SelectValue(ReasoningEffort, settings.ReasoningEffort); SelectValue(ServiceTier, settings.ServiceTier);
        SelectValue(Verbosity, settings.Verbosity); SelectValue(OutputFormat, settings.OutputFormat);
        Tone.Text = settings.Tone ?? "默认"; ResponseLanguage.Text = settings.Language ?? "跟随用户";
        Instructions.Text = settings.Instructions ?? ""; OutputInstructions.Text = settings.OutputInstructions ?? "";
        MaxOutputTokens.Value = settings.MaxOutputTokens; TimeoutSeconds.Value = settings.TimeoutSeconds;
    }
    private static string Value(ComboBox control) => (string)((ComboBoxItem)control.SelectedItem).Tag;
    private static void SelectValue(ComboBox control, string value)
    { control.SelectedItem = control.Items.OfType<ComboBoxItem>().FirstOrDefault(item => (string)item.Tag == value) ?? control.Items[0]; }
    public AiApiSettings ReadSettings()
    {
        if (!Uri.TryCreate(BaseUrl.Text.Trim(), UriKind.Absolute, out var uri) ||
            uri.Scheme is not ("http" or "https") || string.IsNullOrEmpty(uri.Host) ||
            uri.UserInfo.Length > 0 || uri.Query.Length > 0 || uri.Fragment.Length > 0)
            throw new ArgumentException("请填写有效的 HTTP/HTTPS API Base URL。");
        if (uri.AbsolutePath.TrimEnd('/').EndsWith("/messages") || uri.AbsolutePath.TrimEnd('/').EndsWith("/responses") || uri.AbsolutePath.TrimEnd('/').EndsWith("/chat/completions"))
            throw new ArgumentException("请填写 Base URL，不要包含 /messages、/responses 或 /chat/completions。");
        if (Value(ApiMode) == "chat_completions" && (Value(ReasoningMode) == "pro" || Value(ServiceTier) == "ultrafast"))
            throw new ArgumentException("Pro 模式和 Ultrafast 速度需要 Responses 接口。");
        if (!double.IsFinite(MaxOutputTokens.Value) || MaxOutputTokens.Value != Math.Truncate(MaxOutputTokens.Value) || MaxOutputTokens.Value < 0 || MaxOutputTokens.Value > int.MaxValue ||
            !double.IsFinite(TimeoutSeconds.Value) || TimeoutSeconds.Value != Math.Truncate(TimeoutSeconds.Value) || TimeoutSeconds.Value is < 1 or > 3600)
            throw new ArgumentException("Token 上限和超时必须为允许范围内的整数。");
        if (string.IsNullOrWhiteSpace(Model.Text) || (string.IsNullOrWhiteSpace(Key) && string.IsNullOrWhiteSpace(KeyEnvironment.Text)))
            throw new ArgumentException("请填写模型名称，以及 API Key 或密钥环境变量。");
        if (!System.IO.File.Exists(PythonDll.Text.Trim())) throw new ArgumentException("Python 运行时 DLL 不存在。");
        return new AiApiSettings {
            BaseUrl = BaseUrl.Text.Trim().TrimEnd('/'), Model = Model.Text.Trim(), PythonDll = PythonDll.Text.Trim(),
            Models = Models.Text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).Select(value => value.Trim()).Where(value => value.Length > 0).Append(Model.Text.Trim()).Distinct().ToList(),
            ApiMode = Value(ApiMode), ReasoningMode = Value(ReasoningMode), ReasoningEffort = Value(ReasoningEffort),
            ServiceTier = Value(ServiceTier), Verbosity = Value(Verbosity), OutputFormat = Value(OutputFormat),
            Tone = Tone.Text.Trim(), Language = ResponseLanguage.Text.Trim(), ApiKeyEnvironmentVariable = KeyEnvironment.Text.Trim(), Instructions = Instructions.Text,
            OutputInstructions = OutputInstructions.Text, MaxOutputTokens = (int)MaxOutputTokens.Value, TimeoutSeconds = (int)TimeoutSeconds.Value
        };
    }
    public void SetStatus(string text) => SaveStatus.Text = text;
    private void SaveClicked(object sender, RoutedEventArgs e) => SaveRequested?.Invoke(this, EventArgs.Empty);
}
