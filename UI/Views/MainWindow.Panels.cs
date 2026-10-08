using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Tomography.TitleBar;

namespace Tomography.UI.Views;

public sealed partial class MainWindow
{
    private double _zoom = 1;
    private void HandleViewAction(TitleBarAction action)
    {
        switch (action)
        {
            case TitleBarAction.ToggleTools: Toggle(ToolsPanel); break;
            case TitleBarAction.ToggleBottom:
                ToggleBottomPanel(this, new RoutedEventArgs());
                break;
            case TitleBarAction.ToggleProject:
                Toggle(ProjectPanel);
                ProjectColumn.Width = new GridLength(ProjectPanel.Visibility == Visibility.Visible ? 240 : 0);
                break;
            case TitleBarAction.ToggleSidebar:
                Navigation.PaneDisplayMode = Navigation.PaneDisplayMode == NavigationViewPaneDisplayMode.LeftCompact
                    ? NavigationViewPaneDisplayMode.LeftMinimal : NavigationViewPaneDisplayMode.LeftCompact;
                Navigation.IsPaneOpen = false;
                Toggle(AccountButton);
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

    private void BottomTabChanged(SelectorBar sender, SelectorBarSelectionChangedEventArgs args)
    {
        if (BottomPanel is not null) ShowBottomPanel(sender.SelectedItem == AITab);
    }

    private void ShowBottomPanel(bool ai)
    {
        BottomTabs.SelectedItem = ai ? AITab : CommandTab;
        CommandPanel.Visibility = ai ? Visibility.Collapsed : Visibility.Visible;
        ChatPanel.Visibility = ai ? Visibility.Visible : Visibility.Collapsed;
        BottomPanel.Visibility = Visibility.Visible;
        CommandRow.Height = new GridLength(220);
    }

    private void ToggleBottomPanel(object sender, RoutedEventArgs e)
    {
        if (BottomPanel.Visibility == Visibility.Collapsed) ShowBottomPanel(BottomTabs.SelectedItem == AITab);
        else
        {
            BottomPanel.Visibility = Visibility.Collapsed;
            CommandRow.Height = new GridLength(0);
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
