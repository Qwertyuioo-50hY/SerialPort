using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;

namespace Tomography.UI.workspace;

public sealed partial class ImagingWorkspaceView : UserControl
{
    public ImagingWorkspaceView() => InitializeComponent();

    public UIElement Commands { get => (UIElement)CommandHost.Content; set => CommandHost.Content = value; }

    public void ToggleModule(string name)
    {
        var module = name switch { "Waveform" => WaveformModule, "Data" => DataModule,
            "Reconstruction" => ReconstructionModule, _ => CameraModule };
        module.Visibility = module.Visibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
        ArrangeModules();
    }

    private void ArrangeModules()
    {
        var left = new[] { WaveformModule, DataModule }.Where(m => m.Visibility == Visibility.Visible).ToArray();
        var right = new[] { ReconstructionModule, CameraModule }.Where(m => m.Visibility == Visibility.Visible).ToArray();
        foreach (var pair in new[] { left, right })
            for (var i = 0; i < pair.Length; i++)
            {
                Grid.SetRow(pair[i], pair.Length == 1 ? 0 : i * 2);
                Grid.SetRowSpan(pair[i], pair.Length == 1 ? 3 : 1);
                Grid.SetColumn(pair[i], left.Length == 0 || right.Length == 0 || ReferenceEquals(pair, left) ? 0 : 2);
                Grid.SetColumnSpan(pair[i], left.Length == 0 || right.Length == 0 ? 3 : 1);
            }
        ColumnDivider.Visibility = left.Length > 0 && right.Length > 0 ? Visibility.Visible : Visibility.Collapsed;
        RowDivider.Visibility = left.Length == 2 || right.Length == 2 ? Visibility.Visible : Visibility.Collapsed;
    }

    private void ColumnDragDelta(object sender, DragDeltaEventArgs e)
    {
        var width = ModuleGrid.ActualWidth - 8;
        if (width < 400) return;
        var left = Math.Clamp(LeftModules.ActualWidth + e.HorizontalChange, 200, width - 200);
        LeftModules.Width = new GridLength(left, GridUnitType.Star);
        RightModules.Width = new GridLength(width - left, GridUnitType.Star);
    }

    private void RowDragDelta(object sender, DragDeltaEventArgs e)
    {
        var height = ModuleGrid.ActualHeight - 8;
        if (height < 240) return;
        var top = Math.Clamp(TopModules.ActualHeight + e.VerticalChange, 120, height - 120);
        TopModules.Height = new GridLength(top, GridUnitType.Star);
        BottomModules.Height = new GridLength(height - top, GridUnitType.Star);
    }

    private void WaveformModeClicked(object sender, RoutedEventArgs e)
    {
        foreach (ToggleButton button in WaveformModes.Children)
            button.IsChecked = ReferenceEquals(button, sender);
    }
}
