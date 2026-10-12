using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Windows.System;

namespace Tomography.UI.windows;

public sealed partial class AppWindow
{
    private readonly Tomography.UI.SideView.DeviceParametersView _deviceSettings = new();

    private void BuildParameterContents()
    {
        if (_reconstructionSettings?.Parent is Panel algorithmParent) algorithmParent.Children.Remove(_reconstructionSettings);
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
        TextBox TextField(string label, string id)
        {
            var field = new TextBox
            {
                Header = label,
                PlaceholderText = label
            };
            AutomationProperties.SetAutomationId(field, id);
            ParameterBody.Children.Add(field);
            return field;
        }

        switch (ViewModel.Section)
        {
            case "设备":
            case "接口/协议":
                break;
            case "插件":
                Caption("插件管理");
                ParameterBody.Children.Add(new TextBlock { Text = "暂无已安装插件", TextWrapping = TextWrapping.Wrap });
                break;
            case "算法":
                AddReconstructionSettings(ParameterBody);
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
    private void ParameterDragDelta(object sender, DragDeltaEventArgs e)
    {
        _parameterWidth += e.HorizontalChange;
        UpdatePresentation();
    }

    private void ParameterDividerKeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key != VirtualKey.Left && e.Key != VirtualKey.Right) return;
        _parameterWidth += e.Key == VirtualKey.Right ? 16 : -16;
        UpdatePresentation();
        e.Handled = true;
    }

}
