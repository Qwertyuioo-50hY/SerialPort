using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Tomography.UI.workspace;

public sealed partial class ImagingWorkspaceView : UserControl
{
    public ImagingWorkspaceView() => InitializeComponent();
    public UIElement Commands { get => (UIElement)CommandHost.Content; set => CommandHost.Content = value; }
    public Panel AlgorithmPanel => AlgorithmBody;
    public void ToggleModule(string name)
    {
        var module = name switch { "Waveform" => WaveformModule, "Data" => DataModule, "Reconstruction" => ReconstructionModule, _ => CameraModule };
        module.Visibility = module.Visibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
    }
}
