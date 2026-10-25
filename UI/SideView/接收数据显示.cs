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
    private readonly StringBuilder _text = new();
    private Decoder _decoder = Encoding.UTF8.GetDecoder();
    public ReceivedDataView()
    {
        var panel = new StackPanel { Spacing = 16 };
        var title = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 10 };
        title.Children.Add(new FontIcon { Glyph = "\uE9D9", FontSize = 18 });
        title.Children.Add(new TextBlock { Text = "接收数据", FontSize = 18, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold }); panel.Children.Add(title);
        var toolbar = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        var clear = new Button { Content = "清空", CornerRadius = new(12) };
        clear.Click += (_, _) => { _bytes.Clear(); _text.Clear(); _decoder.Reset(); _output.Text = ""; };
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
    public void Append(byte[] bytes, string encoding, bool binary = false)
    {
        const int capacity = 65536;
        if (_encoding != encoding) { _encoding = encoding; _decoder = Tomography.Business.Helpers.EncodingHelper.GetEncodingOrDefault(encoding).GetDecoder(); }
        if (!binary)
        {
            var chars = new char[Tomography.Business.Helpers.EncodingHelper.GetEncodingOrDefault(encoding).GetMaxCharCount(bytes.Length)];
            var count = _decoder.GetChars(bytes, 0, bytes.Length, chars, 0, false);
            _text.Append(new string(chars, 0, count).Replace("\0", "␀")); TrimText();
        }
        if (bytes.Length >= capacity) { _bytes.Clear(); _bytes.AddRange(bytes.AsSpan(bytes.Length - capacity).ToArray()); }
        else { var excess = _bytes.Count + bytes.Length - capacity; if (excess > 0) _bytes.RemoveRange(0, excess); _bytes.AddRange(bytes); }
        if (!_refresh.IsEnabled) _refresh.Start();
    }
    public void AppendEmtFrame(uint[] values)
    {
        if (_text.Length > 0 && _text[_text.Length - 1] != '\n') _text.AppendLine();
        for (var i = 0; i < values.Length; i += 8)
            _text.AppendLine(string.Join(" ", values.Skip(i).Take(8).Select(value => value.ToString(System.Globalization.CultureInfo.InvariantCulture))));
        TrimText();
        if (!_refresh.IsEnabled) _refresh.Start();
    }
    private void TrimText()
    {
        const int capacity = 65536;
        if (_text.Length > capacity) _text.Remove(0, _text.Length - capacity);
    }
    private void Render()
    {
        if (_pause.IsChecked == true) return;
        _output.Text = _format.SelectedIndex == 1 ? string.Join(" ", _bytes.Select(value => value.ToString("X2"))) : _text.ToString();
        _output.SelectionStart = _output.Text.Length;
    }
}
