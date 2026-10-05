using Tomography.Business.Models;

namespace Tomography.UI.Controls;

public sealed partial class WorkspaceView
{
    public event EventHandler? ReconstructionRequested;
    public void SetReconstruction(ReconstructionImage matrix) => ReconstructionImage.SetImage(matrix);
    public void PreviewImage(string path) => ReconstructionImage.PreviewFile(path);
    private void ReconstructionClicked(object sender, Microsoft.UI.Xaml.RoutedEventArgs e) => ReconstructionRequested?.Invoke(this, EventArgs.Empty);
}
