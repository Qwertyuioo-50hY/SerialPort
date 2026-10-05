using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;

namespace Tomography.UI.Views;

public sealed partial class MainWindow
{
    private void BuildParameterContents()
    {
        ParameterBody.Children.Clear();
        void Caption(string text) => ParameterBody.Children.Add(new TextBlock
        {
            Text = text,
            Style = (Style)Application.Current.Resources["CaptionTextBlockStyle"],
            Margin = new Thickness(0, 8, 0, 0)
        });
        void DisabledButton(string label, string id)
        {
            var button = new Button
            {
                Content = label,
                IsEnabled = false,
                HorizontalAlignment = HorizontalAlignment.Stretch
            };
            AutomationProperties.SetAutomationId(button, id);
            ParameterBody.Children.Add(button);
        }
        TextBox TextField(string label, string id, bool multiline = false)
        {
            var field = new TextBox
            {
                Header = label,
                PlaceholderText = multiline ? "输入问题…" : label,
                AcceptsReturn = multiline,
                TextWrapping = multiline ? TextWrapping.Wrap : TextWrapping.NoWrap,
                MinHeight = multiline ? 130 : 32
            };
            AutomationProperties.SetAutomationId(field, id);
            ParameterBody.Children.Add(field);
            return field;
        }

        switch (ViewModel.Section)
        {
            case "接口/协议":
                _protocolSettings ??= new Tomography.UI.Controls.ProtocolSettingsView();
                _protocolSettings.SetPorts(_serialConnection.GetPortNames());
                ParameterBody.Children.Add(_protocolSettings);
                AddAcquisitionSettings(ParameterBody);
                break;
            case "插件":
                Caption("插件管理");
                ParameterBody.Children.Add(new TextBlock { Text = "暂无已安装插件", TextWrapping = TextWrapping.Wrap });
                break;
            case "算法":
                AddReconstructionSettings(ParameterBody);
                break;
            case "AI":
                Caption("AI 辅助分析");
                ParameterBody.Children.Add(new TextBlock { Text = "分析采集数据与重建图像", TextWrapping = TextWrapping.Wrap });
                TextField("问题", "AIQuestion", true);
                DisabledButton("发送", "SendAIQuestion");
                break;
            case "登录":
                Caption("账户登录");
                TextField("账号", "Account");
                var password = new PasswordBox { Header = "密码", PlaceholderText = "密码" };
                AutomationProperties.SetAutomationId(password, "Password");
                ParameterBody.Children.Add(password);
                DisabledButton("登录", "SignIn");
                break;
        }
    }
}
