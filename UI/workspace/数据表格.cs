using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.WinUI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Tomography.UI.workspace;

public sealed class ChannelDataView : UserControl
{
    private readonly CartesianChart _heat = new() { Height = 140, AnimationsSpeed = TimeSpan.Zero };
    private readonly ListView _rows = new() { Height = 220 };
    private double[]? _reference;
    private double[]? _measurements;

    public ChannelDataView()
    {
        var panel = new StackPanel { Spacing = 12 };
        var rowStyle = new Style(typeof(ListViewItem));
        rowStyle.Setters.Add(new Setter(HorizontalContentAlignmentProperty, HorizontalAlignment.Stretch));
        _rows.ItemContainerStyle = rowStyle;
        panel.Children.Add(_heat);
        panel.Children.Add(Row("通道", "空场", "物场", "差值"));
        panel.Children.Add(_rows);
        Content = panel;
        _heat.XAxes = new[] { new Axis { IsVisible = false } };
        _heat.YAxes = new[] { new Axis { IsVisible = false } };
        ScrollViewer.SetVerticalScrollBarVisibility(_rows, ScrollBarVisibility.Hidden);
    }

    public void SetReference(double[] values) { _reference = values.ToArray(); Refresh(); }
    public void SetMeasurements(double[] values) { _measurements = values.ToArray(); Refresh(); }

    private void Refresh()
    {
        _rows.Items.Clear();
        var values = _measurements ?? _reference;
        if (values is null) return;
        for (var i = 0; i < values.Length; i++)
            _rows.Items.Add(Row((i + 1).ToString(), _reference?[i].ToString("G6") ?? "—", _measurements?[i].ToString("G6") ?? "—",
                _reference is not null && _measurements is not null ? (_measurements[i] - _reference[i]).ToString("G6") : "—"));
        _heat.Series = new ISeries[] { new HeatSeries<WeightedPoint> { Name = "通道幅值", Values = values.Select((value, i) => new WeightedPoint(i % 8, 7 - i / 8, value)).ToArray() } };
    }

    private static Grid Row(params string[] values)
    {
        var grid = new Grid { ColumnSpacing = 8, Padding = new Thickness(8, 4, 8, 4), HorizontalAlignment = HorizontalAlignment.Stretch };
        for (var i = 0; i < values.Length; i++)
        {
            grid.ColumnDefinitions.Add(new ColumnDefinition());
            var text = new TextBlock { Text = values[i], FontSize = 12 };
            Grid.SetColumn(text, i); grid.Children.Add(text);
        }
        return grid;
    }
}
