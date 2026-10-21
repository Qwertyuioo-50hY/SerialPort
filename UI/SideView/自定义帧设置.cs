using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Tomography.Business.Models;
using Tomography.Business.Services;
using Tomography.UI.Controls;

namespace Tomography.UI.SideView;

public sealed class FrameSettingsView : UserControl
{
    public event EventHandler<CustomFrameDefinition>? SaveRequested;
    private readonly TextBox _header = Field("帧头 · HEX", "AA BB");
    private readonly TextBox _footer = Field("帧尾 · HEX", "0D 0A");
    private readonly NumberBox _length = new() { Header = "整帧字节数 · 0 按帧尾分隔", Minimum = 0, Maximum = 65536, Value = 0, CornerRadius = new(12) };
    private readonly NumberBox _checksumStart = new() { Header = "校验起始偏移 · 从 0 开始", Minimum = 0, Maximum = 65535, Value = 0, CornerRadius = new(12) };
    private readonly ClickComboBox _checksum = new() { Header = "校验算法", ItemsSource = new[] { "None", "CRC16 Modbus" }, SelectedIndex = 1, HorizontalAlignment = HorizontalAlignment.Stretch, CornerRadius = new(12) };
    private readonly TextBlock _status = new() { TextWrapping = TextWrapping.Wrap, FontSize = 12 };
    private CustomFrameDefinition _loaded = new() { ChannelMap = Array.Empty<int>() };
    public FrameSettingsView()
    {
        var body = new StackPanel { Spacing = 20 };
        var title = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 10 };
        title.Children.Add(new FontIcon { Glyph = "\uE943", FontSize = 18 });
        title.Children.Add(new TextBlock { Text = "自定义帧配置", FontSize = 18, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold }); body.Children.Add(title);
        var grid = new Grid { ColumnSpacing = 20, RowSpacing = 16 };
        grid.ColumnDefinitions.Add(new()); grid.ColumnDefinitions.Add(new());
        var fields = new FrameworkElement[] { _header, _footer, _length, _checksum, _checksumStart };
        for (var i = 0; i < fields.Length; i++)
        {
            if (i % 2 == 0) grid.RowDefinitions.Add(new() { Height = GridLength.Auto });
            Grid.SetRow(fields[i], i / 2); Grid.SetColumn(fields[i], i % 2); grid.Children.Add(fields[i]);
        }
        body.Children.Add(grid);
        body.Children.Add(new TextBlock { Text = "CRC 低字节在前，位于帧尾之前；从起始偏移校验到 CRC 字节之前。", FontSize = 12, TextWrapping = TextWrapping.Wrap, Foreground = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["TextFillColorSecondaryBrush"] });
        var save = new Button { Content = "保存帧配置", CornerRadius = new(12) };
        save.Click += (_, _) =>
        {
            try
            {
                if (!double.IsFinite(_length.Value) || _length.Value != Math.Truncate(_length.Value) || !double.IsFinite(_checksumStart.Value) || _checksumStart.Value != Math.Truncate(_checksumStart.Value)) throw new ArgumentException("长度和偏移必须为整数。");
                var definition = new CustomFrameDefinition { HeaderHex = _header.Text.Trim(), FooterHex = _footer.Text.Trim(), Length = (int)_length.Value,
                    ChecksumStart = (int)_checksumStart.Value, Checksum = (string)_checksum.SelectedItem,
                    DataOffset = _loaded.DataOffset, FieldType = _loaded.FieldType, BigEndian = _loaded.BigEndian, ChannelMap = _loaded.ChannelMap.ToArray() };
                _ = new CustomFrameDecoder(definition);
                SaveRequested?.Invoke(this, definition);
            }
            catch (Exception ex) { _status.Text = ex.Message; }
        };
        body.Children.Add(save); body.Children.Add(_status); Content = body;
    }
    public void LoadDefinition(CustomFrameDefinition? definition)
    {
        _loaded = definition ?? new() { ChannelMap = Array.Empty<int>() };
        _header.Text = _loaded.HeaderHex; _footer.Text = _loaded.FooterHex; _length.Value = _loaded.Length;
        _checksum.SelectedItem = _loaded.Checksum is "None" or "CRC16 Modbus" ? _loaded.Checksum : "CRC16 Modbus";
        _checksumStart.Value = _loaded.ChecksumStart;
    }
    public void SetStatus(string text) => _status.Text = text;
    private static TextBox Field(string title, string placeholder) => new() { Header = title, PlaceholderText = placeholder, CornerRadius = new(12) };
}
