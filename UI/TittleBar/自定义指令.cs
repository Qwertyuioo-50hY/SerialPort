using Microsoft.UI.Xaml.Controls;
namespace Tomography.UI.windows;

public sealed partial class AppWindow
{
    private async Task AddCustomCommandAsync()
    {
        var name = new TextBox { Header = "命令名称", Text = "命令0" };
        var text = new TextBox { Header = "发送内容", AcceptsReturn = true };
        var body = new StackPanel { Spacing = 8 };
        body.Children.Add(name);
        body.Children.Add(text);
        var dialog = new ContentDialog { XamlRoot = Root.XamlRoot, RequestedTheme = Root.ActualTheme,
            Title = "添加命令", Content = body, PrimaryButtonText = "添加", CloseButtonText = "取消", IsPrimaryButtonEnabled = false };
        void Validate(object sender, TextChangedEventArgs e) => dialog.IsPrimaryButtonEnabled =
            !string.IsNullOrWhiteSpace(name.Text) && !string.IsNullOrWhiteSpace(text.Text);
        name.TextChanged += Validate;
        text.TextChanged += Validate;
        if (await dialog.ShowAsync() == ContentDialogResult.Primary) AppTitleBar.AddCustomCommand(name.Text.Trim(), text.Text);
    }

    private void CustomCommandRequested(object? sender, string text)
    {
        if (_connecting || _sampling) { ConnectionStatus.Text = "设备正在握手或采样，请稍后发送"; return; }
        if (!_deviceReady) { ConnectionStatus.Text = "请先连接设备并完成握手"; return; }
        ConnectionStatus.Text = _serialConnection.SendText(text).Message;
    }

    private async Task ConfigureAsync(string title)
    {
        var body = new StackPanel { Spacing = 8 };
        if (title == "自定义API")
        {
            body.Children.Add(new TextBox { Header = "API地址", PlaceholderText = "https://" });
            body.Children.Add(new PasswordBox { Header = "API密钥" });
            body.Children.Add(new TextBox { Header = "模型名称" });
        }
        else
        {
            var format = new Tomography.UI.Controls.ClickComboBox { Header = "波形显示", ItemsSource = new[] { "全场波形图", "物场波形图", "叠加波形图" }, SelectedIndex = 1 };
            body.Children.Add(format);
            body.Children.Add(new TextBlock { Text = "可在工作台切换波形类型" });
        }
        await new ContentDialog { XamlRoot = Root.XamlRoot, RequestedTheme = Root.ActualTheme,
            Title = title, Content = body, CloseButtonText = "关闭" }.ShowAsync();
    }
}
