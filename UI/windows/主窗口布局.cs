using System.ComponentModel;
using System.Runtime.InteropServices;
using Microsoft.UI;
using Microsoft.UI.Input;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Tomography.UI.TittleBar;
using Tomography.UI.windows;
using Windows.Graphics;
using Windows.System;

namespace Tomography.UI.windows;

public sealed partial class AppWindow : Window
{
    public WindowStateViewModel ViewModel { get; } = new();
    private InputNonClientPointerSource? _titleBarInput;
    private double _parameterWidth = 286;
    private bool _loaded;

    public AppWindow()
    {
        InitializeComponent();
        SystemBackdrop = new MicaBackdrop();
        var hwnd = Win32Interop.GetWindowFromWindowId(AppWindow.Id);
        var scale = GetDpiForWindow(hwnd) / 96.0;
        var display = DisplayArea.GetFromWindowId(AppWindow.Id, DisplayAreaFallback.Nearest);
        var work = display.WorkArea;
        var width = Math.Min((int)(1440 * scale), work.Width);
        var height = Math.Min((int)(900 * scale), work.Height);
        AppWindow.MoveAndResize(new RectInt32(work.X + (work.Width - width) / 2,
            work.Y + (work.Height - height) / 2, width, height), display);
        AppWindow.SetIcon(System.IO.Path.Combine(AppContext.BaseDirectory, "Assets", "AppIcon.ico"));
        if (AppWindowTitleBar.IsCustomizationSupported())
        {
            ExtendsContentIntoTitleBar = true;
            AppWindow.TitleBar.PreferredHeightOption = TitleBarHeightOption.Standard;
            AppWindow.TitleBar.IconShowOptions = IconShowOptions.HideIconAndSystemMenu;
            SetTitleBar(AppTitleBar);
            _titleBarInput = InputNonClientPointerSource.GetForWindowId(AppWindow.Id);
        }
        AppTitleBar.ActionRequested += TitleBarActionRequested;
        AppTitleBar.CommandRequested += CustomCommandRequested;
        Navigation.SelectionChanged += NavigationSelectionUpdated;
        AppTitleBar.InteractiveBoundsChanged += TitleBarBoundsChanged;
        Root.ActualThemeChanged += RootThemeChanged;
        AppWindow.Changed += AppWindowChanged;
        ViewModel.PropertyChanged += ViewModelPropertyChanged;
        Activated += WindowActivated;
        Closed += WindowClosed;
        Navigation.SelectedItem = DeviceNavigation;
        Workspace.ReconstructionRequested += WorkspaceReconstructionRequested;
        BuildParameterContents();
    }

    public static ImageSource? AvatarSource(string? uri) => string.IsNullOrWhiteSpace(uri)
        ? null : new Microsoft.UI.Xaml.Media.Imaging.BitmapImage(new Uri(uri));

    private void AccountClicked(object sender, RoutedEventArgs e) => ViewModel.SelectSection("登录");

    public static Visibility BoolToVisibility(bool value) => value ? Visibility.Visible : Visibility.Collapsed;

    private void RootLoaded(object sender, RoutedEventArgs e)
    {
        _loaded = true;
        UpdatePresentation();
        UpdateTitleBar();
    }

    private void RootSizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (_loaded) UpdatePresentation();
    }

    private void ViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(WindowStateViewModel.Section)) BuildParameterContents();
        UpdatePresentation();
    }

    private void UpdatePresentation()
    {
        AppTitleBar.ParametersVisible = ViewModel.ParametersVisible;
        Navigation.SelectedItem = ViewModel.ParametersVisible && Navigation.PaneDisplayMode != NavigationViewPaneDisplayMode.LeftMinimal
            ? Navigation.MenuItems.OfType<NavigationViewItem>().FirstOrDefault(item => item.Tag as string == ViewModel.Section) : null;
        if (ContentGrid.ActualWidth > 0)
            _parameterWidth = Math.Clamp(_parameterWidth, 240, Math.Max(240, Math.Min(430, ContentGrid.ActualWidth - 500)));
        ParameterColumn.Width = new GridLength(ViewModel.ParametersVisible ? _parameterWidth : 0);
        DividerColumn.Width = new GridLength(ViewModel.ParametersVisible ? 2 : 0);
        AppTitleBar.SetModeRightEdge((Navigation.PaneDisplayMode == NavigationViewPaneDisplayMode.LeftMinimal ? 0 : 42) + 6 + _parameterWidth);
    }

    private void TitleBarBoundsChanged(object? sender, EventArgs e) => UpdateTitleBar();
    private void RootThemeChanged(FrameworkElement sender, object args) => UpdateTitleBar();
    private void AppWindowChanged(Microsoft.UI.Windowing.AppWindow sender, AppWindowChangedEventArgs args) => UpdateTitleBar();

    private void UpdateTitleBar()
    {
        if (_titleBarInput == null || AppTitleBar.XamlRoot == null) return;
        var scale = AppTitleBar.XamlRoot.RasterizationScale;
        AppTitleBar.SetInsets(AppWindow.TitleBar.LeftInset / scale, AppWindow.TitleBar.RightInset / scale);
        _titleBarInput.SetRegionRects(NonClientRegionKind.Passthrough, AppTitleBar.GetInteractiveBounds());
        AppWindow.TitleBar.ButtonBackgroundColor = AppWindow.TitleBar.ButtonInactiveBackgroundColor = Colors.Transparent;
        AppWindow.TitleBar.ButtonForegroundColor = Root.ActualTheme == ElementTheme.Dark ? Colors.White : Colors.Black;
    }

    private void WindowActivated(object sender, WindowActivatedEventArgs args) =>
        AppTitleBar.SetActive(args.WindowActivationState != WindowActivationState.Deactivated);

    private void WindowClosed(object sender, WindowEventArgs args)
    {
        _acquisitionStop?.Cancel();
        Workspace.ReconstructionRequested -= WorkspaceReconstructionRequested;
        _connectionLifetime.Cancel();
        _serialConnection.Dispose();
        ViewModel.PropertyChanged -= ViewModelPropertyChanged;
        AppTitleBar.ActionRequested -= TitleBarActionRequested;
        AppTitleBar.CommandRequested -= CustomCommandRequested;
        Navigation.SelectionChanged -= NavigationSelectionUpdated;
        AppTitleBar.InteractiveBoundsChanged -= TitleBarBoundsChanged;
        Root.ActualThemeChanged -= RootThemeChanged;
        AppWindow.Changed -= AppWindowChanged;
        Activated -= WindowActivated;
        Closed -= WindowClosed;
    }

    [DllImport("user32.dll")]
    private static extern uint GetDpiForWindow(nint hwnd);
}
