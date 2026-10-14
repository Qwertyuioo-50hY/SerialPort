using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using SkiaSharp;

namespace Tomography.UI.workspace;

public sealed partial class ChannelWaveformView : UserControl
{
    private readonly LineSeries<ObservablePoint> _reference = new() { Name = "空场", GeometrySize = 0, Fill = null, Stroke = new SolidColorPaint(SKColors.DodgerBlue, 2) };
    private readonly LineSeries<ObservablePoint> _measurements = new() { Name = "物场", GeometrySize = 0, Fill = null, Stroke = new SolidColorPaint(SKColors.OrangeRed, 2) };

    public ChannelWaveformView()
    {
        InitializeComponent();
        Chart.Series = new ISeries[] { _reference, _measurements };
        Chart.XAxes = new[] { new Axis { Name = "通道", MinLimit = 1, MaxLimit = 64, MinStep = 1 } };
        Chart.YAxes = new[] { new Axis { Name = "幅值" } };
        Chart.AnimationsSpeed = TimeSpan.Zero;
    }

    public void ShowChannels(uint[] channels) => ShowMeasurements(channels.Select(value => (double)value).ToArray());
    public void ShowMeasurements(double[] channels) => SetValues(_measurements, channels);
    public void ShowReference(double[] channels) => SetValues(_reference, channels);

    private void SetValues(LineSeries<ObservablePoint> series, double[] values)
    {
        if (values.Length != 64 || values.Any(value => !double.IsFinite(value))) throw new ArgumentException("必须提供 64 个有限通道值。", nameof(values));
        series.Values = values.Select((value, index) => new ObservablePoint(index + 1, value)).ToArray();
        EmptyMessage.Visibility = Visibility.Collapsed;
    }
}
