using System.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Tomography.UI.Controls;

namespace Tomography.UI.SideView;

public sealed class ReceivedDataView : UserControl
{
    private readonly List<byte> _bytes = new();
    private readonly TextBox _output = new() { IsReadOnly = true, AcceptsReturn = true, TextWrapping = TextWrapping.Wrap, MinHeight = 260, MaxHeight = 360, FontFamily = new Microsoft.UI.Xaml.Media.FontFamily("Cascadia Mono"), FontSize = 13, CornerRadius = new(12) };
    private readonly ClickComboBox _format = new() { ItemsSource = new[] { "文本", "十六进制" }, SelectedIndex = 0, MinWidth = 100, CornerRadius = new(12) };
    private readonly ToggleButton _pause = new() { Content = "暂停", CornerRadius = new(12) };
    private readonly DispatcherTimer _refresh = new() { Interval = TimeSpan.FromMilliseconds(80) };
    private string _encoding = "UTF-8";
    public ReceivedDataView()
    {
        var panel = new StackPanel { Spacing = 16 };
        var title = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 10 };
        title.Children.Add(new FontIcon { Glyph = "\uE9D9", FontSize = 18 });
        title.Children.Add(new TextBlock { Text = "接收数据", FontSize = 18, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold }); panel.Children.Add(title);
        var toolbar = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        var clear = new Button { Content = "清空", CornerRadius = new(12) };
        clear.Click += (_, _) => { _bytes.Clear(); _output.Text = ""; };
        _format.SelectionChanged += (_, _) => Render();
        _pause.Checked += (_, _) => _pause.Content = "继续";
        _pause.Unchecked += (_, _) => { _pause.Content = "暂停"; Render(); };
        toolbar.Children.Add(_format); toolbar.Children.Add(_pause); toolbar.Children.Add(clear);
        panel.Children.Add(toolbar); panel.Children.Add(_output); Content = panel;
        ScrollViewer.SetVerticalScrollBarVisibility(_output, ScrollBarVisibility.Hidden);
        ScrollViewer.SetHorizontalScrollBarVisibility(_output, ScrollBarVisibility.Disabled);
        _refresh.Tick += (_, _) => { _refresh.Stop(); Render(); };
        Unloaded += (_, _) => _refresh.Stop(); Loaded += (_, _) => Render();
    }
    public void Append(byte[] bytes, string encoding)
    {
        const int capacity = 65536;
        _encoding = encoding;
        if (bytes.Length >= capacity) { _bytes.Clear(); _bytes.AddRange(bytes.AsSpan(bytes.Length - capacity).ToArray()); }
        else { var excess = _bytes.Count + bytes.Length - capacity; if (excess > 0) _bytes.RemoveRange(0, excess); _bytes.AddRange(bytes); }
        if (!_refresh.IsEnabled) _refresh.Start();
    }
    private void Render()
    {
        if (_pause.IsChecked == true) return;
        var bytes = _bytes.ToArray();
        // Preserve partial multi-byte characters until the next receive callback.
        var encoding = Tomography.Business.Helpers.EncodingHelper.GetEncodingOrDefault(_encoding);
        var decoder = encoding.GetDecoder();
        var chars = new char[encoding.GetMaxCharCount(bytes.Length)];
        var count = decoder.GetChars(bytes, 0, bytes.Length, chars, 0, false);
        _output.Text = _format.SelectedIndex == 1 ? string.Join(" ", bytes.Select(value => value.ToString("X2"))) : new string(chars, 0, count).Replace("\0", "␀");
        _output.SelectionStart = _output.Text.Length;
    }
}
