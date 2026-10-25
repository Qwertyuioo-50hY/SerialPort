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
    private readonly LineSeries<ObservablePoint> _reference = new() { Name = "空场", GeometrySize = 0, Fill = null, LineSmoothness = 0.45, Stroke = new SolidColorPaint(SKColors.DodgerBlue, 2) };
    private readonly LineSeries<ObservablePoint> _measurements = new() { Name = "物场", GeometrySize = 0, Fill = null, LineSmoothness = 0.45, Stroke = new SolidColorPaint(SKColors.OrangeRed, 2) };
    private readonly Axis _vertical = new() { TextSize = 11, SeparatorsPaint = null, Labeler = value => value.ToString("0.#########", System.Globalization.CultureInfo.InvariantCulture) };
    public ChannelWaveformView()
    {
        InitializeComponent();
        Chart.Series = new ISeries[] { _reference, _measurements };
        Chart.XAxes = new[] { new Axis { MinLimit = 1, MaxLimit = 64, MinStep = 1, TextSize = 11, SeparatorsPaint = null } };
        Chart.AddHandler(PointerWheelChangedEvent, new Microsoft.UI.Xaml.Input.PointerEventHandler((_, args) => args.Handled = true), true);
        Chart.ZoomMode = LiveChartsCore.Measure.ZoomAndPanMode.Y;
        Chart.YAxes = new[] { _vertical };
        Chart.DrawMargin = new LiveChartsCore.Measure.Margin(60, 32, 40, 28);
        Chart.LegendTextSize = 12; Chart.AnimationsSpeed = TimeSpan.Zero;
    }
    public void ShowChannels(uint[] channels) => ShowMeasurements(channels.Select(value => (double)value).ToArray());
    public void ShowMeasurements(double[] channels) => SetValues(_measurements, channels);
    public void ShowReference(double[] channels) => SetValues(_reference, channels);
    private void SetValues(LineSeries<ObservablePoint> series, double[] values)
    {
        if (values.Length != 64 || values.Any(value => !double.IsFinite(value))) throw new ArgumentException("必须提供 64 个有限通道值。", nameof(values));
        series.Values = values.Select((value, index) => new ObservablePoint(index + 1, value / 1_000_000d)).ToArray();
    }
    private void WaveformSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (Chart is null) return;
        Chart.Series = WaveformSelection.SelectedIndex switch
        {
            1 => new ISeries[] { _reference }, 2 => new ISeries[] { _measurements }, _ => new ISeries[] { _reference, _measurements }
        };
        ReferenceLegend.Visibility = WaveformSelection.SelectedIndex == 2 ? Visibility.Collapsed : Visibility.Visible;
        MeasurementLegend.Visibility = WaveformSelection.SelectedIndex == 1 ? Visibility.Collapsed : Visibility.Visible;
        ResetZoomClicked(this, new RoutedEventArgs());
    }
    private void ZoomInClicked(object sender, RoutedEventArgs e) => Zoom(0.5);
    private void ZoomOutClicked(object sender, RoutedEventArgs e) => Zoom(2);
    private void ResetZoomClicked(object sender, RoutedEventArgs e) { _vertical.MinLimit = null; _vertical.MaxLimit = null; }
    private void Zoom(double factor)
    {
        var low = _vertical.MinLimit ?? _vertical.VisibleDataBounds.Min;
        var high = _vertical.MaxLimit ?? _vertical.VisibleDataBounds.Max;
        if (!double.IsFinite(low) || !double.IsFinite(high) || high <= low) return;
        var center = (low + high) / 2; var half = Math.Max(1e-9, (high - low) * factor / 2);
        _vertical.MinLimit = center - half; _vertical.MaxLimit = center + half;
    }
}
