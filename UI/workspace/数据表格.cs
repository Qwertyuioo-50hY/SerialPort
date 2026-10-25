using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace Tomography.UI.workspace;

public sealed class ChannelDataView : UserControl
{
    private readonly ListView _rows = new() { Height = 360 };
    private readonly Brush _line = new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(55, 128, 128, 128));
    private double[]? _reference;
    private double[]? _measurements;
    public ChannelDataView()
    {
        var panel = new Grid();
        panel.RowDefinitions.Add(new() { Height = new GridLength(44) });
        panel.RowDefinitions.Add(new() { Height = new GridLength(1, GridUnitType.Star) });
        var style = new Style(typeof(ListViewItem));
        style.Setters.Add(new Setter(HorizontalContentAlignmentProperty, HorizontalAlignment.Stretch));
        style.Setters.Add(new Setter(PaddingProperty, new Thickness(0)));
        style.Setters.Add(new Setter(MinHeightProperty, 36d));
        _rows.ItemContainerStyle = style;
        panel.Children.Add(Row(true, "通道", "空场 (V)", "物场 (V)", "差值 (V)"));
        Grid.SetRow(_rows, 1); panel.Children.Add(_rows); Content = panel;
        SizeChanged += (_, e) => _rows.Height = Math.Max(360, e.NewSize.Height - 44);
        ScrollViewer.SetVerticalScrollBarVisibility(_rows, ScrollBarVisibility.Hidden);
        ScrollViewer.SetHorizontalScrollBarVisibility(_rows, ScrollBarVisibility.Disabled);
        _rows.AddHandler(PointerWheelChangedEvent, new Microsoft.UI.Xaml.Input.PointerEventHandler((_, args) => args.Handled = true), true);
    }
    public void SetReference(double[] values) { _reference = values.ToArray(); Refresh(); }
    public void SetMeasurements(double[] values) { _measurements = values.ToArray(); Refresh(); }
    private static string Voltage(double value) => (value / 1_000_000d).ToString("0.#########", System.Globalization.CultureInfo.InvariantCulture);
    private void Refresh()
    {
        _rows.Items.Clear(); var values = _measurements ?? _reference;
        if (values is null) return;
        for (var i = 0; i < values.Length; i++)
            _rows.Items.Add(Row(false, (i + 1).ToString(), _reference is null ? "—" : Voltage(_reference[i]), _measurements is null ? "—" : Voltage(_measurements[i]),
                _reference is not null && _measurements is not null ? Voltage(_measurements[i] - _reference[i]) : "—"));
    }
    private Grid Row(bool header, params string[] values)
    {
        var grid = new Grid { HorizontalAlignment = HorizontalAlignment.Stretch };
        for (var i = 0; i < values.Length; i++)
        {
            grid.ColumnDefinitions.Add(new() { Width = i == 0 ? new GridLength(64) : new GridLength(1, GridUnitType.Star) });
            var text = new TextBlock { Text = values[i], FontSize = 15, FontWeight = header ? Microsoft.UI.Text.FontWeights.SemiBold : Microsoft.UI.Text.FontWeights.Normal,
                HorizontalAlignment = i == 0 ? HorizontalAlignment.Center : HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Center };
            var cell = new Border { Child = text, Padding = new Thickness(8, 6, 8, 6), BorderBrush = _line, BorderThickness = new Thickness(0, 0, i == values.Length - 1 ? 0 : 1, 1) };
            Grid.SetColumn(cell, i); grid.Children.Add(cell);
        }
        return grid;
    }
}
