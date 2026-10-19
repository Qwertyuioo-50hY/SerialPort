using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Tomography.Business.Models;
using Tomography.Business.Services;

namespace Tomography.UI.windows;

public sealed partial class AppWindow
{
    private async Task EditFrameDefinitionAsync()
    {
        if (_connecting || ActiveChannel.IsOpen || _sampling) { ShowDeviceError("请先断开设备再修改帧定义。"); return; }
        var old = _projectSettings.CustomFrame ?? new CustomFrameDefinition();
        var panel = new StackPanel { Spacing = 8 };
        TextBox Field(string name, string text, string id)
        {
            var field = new TextBox { Header = name, Text = text };
            AutomationProperties.SetAutomationId(field, id); panel.Children.Add(field); return field;
        }
        var header = Field("帧头（十六进制，必填）", old.HeaderHex, "FrameHeader");
        var length = Field("整帧字节数（含帧头、校验）", old.Length.ToString(), "FrameLength");
        var offset = Field("数值区起始字节偏移", old.DataOffset.ToString(), "FrameOffset");
        var type = new Tomography.UI.Controls.ClickComboBox { Header = "字段类型", ItemsSource = new[] { "UInt16", "Int16", "UInt32", "Int32", "Float32", "Float64" }, SelectedItem = old.FieldType, HorizontalAlignment = HorizontalAlignment.Stretch };
        var endian = new Tomography.UI.Controls.ClickComboBox { Header = "字节序", ItemsSource = new[] { "小端", "大端" }, SelectedIndex = old.BigEndian ? 1 : 0, HorizontalAlignment = HorizontalAlignment.Stretch };
        var checksum = new Tomography.UI.Controls.ClickComboBox { Header = "末尾单字节校验（覆盖此前全部字节）", ItemsSource = new[] { "None", "XOR8", "SUM8" }, SelectedItem = old.Checksum, HorizontalAlignment = HorizontalAlignment.Stretch };
        AutomationProperties.SetAutomationId(type, "FrameType"); AutomationProperties.SetAutomationId(endian, "FrameEndian"); AutomationProperties.SetAutomationId(checksum, "FrameChecksum");
        panel.Children.Add(type); panel.Children.Add(endian); panel.Children.Add(checksum);
        var map = Field("64 通道的数值字段索引（从 0 开始，逗号分隔）", string.Join(",", old.ChannelMap), "FrameMap");
        map.TextWrapping = TextWrapping.Wrap; map.AcceptsReturn = true;
        panel.Children.Add(new TextBlock { Text = "这是接收帧定义，不发送指令、不执行脚本、不证明固件兼容；请按真实帧填写。", TextWrapping = TextWrapping.Wrap });
        var error = new TextBlock { TextWrapping = TextWrapping.Wrap }; panel.Children.Add(error);
        var dialog = new ContentDialog { XamlRoot = Root.XamlRoot, RequestedTheme = Root.ActualTheme, Title = "自定义数值帧", Content = new ScrollViewer { Content = panel, MaxHeight = 480 }, PrimaryButtonText = "保存定义", CloseButtonText = "取消" };
        CustomFrameDefinition? definition = null;
        dialog.PrimaryButtonClick += (sender, e) =>
        {
            try
            {
                definition = new() { HeaderHex = header.Text, Length = int.Parse(length.Text), DataOffset = int.Parse(offset.Text),
                    FieldType = (string)type.SelectedItem, BigEndian = endian.SelectedIndex == 1, Checksum = (string)checksum.SelectedItem,
                    ChannelMap = map.Text.Split(',').Select(int.Parse).ToArray() };
                _ = new CustomFrameDecoder(definition);
            }
            catch (Exception ex) { definition = null; e.Cancel = true; error.Text = ex.Message; }
        };
        if (await dialog.ShowAsync() != ContentDialogResult.Primary || definition is null) return;
        _projectSettings.CustomFrame = definition; _reconstruction.InvalidateReference();
        DeviceView.SetParameterStatus("帧定义已保存；需用真实固件样例验证，并重采空场参考。");
        await SaveDeviceSettingsAsync();
    }
}
