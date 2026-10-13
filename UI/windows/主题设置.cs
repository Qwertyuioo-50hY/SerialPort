using Microsoft.UI.Xaml;

namespace Tomography.UI.windows;

public sealed partial class AppWindow
{
    private void SideRailThemeRequested(object? sender, ElementTheme theme) => Root.RequestedTheme = theme;
}
