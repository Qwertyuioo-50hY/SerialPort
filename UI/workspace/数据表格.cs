using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Tomography.UI.workspace;

public sealed class ChannelDataView : UserControl
{
    private readonly ListView _rows = new() { Height = 360 };
    private double[]? _reference;
    private double[]? _measurements;

    public ChannelDataView()
    {
        var panel = new StackPanel { Spacing = 12 };
        var rowStyle = new Style(typeof(ListViewItem));
        rowStyle.Setters.Add(new Setter(HorizontalContentAlignmentProperty, HorizontalAlignment.Stretch));
        _rows.ItemContainerStyle = rowStyle;
        panel.Children.Add(Row("通道", "空场 (V)", "物场 (V)", "差值 (V)"));
        panel.Children.Add(_rows);
        Content = panel;
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
            _rows.Items.Add(Row((i + 1).ToString(), (_reference?[i] / 1_000_000d)?.ToString("0.#########") ?? "—", (_measurements?[i] / 1_000_000d)?.ToString("0.#########") ?? "—",
                _reference is not null && _measurements is not null ? ((_measurements[i] - _reference[i]) / 1_000_000d).ToString("0.#########") : "—"));
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
