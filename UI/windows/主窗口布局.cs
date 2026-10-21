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
using Tomography.Business.Services.Authentication;

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
    private readonly Tomography.UI.Account.BorderlessLoginFrame _frame;
    private TrayIcon? _tray;
    private bool _exiting;
    public void CloseForAccountChange() { _exiting = true; Close(); }
    private WindowId PickerWindowId => AppWindow.Id;

    public AppWindow(AuthenticationService authentication)
    {
        var user = authentication.CurrentUser ?? throw new InvalidOperationException("请先登录。");
        InitializeComponent();
        ViewModel.AccountName = user.Username;
        SideRail.SetAccount(user.Username);
        SideRail.AccountActionRequested += (_, switchAccount) =>
        {
            if (CanChangeProject()) SignOutRequested?.Invoke(this, switchAccount);
        };
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
        AppWindow.TitleBar.PreferredHeightOption = TitleBarHeightOption.Collapsed;
        if (AppWindow.Presenter is OverlappedPresenter presenter)
        {
            presenter.SetBorderAndTitleBar(false, false); presenter.IsResizable = true;
        }
        if (OperatingSystem.IsWindowsVersionAtLeast(10, 0, 22000))
        {
            var cornerPreference = 2u;
            DwmSetWindowAttribute(hwnd, 33, ref cornerPreference, sizeof(uint));
        }
        _frame = new Tomography.UI.Account.BorderlessLoginFrame(hwnd, true);
        _tray = new TrayIcon(hwnd, () => { AppWindow.Show(); if (AppWindow.Presenter is OverlappedPresenter p && p.State == OverlappedPresenterState.Minimized) p.Restore(); Activate(); }, () => { _exiting = true; Close(); });
        AppWindow.Closing += (_, e) => { if (!_exiting) { e.Cancel = true; AppWindow.Hide(); } };
        _titleBarInput = InputNonClientPointerSource.GetForWindowId(AppWindow.Id);
        Root.ActualThemeChanged += RootThemeChanged;
        Activated += WindowActivated;
        AppWindow.Changed += AppWindowChanged;
        ViewModel.PropertyChanged += ViewModelPropertyChanged;
        Closed += WindowClosed;
        Workspace.ReconstructionRequested += WorkspaceReconstructionRequested;
        SideRail.ThemeRequested += SideRailThemeRequested;
        InitializeDevicePage();
        InitializeAi();
        InitializeHome();
        InitializeInterfacePreferences();
        InitializeProfile(user);
        SideRail.SetSection(ViewModel.Section);
        AddReconstructionSettings(Workspace.AlgorithmPanel);
        _reconstructionSettings!.CustomAlgorithmRequested += async (_, _) => await ImportFileAsync("自定义算法");
        BuildParameterContents();
    }

    public event EventHandler<bool>? SignOutRequested;
    public void SetAccountBusy(bool busy) => Root.IsHitTestVisible = !busy;
    public void ShowAccountError(string message)
    {
        ViewModel.Section = "主页"; SideRail.SetSection("主页"); HomePanel.SetStatus(message);
    }

    public static ImageSource? AvatarSource(string? uri) => string.IsNullOrWhiteSpace(uri)
        ? null : new Microsoft.UI.Xaml.Media.Imaging.BitmapImage(new Uri(uri));

    public static Visibility BoolToVisibility(bool value) => value ? Visibility.Visible : Visibility.Collapsed;

    private void RootLoaded(object sender, RoutedEventArgs e)
    {
        _loaded = true;
        UpdatePresentation();
        QueueWindowFrameUpdate();
        UpdateTitleBar();
        ApplyInterfaceLanguage();
    }

    private void RootSizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (_loaded) { UpdatePresentation(); UpdateTitleBar(); QueueWindowFrameUpdate(); }
    }

    private void ViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(WindowStateViewModel.Section)) BuildParameterContents();
        UpdatePresentation();
        DispatcherQueue.TryEnqueue(ApplyInterfaceLanguage);
    }

    private void UpdatePresentation()
    {
        var device = ViewModel.Section is "设备" or "接口/协议";
        DeviceShell.Visibility = device ? Visibility.Visible : Visibility.Collapsed;
        AiSettingsPanel.Visibility = ViewModel.Section == "设置" ? Visibility.Visible : Visibility.Collapsed;
        AiChatView.Visibility = ViewModel.Section == "AI" ? Visibility.Visible : Visibility.Collapsed;
        HomePanel.Visibility = ViewModel.Section == "主页" ? Visibility.Visible : Visibility.Collapsed;
        ContentGrid.Visibility = device || ViewModel.Section is "主页" or "AI" or "设置" ? Visibility.Collapsed : Visibility.Visible;
        ShellFrame.BorderThickness = new Thickness(0);
        ShellFrame.CornerRadius = new CornerRadius(12);
        SideRail.CornerRadius = ShellFrame.CornerRadius;
        if (ContentGrid.ActualWidth > 0)
            _parameterWidth = Math.Clamp(_parameterWidth, 240, Math.Max(240, Math.Min(430, ContentGrid.ActualWidth - 760)));
        ParameterColumn.Width = new GridLength(ViewModel.ParametersVisible ? _parameterWidth : 0);
        DividerColumn.Width = new GridLength(ViewModel.ParametersVisible ? 8 : 0);
    }

    private void RootThemeChanged(FrameworkElement sender, object args)
    {
        QueueWindowFrameUpdate();
        UpdateTitleBar();
    }
    private void WindowActivated(object sender, WindowActivatedEventArgs args) => QueueWindowFrameUpdate();
    private void AppWindowChanged(Microsoft.UI.Windowing.AppWindow sender, AppWindowChangedEventArgs args)
    {
        if (args.DidPresenterChange || args.DidSizeChange) QueueWindowFrameUpdate();
        UpdateTitleBar();
    }

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
        _loaded = false;
        _tray?.Dispose();
        _frame.Dispose();
        _acquisitionStop?.Cancel();
        Workspace.ReconstructionRequested -= WorkspaceReconstructionRequested;
        _connectionLifetime.Cancel();
        CloseDevice();
        _serialConnection.Dispose();
        _tcpConnection.Dispose();
        _networkConnection.Dispose();
        ViewModel.PropertyChanged -= ViewModelPropertyChanged;
        SideRail.ThemeRequested -= SideRailThemeRequested;
        Root.ActualThemeChanged -= RootThemeChanged;
        Activated -= WindowActivated;
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
