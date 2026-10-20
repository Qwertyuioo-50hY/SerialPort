using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Tomography.UI.SideView;

public sealed partial class IconRailView
{
    public event EventHandler<ElementTheme>? ThemeRequested;
    private bool _restoringTheme;
    public void RestoreTheme(ElementTheme theme)
    {
        SetPopupTheme(theme);
        _restoringTheme = true;
        try { ThemeSwitch.IsOn = theme == ElementTheme.Dark; }
        finally { _restoringTheme = false; }
    }

    private void SetPopupTheme(ElementTheme theme)
    {
        AccountSurface.RequestedTheme = theme;
        AccountSurface.Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(theme == ElementTheme.Light ? Microsoft.UI.Colors.White : Microsoft.UI.ColorHelper.FromArgb(255,29,29,29));
        MainLanguageMenu.Theme = theme;
    }

    private void ToggleTheme(object sender, RoutedEventArgs e) => ThemeSwitch.IsOn = !ThemeSwitch.IsOn;

    private void ThemeToggled(object sender, RoutedEventArgs e)
    {
        var dark = ThemeSwitch.IsOn;
        SetPopupTheme(dark ? ElementTheme.Dark : ElementTheme.Light);
        ThemeIcon.Glyph = dark ? "\uE708" : "\uE706";
        if (!_restoringTheme) ThemeRequested?.Invoke(this, dark ? ElementTheme.Dark : ElementTheme.Light);
    }
}
