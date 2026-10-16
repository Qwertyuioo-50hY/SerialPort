using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Tomography.UI.SideView;

public sealed partial class IconRailView
{
    public event EventHandler<ElementTheme>? ThemeRequested;
    private bool _restoringTheme;
    public void RestoreTheme(ElementTheme theme)
    {
        _restoringTheme = true;
        try { ThemeSwitch.IsOn = theme == ElementTheme.Dark; }
        finally { _restoringTheme = false; }
    }

    private void ToggleTheme(object sender, RoutedEventArgs e) => ThemeSwitch.IsOn = !ThemeSwitch.IsOn;

    private void ThemeToggled(object sender, RoutedEventArgs e)
    {
        var dark = ThemeSwitch.IsOn;
        ThemeIcon.Glyph = dark ? "\uE708" : "\uE706";
        if (!_restoringTheme) ThemeRequested?.Invoke(this, dark ? ElementTheme.Dark : ElementTheme.Light);
    }
}
