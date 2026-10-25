using System.Runtime.InteropServices.WindowsRuntime;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using Tomography.Business.Interfaces;
using Tomography.Business.Models;

namespace Tomography.UI.workspace;

public sealed partial class ReconstructionImageView : UserControl, IReconstructionImageSink
{
    public ReconstructionImageView() => InitializeComponent();
    public void SetImage(ReconstructionImage matrix) => SetImage(matrix, true);
    public void SetImage(ReconstructionImage matrix, bool circular)
    {
        const int size = 512;
        var pixels = ReconstructionRasterizer.Render(matrix, circular, size);
        var bitmap = new WriteableBitmap(size, size);
        using (var stream = bitmap.PixelBuffer.AsStream()) stream.Write(pixels);
        bitmap.Invalidate(); ResultImage.Source = bitmap;
        ResultImage.Visibility = ColorScale.Visibility = Visibility.Visible;
        Placeholder.Visibility = Visibility.Collapsed;
    }
    public void PreviewFile(string path)
    {
        ResultImage.Source = new BitmapImage(new Uri(path));
        ResultImage.Visibility = Visibility.Visible;
        Placeholder.Visibility = ColorScale.Visibility = Visibility.Collapsed;
    }
}
