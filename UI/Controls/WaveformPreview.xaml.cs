using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;

namespace Tomography.UI.Controls;

/// <summary>显示最新一帧 64 通道值，按数据范围绘制折线。</summary>
public sealed partial class WaveformPreview : UserControl
{
    private double[]? _channels;
    public WaveformPreview() => InitializeComponent();

    public void ShowChannels(uint[] channels) => ShowMeasurements(channels.Select(value => (double)value).ToArray());

    public void ShowMeasurements(double[] channels)
    {
        if (channels.Length != 64 || channels.Any(value => !double.IsFinite(value))) throw new ArgumentException("必须提供 64 个通道值。", nameof(channels));
        _channels = channels.ToArray();
        EmptyMessage.Visibility = Visibility.Collapsed;
        Draw();
    }

    private void PlotSizeChanged(object sender, SizeChangedEventArgs e) => Draw();

    private void Draw()
    {
        if (_channels is null || PlotArea.ActualWidth <= 4 || PlotArea.ActualHeight <= 4) return;
        double minimum = _channels.Min(), maximum = _channels.Max();
        if (maximum == minimum) { minimum = Math.Max(0, minimum - 1); maximum += 1; }
        MinimumLabel.Text = minimum.ToString("0");
        MiddleLabel.Text = ((minimum + maximum) / 2).ToString("0.##");
        MaximumLabel.Text = maximum.ToString("0");
        var points = new PointCollection();
        for (var i = 0; i < _channels.Length; i++)
            points.Add(new Point(2 + i * (PlotArea.ActualWidth - 4) / 63,
                2 + (maximum - _channels[i]) / (maximum - minimum) * (PlotArea.ActualHeight - 4)));
        ChannelLine.Points = points;
    }
}
