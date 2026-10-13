using System.ComponentModel;
using System.Runtime.InteropServices;
using Microsoft.UI;
using Microsoft.UI.Input;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Tomography.UI.TittleBar;
using Windows.Graphics;

namespace Tomography.UI.windows;

public sealed partial class AppWindow : Window
{
    public WindowStateViewModel ViewModel { get; } = new();
    private InputNonClientPointerSource? _titleBarInput;
    private double _parameterWidth = 294;
    private readonly WindowTitleBarView AppTitleBar = new();
    private readonly FontIconSource ConnectionIcon = new();
    private TextBlock ConnectionStatus = new();
    private bool _loaded;

    public AppWindow()
    {
        InitializeComponent();
        SystemBackdrop = null;
        var hwnd = Win32Interop.GetWindowFromWindowId(AppWindow.Id);
        var scale = GetDpiForWindow(hwnd) / 96.0;
        var display = DisplayArea.GetFromWindowId(AppWindow.Id, DisplayAreaFallback.Nearest);
        var work = display.WorkArea;
        var width = Math.Min((int)(1440 * scale), work.Width);
        var height = Math.Min((int)(900 * scale), work.Height);
        AppWindow.MoveAndResize(new RectInt32(work.X + (work.Width - width) / 2,
            work.Y + (work.Height - height) / 2, width, height), display);
        AppWindow.SetIcon(System.IO.Path.Combine(AppContext.BaseDirectory, "Assets", "AppIcon.ico"));
        ExtendsContentIntoTitleBar = true;
        if (AppWindow.Presenter is OverlappedPresenter presenter) presenter.SetBorderAndTitleBar(false, false);
        if (OperatingSystem.IsWindowsVersionAtLeast(10, 0, 22000))
        {
            const int DWMWA_BORDER_COLOR = 34;
            var borderColor = 0xFFFFFFFEu; // DWMWA_COLOR_NONE
            DwmSetWindowAttribute(hwnd, DWMWA_BORDER_COLOR, ref borderColor, sizeof(uint));
        }
        _titleBarInput = InputNonClientPointerSource.GetForWindowId(AppWindow.Id);
        Root.ActualThemeChanged += RootThemeChanged;
        AppWindow.Changed += AppWindowChanged;
        ViewModel.PropertyChanged += ViewModelPropertyChanged;
        Closed += WindowClosed;
        Workspace.ReconstructionRequested += WorkspaceReconstructionRequested;
        ProjectPanel.WindowCommandRequested += WindowCommandRequested;
        SideRail.ThemeRequested += SideRailThemeRequested;
        InitializeDevicePage();
        BuildParameterContents();
    }

    public static ImageSource? AvatarSource(string? uri) => string.IsNullOrWhiteSpace(uri)
        ? null : new Microsoft.UI.Xaml.Media.Imaging.BitmapImage(new Uri(uri));

    public static Visibility BoolToVisibility(bool value) => value ? Visibility.Visible : Visibility.Collapsed;

    private void RootLoaded(object sender, RoutedEventArgs e)
    {
        _loaded = true;
        UpdatePresentation();
        UpdateTitleBar();
    }

    private void RootSizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (_loaded) { UpdatePresentation(); UpdateTitleBar(); }
    }

    private void ViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(WindowStateViewModel.Section)) BuildParameterContents();
        UpdatePresentation();
    }

    private void UpdatePresentation()
    {
        var device = ViewModel.Section is "设备" or "接口/协议";
        DeviceShell.Visibility = device ? Visibility.Visible : Visibility.Collapsed;
        ContentGrid.Visibility = device ? Visibility.Collapsed : Visibility.Visible;
        ShellFrame.BorderThickness = new Thickness(8);
        ShellFrame.CornerRadius = new CornerRadius(8);
        SideRail.CornerRadius = ShellFrame.CornerRadius;
        if (ContentGrid.ActualWidth > 0)
            _parameterWidth = Math.Clamp(_parameterWidth, 240, Math.Max(240, Math.Min(430, ContentGrid.ActualWidth - 760)));
        ParameterColumn.Width = new GridLength(ViewModel.ParametersVisible ? _parameterWidth : 0);
        DividerColumn.Width = new GridLength(ViewModel.ParametersVisible ? 8 : 0);
    }

    private void RootThemeChanged(FrameworkElement sender, object args) => UpdateTitleBar();
    private void AppWindowChanged(Microsoft.UI.Windowing.AppWindow sender, AppWindowChangedEventArgs args) => UpdateTitleBar();

    private void UpdateTitleBar()
    {
        if (_titleBarInput is null || Root.XamlRoot is null) return;
        var scale = Root.XamlRoot.RasterizationScale;
        _titleBarInput.SetRegionRects(NonClientRegionKind.Caption, new[]
        {
            new RectInt32((int)(64 * scale), 0, (int)(Math.Max(0, Root.ActualWidth - 220) * scale), (int)(12 * scale))
        });
    }

    private void WindowClosed(object sender, WindowEventArgs args)
    {
        _acquisitionStop?.Cancel();
        Workspace.ReconstructionRequested -= WorkspaceReconstructionRequested;
        ProjectPanel.WindowCommandRequested -= WindowCommandRequested;
        _connectionLifetime.Cancel();
        CloseDevice();
        _serialConnection.Dispose();
        _tcpConnection.Dispose();
        ViewModel.PropertyChanged -= ViewModelPropertyChanged;
        SideRail.ThemeRequested -= SideRailThemeRequested;
        Root.ActualThemeChanged -= RootThemeChanged;
        AppWindow.Changed -= AppWindowChanged;
        Closed -= WindowClosed;
    }

    private void WindowCommandRequested(object? sender, string command)
    {
        if (command == "Close") { Close(); return; }
        if (AppWindow.Presenter is not OverlappedPresenter presenter) return;
        if (command == "Minimize") presenter.Minimize();
        else if (presenter.State == OverlappedPresenterState.Maximized) presenter.Restore();
        else presenter.Maximize();
    }

    [DllImport("user32.dll")]
    private static extern uint GetDpiForWindow(nint hwnd);
    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(nint hwnd, int attribute, ref uint value, int size);
}
