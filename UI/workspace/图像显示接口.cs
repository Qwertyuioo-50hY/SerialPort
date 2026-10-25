using Tomography.Business.Models;

namespace Tomography.UI.workspace;

public sealed partial class ImagingWorkspaceView
{
    public event EventHandler? ReconstructionRequested;
    public void SetReconstruction(ReconstructionImage matrix, bool circular = true) => ReconstructionImage.SetImage(matrix, circular);
    public void PreviewImage(string path) => ReconstructionImage.PreviewFile(path);
    private void ReconstructionClicked(object sender, Microsoft.UI.Xaml.RoutedEventArgs e) => ReconstructionRequested?.Invoke(this, EventArgs.Empty);
}
