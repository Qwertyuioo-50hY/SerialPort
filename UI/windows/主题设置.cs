using Microsoft.UI.Xaml;
using Microsoft.UI;
using Microsoft.UI.Xaml.Media;

namespace Tomography.UI.windows;

public sealed partial class AppWindow
{
    private void UpdateWindowFrame()
    {
        var hwnd = Win32Interop.GetWindowFromWindowId(AppWindow.Id);
        var dark = Root.ActualTheme == ElementTheme.Dark ? 1u : 0u;
        DwmSetWindowAttribute(hwnd, 20, ref dark, sizeof(uint)); // DWMWA_USE_IMMERSIVE_DARK_MODE
        if (!OperatingSystem.IsWindowsVersionAtLeast(10, 0, 22000)) return;
        if (Root.Background is SolidColorBrush background)
        {
            var color = background.Color;
            var captionColor = (uint)(color.R | color.G << 8 | color.B << 16);
            DwmSetWindowAttribute(hwnd, 35, ref captionColor, sizeof(uint)); // DWMWA_CAPTION_COLOR
        }
        var borderColor = 0xFFFFFFFEu; // DWMWA_COLOR_NONE
        DwmSetWindowAttribute(hwnd, 34, ref borderColor, sizeof(uint)); // DWMWA_BORDER_COLOR
    }

    private void SideRailThemeRequested(object? sender, ElementTheme theme)
    {
        var previous = _globalSettings.Theme;
        try
        {
            _globalSettings.Theme = theme.ToString();
            SaveGlobalSettings();
            Root.RequestedTheme = theme;
        }
        catch (Exception) { _globalSettings.Theme = previous; AiSettingsPanel.SetStatus("主题保存失败。"); }
    }
}
