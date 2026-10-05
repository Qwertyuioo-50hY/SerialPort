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
using Tomography.TitleBar;
using Tomography.UI.ViewModels;
using Windows.Graphics;
using Windows.System;

namespace Tomography.UI.Views;

public sealed partial class MainWindow : Window
{
    public MainViewModel ViewModel { get; } = new();
    private InputNonClientPointerSource? _titleBarInput;
    private double _parameterWidth = 286;
    private bool _loaded;

    public MainWindow()
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
        if (e.PropertyName == nameof(MainViewModel.Section)) BuildParameterContents();
        UpdatePresentation();
    }

    private void NavigationSelectionUpdated(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (!ViewModel.ParametersVisible && sender.SelectedItem != null) sender.SelectedItem = null;
    }

    private void NavigationItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
    {
        if (args.InvokedItemContainer is NavigationViewItem item && item.Tag is string section)
        {
            if (section == "Connect") ToggleDeviceConnection();
            else ViewModel.SelectSection(section);
        }
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

    private void ParameterDragDelta(object sender, DragDeltaEventArgs e)
    {
        _parameterWidth += e.HorizontalChange;
        UpdatePresentation();
    }

    private void ParameterDividerKeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key != VirtualKey.Left && e.Key != VirtualKey.Right) return;
        _parameterWidth += e.Key == VirtualKey.Right ? 16 : -16;
        UpdatePresentation();
        e.Handled = true;
    }

    private async void TitleBarActionRequested(object? sender, TitleBarAction action)
    {
        switch (action)
        {
            case TitleBarAction.ToggleParameters: ViewModel.ParametersVisible = !ViewModel.ParametersVisible; break;
            case TitleBarAction.ConnectDevice: ToggleDeviceConnection(); break;
            case TitleBarAction.ImportFile: await ImportFileAsync(); break;
            case TitleBarAction.ExportFile: await ExportFileAsync(); break;
            case TitleBarAction.ExportAcquisitionCsv: await ExportAcquisitionAsync(false); break;
            case TitleBarAction.ExportAcquisitionMat: await ExportAcquisitionAsync(true); break;
            case TitleBarAction.ExportMatrix: await ExportReconstructionAsync(false); break;
            case TitleBarAction.ExportImage: await ExportReconstructionAsync(true); break;
            case TitleBarAction.OpenChat: ChatPanel.Visibility = Visibility.Visible; break;
            case TitleBarAction.OpenConsole:
                CommandPanel.Visibility = Visibility.Visible;
                CommandRow.Height = new GridLength(220);
                break;
            case TitleBarAction.AddCommand: await AddCustomCommandAsync(); break;
            case TitleBarAction.ConfigureApi: await ConfigureAsync("自定义API"); break;
            case TitleBarAction.ConfigureAlgorithm:
                ViewModel.Section = "算法";
                ViewModel.ParametersVisible = true;
                break;
            case TitleBarAction.ConfigureWaveform: await ConfigureAsync("波形图像"); break;
            default: HandleViewAction(action); break;
            case TitleBarAction.Help:
                var dialog = new ContentDialog
                {
                    XamlRoot = Root.XamlRoot,
                    RequestedTheme = Root.ActualTheme,
                    Title = "快捷键",
                    Content = "Ctrl+M：显示 / 隐藏顶部菜单\nAlt+F / V / I / C / H：文件 / 视图 / 指令 / 自定义 / 帮助\nCtrl+B：显示 / 隐藏参数面板\n拖动面板右侧边缘或按左右方向键：调整宽度\nAlt：显示顶部菜单访问键",
                    CloseButtonText = "关闭"
                };
                await dialog.ShowAsync();
                break;
        }
    }

    private void TitleBarBoundsChanged(object? sender, EventArgs e) => UpdateTitleBar();
    private void RootThemeChanged(FrameworkElement sender, object args) => UpdateTitleBar();
    private void AppWindowChanged(AppWindow sender, AppWindowChangedEventArgs args) => UpdateTitleBar();

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
        if (_acquisitionSettings is not null)
        {
            _acquisitionSettings.AcquisitionRequested -= AcquisitionRequested;
            _acquisitionSettings.StopRequested -= StopAcquisition;
        }
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
