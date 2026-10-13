using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls.Primitives;

namespace Tomography.UI.windows;

public sealed partial class AppWindow
{
    private double _directoryWidth = 260;

    private void ToggleProjectNavigation(object? sender, EventArgs e)
    {
        var visible = DirectoryFrame.Visibility != Visibility.Visible;
        DirectoryFrame.Visibility = DirectoryDivider.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;
        DirectoryColumn.Width = new GridLength(visible ? _directoryWidth : 0);
        DirectoryDividerColumn.Width = new GridLength(visible ? 8 : 0);
    }

    private void DirectoryDragDelta(object sender, DragDeltaEventArgs e)
    {
        var maximum = Math.Max(180, Math.Min(430, Root.ActualWidth - 68 - 8 - 650));
        _directoryWidth = Math.Clamp(DirectoryColumn.ActualWidth + e.HorizontalChange, 180, maximum);
        DirectoryColumn.Width = new GridLength(_directoryWidth);
    }
}
