using Microsoft.UI.Xaml;

namespace Tomography.UI.windows;

public sealed partial class AppWindow
{
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
