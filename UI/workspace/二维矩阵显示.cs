using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.SkiaSharpView;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using Tomography.Business.Interfaces;
using Tomography.Business.Models;

namespace Tomography.UI.workspace;

public sealed partial class ReconstructionImageView : UserControl, IReconstructionImageSink
{
    public ReconstructionImageView()
    {
        InitializeComponent();
        HeatChart.XAxes = new[] { new Axis { IsVisible = false } };
        HeatChart.YAxes = new[] { new Axis { IsVisible = false } };
        HeatChart.AnimationsSpeed = TimeSpan.Zero;
        HeatChart.MatchAxesScreenDataRatio = true;
    }

    public void SetImage(ReconstructionImage matrix)
    {
        matrix.Validate();
        var points = new WeightedPoint[matrix.Values.Length];
        for (var i = 0; i < points.Length; i++) points[i] = new WeightedPoint(i % matrix.Width, matrix.Height - 1 - i / matrix.Width, matrix.Values[i]);
        HeatChart.Series = new ISeries[] { new HeatSeries<WeightedPoint> { Name = "二维重建", Values = points } };
        HeatChart.Visibility = Visibility.Visible;
        ResultImage.Visibility = Placeholder.Visibility = Visibility.Collapsed;
    }

    public void PreviewFile(string path)
    {
        ResultImage.Source = new BitmapImage(new Uri(path));
        ResultImage.Visibility = Visibility.Visible;
        HeatChart.Visibility = Placeholder.Visibility = Visibility.Collapsed;
    }
}
