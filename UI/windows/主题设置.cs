using Microsoft.UI.Xaml;
using Microsoft.UI;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Dispatching;
using System.Runtime.InteropServices;

namespace Tomography.UI.windows;

public sealed partial class AppWindow
{
    private bool _windowFrameUpdatePending;
    private void QueueWindowFrameUpdate()
    {
        if (_windowFrameUpdatePending) return;
        _windowFrameUpdatePending = DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, () =>
        {
            _windowFrameUpdatePending = false;
            if (_loaded) UpdateWindowFrame();
        });
    }

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
        var borderColor = Root.Background is SolidColorBrush brush ? (uint)(brush.Color.R | brush.Color.G << 8 | brush.Color.B << 16) : 0u; // DWMWA_COLOR_NONE
        DwmSetWindowAttribute(hwnd, 34, ref borderColor, sizeof(uint)); // DWMWA_BORDER_COLOR
        // Repaint the cached non-client pixels without requiring a window move.
        const uint RDW_INVALIDATE = 0x0001, RDW_UPDATENOW = 0x0100, RDW_FRAME = 0x0400;
        RedrawWindow(hwnd, 0, 0, RDW_INVALIDATE | RDW_UPDATENOW | RDW_FRAME);
    }

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool RedrawWindow(nint hwnd, nint updateRect, nint updateRegion, uint flags);

    private void SideRailThemeRequested(object? sender, ElementTheme theme)
    {
        var previous = _globalSettings.Theme;
        var previousAppearance = _globalSettings.Appearance is null ? null : Tomography.Business.Services.AppearanceStore.Copy(_globalSettings.Appearance);
        try
        {
            _globalSettings.Theme = theme.ToString();
            if (_globalSettings.Appearance is not null)
            {
                _globalSettings.Appearance.Mode = theme.ToString();
                if (!_globalSettings.Appearance.SeparateModes && _globalSettings.Appearance.Shared.Name != "自定义")
                {
                    _globalSettings.Appearance.Shared.Background = theme == ElementTheme.Dark ? "#181818" : "#F4F5F7";
                    _globalSettings.Appearance.Shared.Foreground = theme == ElementTheme.Dark ? "#FFFFFF" : "#202020";
                }
            }
            SaveGlobalSettings();
            Root.RequestedTheme = theme;
            ApplyAppearance();
            if (_globalSettings.Appearance is not null) AiSettingsPanel.Appearance.LoadSettings(_globalSettings.Appearance, AppWindow.Id);
        }
        catch (Exception) { _globalSettings.Theme = previous; _globalSettings.Appearance = previousAppearance; ApplyAppearance(); AiSettingsPanel.SetStatus("主题保存失败。"); }
    }
}
