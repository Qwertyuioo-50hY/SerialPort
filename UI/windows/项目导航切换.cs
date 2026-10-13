using Microsoft.UI.Xaml;

namespace Tomography.UI.windows;

public sealed partial class AppWindow
{
    private void ToggleProjectNavigation(object? sender, EventArgs e)
    {
        var visible = DirectoryFrame.Visibility != Visibility.Visible;
        DirectoryFrame.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;
        DirectoryColumn.Width = new GridLength(visible ? 260 : 0);
    }
}
