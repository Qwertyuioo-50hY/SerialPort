using Tomography.Business.Models;

namespace Tomography.UI.workspace;

public sealed partial class ImagingWorkspaceView
{
    public event EventHandler? ReconstructionRequested;
    public event EventHandler? ReconstructionFrameRateChanged;
    public double ReconstructionFramesPerSecond => double.IsFinite(ReconstructionFps.Value) ? Math.Clamp(ReconstructionFps.Value, 1, 60) : 5;
    private void ReconstructionFpsChanged(Microsoft.UI.Xaml.Controls.NumberBox sender, Microsoft.UI.Xaml.Controls.NumberBoxValueChangedEventArgs args) => ReconstructionFrameRateChanged?.Invoke(this, EventArgs.Empty);
    public void SetReconstruction(ReconstructionImage matrix, bool circular = true) => ReconstructionImage.SetImage(matrix, circular);
    public void PreviewImage(string path) => ReconstructionImage.PreviewFile(path);
    private void ReconstructionClicked(object sender, Microsoft.UI.Xaml.RoutedEventArgs e) => ReconstructionRequested?.Invoke(this, EventArgs.Empty);
}
