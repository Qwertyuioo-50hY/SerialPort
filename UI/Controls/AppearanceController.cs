using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Tomography.Business.Models;
using Windows.UI;

namespace Tomography.UI.Controls;

internal sealed class AppearanceController
{
    private readonly FrameworkElement _root;
    private readonly ConditionalWeakTable<DependencyObject, OriginalText> _original = new();
    private bool _lastMotion;
    private bool _highContrast;
    private readonly DispatcherTimer _systemPreferences = new() { Interval = TimeSpan.FromSeconds(2) };
    public event EventHandler? MotionChanged;
    private AppearanceSettings _settings = new();
    private AppearancePalette _palette = new();
    private bool _refreshPending;
    private int _version;
    public bool ReduceMotion => _settings.ReduceMotion == "On" || (_settings.ReduceMotion == "Default" && !SystemAnimationsEnabled());
    public AppearanceController(FrameworkElement root)
    {
        _root = root;
        root.Loaded += (_, _) => Refresh();
        root.SizeChanged += (_, _) => Refresh();
        _systemPreferences.Tick += (_, _) =>
        {
            var motion = ReduceMotion;
            if (_lastMotion != motion) { _lastMotion = motion; MotionChanged?.Invoke(this,EventArgs.Empty); }
            if (_highContrast != HighContrast()) Apply(_settings);
        };
        root.Loaded += (_, _) => _systemPreferences.Start();
        root.Unloaded += (_, _) => _systemPreferences.Stop();
        root.PointerMoved += (_, e) =>
        {
            if (e.OriginalSource is not DependencyObject node) return;
            while (node is not null && node is not ButtonBase && node is not ComboBox) node = VisualTreeHelper.GetParent(node);
            if (_settings.PointerCursor) SetCursor(LoadCursor(0, node is null ? 32512 : 32649));
        };
    }
    public void Apply(AppearanceSettings settings)
    {
        _version++;
        _settings = settings;
        _palette = Tomography.Business.Services.AppearanceStore.Copy(!settings.SeparateModes ? settings.Shared : _root.ActualTheme == ElementTheme.Dark ? settings.Dark : settings.Light);
        if (_palette.Name is "FieldSync" or "极简" or "海蓝")
        {
            _palette.Background = _root.ActualTheme == ElementTheme.Dark ? "#181818" : "#F4F5F7";
            _palette.Foreground = _root.ActualTheme == ElementTheme.Dark ? "#FFFFFF" : "#202020";
        }
        var highContrast = _highContrast = HighContrast();
        if (!highContrast)
        {
            var background = Color(_palette.Background); var foreground = Color(_palette.Foreground); var accent = Color(_palette.Accent);
            var surface = Blend(background, foreground, 0.015 + _palette.Contrast / 3000);
            foreach (var dictionary in Application.Current.Resources.ThemeDictionaries.Values.OfType<ResourceDictionary>())
            {
                if (ReferenceEquals(dictionary, Application.Current.Resources.ThemeDictionaries["HighContrast"])) continue;
                foreach (var key in new[] { "DeviceCanvasBrush", "DeviceToolbarBrush" }) SetBrush(dictionary,key,background);
                foreach (var key in new[] { "DevicePageBrush", "WorkspaceBackgroundBrush", "ParameterBackgroundBrush" }) SetBrush(dictionary,key,surface);
                SetBrush(dictionary,"DeviceDividerBrush",Blend(background,foreground,0.18));
                SetBrush(dictionary,"RailAccentBrush",accent);
                SetBrush(dictionary,"ShellBackgroundBrush",_palette.TranslucentSidebar ? Windows.UI.Color.FromArgb(210,background.R,background.G,background.B) : background);
            }
            _root.Resources["AppearanceForeground"] = new SolidColorBrush(foreground);
            _root.Resources["SystemAccentColor"] = accent;
            foreach (var key in new[] { "AccentFillColorDefaultBrush", "AccentFillColorSecondaryBrush", "AccentTextFillColorPrimaryBrush" }) _root.Resources[key] = new SolidColorBrush(accent);
        }
        else
        {
            foreach (var key in new[] { "SystemAccentColor", "AccentFillColorDefaultBrush", "AccentFillColorSecondaryBrush", "AccentTextFillColorPrimaryBrush" }) _root.Resources.Remove(key);
        }
        ApplyTree(_root, highContrast);
    }
    // Never change typography from LayoutUpdated: template recreation can keep
    // scheduling new layouts and starve input on large settings pages.
    public void Refresh()
    {
        if (_refreshPending) return;
        _refreshPending = _root.DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, () =>
        {
            _refreshPending = false;
            ApplyTree(_root, _highContrast);
        });
    }
    private void ApplyTree(DependencyObject node, bool highContrast) => Visit(node, highContrast);
    private void Visit(DependencyObject node, bool highContrast)
    {
        if (node is IconElement) return;
        var original = _original.GetValue(node, _ => new OriginalText(node is TextBlock tb ? tb.FontSize : node is Control c ? c.FontSize : 14, node is TextBlock ft ? ft.FontFamily.Source : node is Control fc ? fc.FontFamily.Source : ""));
        var update = original.Version != _version;
        original.Version = _version;
        if (update && node is TextBlock text && !original.Symbol)
        {
            var old = original;
            // FontIcon is a separate control; preserve its symbol font.
            text.FontFamily = new FontFamily(original.Code ? _palette.CodeFont : IsContent(text) && !string.IsNullOrEmpty(_palette.ContentFont) ? _palette.ContentFont : _palette.Font);
            text.FontSize = original.Code ? _palette.CodeSize : Math.Round(old.Size * _palette.FontSize / 14,2);
            if (!highContrast && !IsRail(text)) text.Foreground = new SolidColorBrush(Color(_palette.Foreground));
            else if (highContrast) text.ClearValue(TextBlock.ForegroundProperty);
            text.FontWeight = Weight(original.Code ? _palette.CodeStyle : IsContent(text) ? _palette.ContentStyle : _palette.FontStyle);
        }
        else if (update && node is Control control && node is not FontIcon && node is not SymbolIcon)
        {
            control.FontFamily = new FontFamily(original.Code ? _palette.CodeFont : _palette.Font);
            control.FontSize = original.Code ? _palette.CodeSize : _palette.FontSize;
            control.FontWeight = Weight(original.Code ? _palette.CodeStyle : _palette.FontStyle);
            if (!highContrast && control is TextBox or ComboBox or NumberBox or PasswordBox) control.Foreground = new SolidColorBrush(Color(_palette.Foreground));
            else if (highContrast) control.ClearValue(Control.ForegroundProperty);
        }
        // Let editable controls and buttons style their own template children.
        // Rewriting those children breaks inherited fonts and template states.
        if (node is TextBox or PasswordBox or NumberBox or ComboBox or AutoSuggestBox or ButtonBase) return;
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(node); i++) Visit(VisualTreeHelper.GetChild(node,i),highContrast);
    }
    private static bool IsRail(DependencyObject node)
    {
        while ((node = VisualTreeHelper.GetParent(node)) is not null) if (node is Tomography.UI.SideView.IconRailView) return true;
        return false;
    }
    private static bool IsContent(DependencyObject node)
    {
        while ((node = VisualTreeHelper.GetParent(node)) is not null) if (node is Tomography.UI.Bottom.AiChatPanelView) return true;
        return false;
    }
    private static Windows.UI.Text.FontWeight Weight(string value) => value == "粗体" ? Microsoft.UI.Text.FontWeights.Bold : value == "中等" ? Microsoft.UI.Text.FontWeights.SemiBold : Microsoft.UI.Text.FontWeights.Normal;
    private sealed class OriginalText(double size, string family) { public double Size = size; public bool Symbol = family.Contains("Icons") || family.Contains("Symbol") || family.Contains("MDL2"); public bool Code = family.Contains("Mono") || family.Contains("Consolas"); public int Version = -1; }
    private static Color Color(string hex) => (Color)Microsoft.UI.Xaml.Markup.XamlBindingHelper.ConvertValue(typeof(Color),hex);
    private static Color Blend(Color a, Color b, double amount) => Windows.UI.Color.FromArgb(255,(byte)(a.R+(b.R-a.R)*amount),(byte)(a.G+(b.G-a.G)*amount),(byte)(a.B+(b.B-a.B)*amount));
    private static void SetBrush(ResourceDictionary dictionary,string key, Color color) { if (dictionary.TryGetValue(key,out var value) && value is SolidColorBrush brush) brush.Color = color; }
    private static bool SystemAnimationsEnabled() { return !SystemParametersInfo(0x1042,0,out var enabled,0) || enabled; }
    private static bool HighContrast()
    {
        var data = new HighContrastInfo { Size = (uint)Marshal.SizeOf<HighContrastInfo>() };
        return SystemParametersInfoHighContrast(0x42,data.Size,ref data,0) && (data.Flags & 1) != 0;
    }
    [StructLayout(LayoutKind.Sequential)] private struct HighContrastInfo { public uint Size, Flags; public nint Scheme; }
    [DllImport("user32.dll", EntryPoint = "SystemParametersInfoW")] private static extern bool SystemParametersInfo(uint action,uint parameter,[MarshalAs(UnmanagedType.Bool)] out bool value,uint flags);
    [DllImport("user32.dll", EntryPoint = "SystemParametersInfoW")] private static extern bool SystemParametersInfoHighContrast(uint action,uint parameter,ref HighContrastInfo value,uint flags);
    [DllImport("user32.dll")] private static extern nint LoadCursor(nint instance, int name);
    [DllImport("user32.dll")] private static extern nint SetCursor(nint cursor);
}
