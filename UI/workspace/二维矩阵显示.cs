using System.Runtime.InteropServices.WindowsRuntime;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using Tomography.Business.Interfaces;
using Tomography.Business.Models;
using Tomography.Business.Services;

namespace Tomography.UI.workspace;

public sealed partial class ReconstructionImageView : UserControl, IReconstructionImageSink
{
    public ReconstructionImageView() => InitializeComponent();
    public void SetImage(ReconstructionImage matrix)
    {
        var pixels = MatrixPixelConverter.ToBgra(matrix);
        var bitmap = new WriteableBitmap(matrix.Width, matrix.Height);
        using (var stream = bitmap.PixelBuffer.AsStream()) stream.Write(pixels);
        bitmap.Invalidate();
        ResultImage.Source = bitmap;
        Placeholder.Visibility = Visibility.Collapsed;
    }
    public void PreviewFile(string path)
    {
        ResultImage.Source = new BitmapImage(new Uri(path));
        Placeholder.Visibility = Visibility.Collapsed;
    }
}
