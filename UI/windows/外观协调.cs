using Microsoft.UI.Xaml;
using Tomography.Business.Models;
using Tomography.Business.Services;
using Tomography.UI.Controls;

namespace Tomography.UI.windows;

public sealed partial class AppWindow
{
    private AppearanceController? _appearanceController;
    private void InitializeAppearance()
    {
        _globalSettings.Appearance ??= new AppearanceSettings { Mode = _globalSettings.Theme,
            Shared = new AppearancePalette { Background = _globalSettings.Theme == "Light" ? "#F4F5F7" : "#181818", Foreground = _globalSettings.Theme == "Light" ? "#202020" : "#FFFFFF" } };
        try { AppearanceStore.Validate(_globalSettings.Appearance); } catch { _globalSettings.Appearance = new(); }
        _appearanceController = new AppearanceController(Root);
        _appearanceController.MotionChanged += (_, _) => AiChatView.SetReducedMotion(_appearanceController.ReduceMotion);
        AiSettingsPanel.Appearance.LoadSettings(_globalSettings.Appearance, AppWindow.Id);
        AiSettingsPanel.Appearance.Changed += (_, settings) =>
        {
            var previous = _globalSettings.Appearance; var theme = _globalSettings.Theme;
            try { _globalSettings.Appearance = settings; _globalSettings.Theme = settings.Mode; SaveGlobalSettings(); ApplyAppearance(); }
            catch { _globalSettings.Appearance = previous; _globalSettings.Theme = theme; ApplyAppearance(); throw; }
        };
        Root.ActualThemeChanged += (_, _) => ApplyAppearance();
        ApplyAppearance();
    }
    private void ApplyAppearance()
    {
        if (_appearanceController is null || _globalSettings.Appearance is null) return;
        var settings = _globalSettings.Appearance;
        Root.RequestedTheme = Enum.Parse<ElementTheme>(settings.Mode);
        _appearanceController.Apply(settings);
        AiChatView.SetReducedMotion(_appearanceController.ReduceMotion);
        SideRail.RestoreTheme(Root.ActualTheme);
        QueueWindowFrameUpdate();
    }
}
