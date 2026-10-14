using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Tomography.Business.Models;

namespace Tomography.UI.SideView;

public sealed partial class AiSettingsView : UserControl
{
    public event EventHandler? SaveRequested;
    public AiSettingsView() => InitializeComponent();
    public string Key => ApiKey.Password.Trim();
    public void LoadSettings(AiApiSettings settings, string key)
    { BaseUrl.Text = settings.BaseUrl; Model.Text = settings.Model; PythonDll.Text = settings.PythonDll; ApiKey.Password = key; }
    public AiApiSettings ReadSettings()
    {
        if (!Uri.TryCreate(BaseUrl.Text.Trim(), UriKind.Absolute, out var uri) ||
            uri.Scheme is not ("http" or "https") || string.IsNullOrEmpty(uri.Host) ||
            uri.UserInfo.Length > 0 || uri.Query.Length > 0 || uri.Fragment.Length > 0)
            throw new ArgumentException("请填写有效的 HTTP/HTTPS API Base URL。");
        if (string.IsNullOrWhiteSpace(Model.Text) || string.IsNullOrWhiteSpace(Key))
            throw new ArgumentException("请填写模型名称和 API Key。");
        if (!System.IO.File.Exists(PythonDll.Text.Trim())) throw new ArgumentException("Python 运行时 DLL 不存在。");
        return new AiApiSettings { BaseUrl = BaseUrl.Text.Trim().TrimEnd('/'), Model = Model.Text.Trim(), PythonDll = PythonDll.Text.Trim() };
    }
    public void SetStatus(string text) => SaveStatus.Text = text;
    private void SaveClicked(object sender, RoutedEventArgs e) => SaveRequested?.Invoke(this, EventArgs.Empty);
}
