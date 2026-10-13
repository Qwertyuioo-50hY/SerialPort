using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Tomography.UI.TittleBar;

namespace Tomography.UI.windows;

public sealed partial class AppWindow
{
    private double _zoom = 1;
    private void HandleViewAction(TitleBarAction action)
    {
        switch (action)
        {
            case TitleBarAction.ToggleTools: Toggle(ToolsPanel); break;
            case TitleBarAction.ToggleProject:
                ToggleProjectNavigation(this, EventArgs.Empty);
                break;
            case TitleBarAction.ToggleSidebar:
                Toggle(SideRail);
                UpdatePresentation();
                break;
            case TitleBarAction.ToggleWaveform: Workspace.ToggleModule("Waveform"); break;
            case TitleBarAction.ToggleData: Workspace.ToggleModule("Data"); break;
            case TitleBarAction.ToggleReconstruction: Workspace.ToggleModule("Reconstruction"); break;
            case TitleBarAction.ToggleCamera: Workspace.ToggleModule("Camera"); break;
            case TitleBarAction.ZoomIn: SetZoom(_zoom + 0.1); break;
            case TitleBarAction.ZoomOut: SetZoom(_zoom - 0.1); break;
            case TitleBarAction.ZoomReset: SetZoom(1); break;
        }
    }

    private void SetZoom(double value)
    {
        _zoom = Math.Clamp(value, 0.5, 2);
        WorkspaceScroll.ChangeView(null, null, (float)_zoom);
    }

    private static void Toggle(FrameworkElement element) => element.Visibility =
        element.Visibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
}
