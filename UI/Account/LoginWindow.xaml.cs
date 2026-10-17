using System.Runtime.InteropServices;
using Microsoft.UI;
using Microsoft.UI.Input;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Windows.Graphics;

namespace Tomography.UI.Account;

public sealed partial class LoginWindow : Window
{
    public LoginViewModel ViewModel { get; }
    private readonly bool _allowAutoLogin;
    private readonly bool _switchAccount;
    public event EventHandler? Authenticated;
    private bool _closed;

    public LoginWindow(LoginViewModel viewModel, bool allowAutoLogin, bool switchAccount = false)
    {
        ViewModel = viewModel; _allowAutoLogin = allowAutoLogin; _switchAccount = switchAccount;
        InitializeComponent();
        var hwnd = Win32Interop.GetWindowFromWindowId(AppWindow.Id);
        var scale = GetDpiForWindow(hwnd) / 96d;
        var work = DisplayArea.GetFromWindowId(AppWindow.Id, DisplayAreaFallback.Nearest).WorkArea;
        var width = Math.Min(work.Width, (int)(1000 * scale));
        var height = Math.Min(work.Height, (int)(640 * scale));
        AppWindow.MoveAndResize(new(work.X + (work.Width - width) / 2, work.Y + (work.Height - height) / 2, width, height));
        AppWindow.SetIcon(System.IO.Path.Combine(AppContext.BaseDirectory, "Assets", "AppIcon.ico"));
        ExtendsContentIntoTitleBar = true;
        AppWindow.TitleBar.PreferredHeightOption = TitleBarHeightOption.Collapsed;
        if (AppWindow.Presenter is OverlappedPresenter presenter)
        {
            presenter.SetBorderAndTitleBar(false, false);
            presenter.PreferredMinimumWidth = Math.Min(work.Width, (int)(800 * scale));
            presenter.PreferredMinimumHeight = Math.Min(work.Height, (int)(560 * scale));
            presenter.IsMaximizable = false;
        }
        var border = 0xFFFFFFFEu;
        DwmSetWindowAttribute(hwnd, 34, ref border, sizeof(uint));
        ViewModel.Authenticated += ViewModelAuthenticated;
        Closed += (_, _) => { _closed = true; ViewModel.ClearSecrets(); ViewModel.Authenticated -= ViewModelAuthenticated; };
    }

    public static Visibility ToVisibility(bool value) => value ? Visibility.Visible : Visibility.Collapsed;
    private async void RootLoaded(object sender, RoutedEventArgs e)
    {
        RootSizeChanged(sender, null!);
        await ViewModel.InitializeAsync(_allowAutoLogin);
        if (_switchAccount) ViewModel.Username = "";
    }
    private void RootSizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (Root.XamlRoot is null) return;
        var scale = Root.XamlRoot.RasterizationScale;
        InputNonClientPointerSource.GetForWindowId(AppWindow.Id).SetRegionRects(NonClientRegionKind.Caption,
            [new RectInt32(0, 0, (int)(Math.Max(0, Root.ActualWidth - 96) * scale), (int)(40 * scale))]);
    }
    private void ViewModelAuthenticated(object? sender, EventArgs e) { if (!_closed) Authenticated?.Invoke(this, EventArgs.Empty); }
    private void MinimizeClicked(object sender, RoutedEventArgs e) { if (AppWindow.Presenter is OverlappedPresenter presenter) presenter.Minimize(); }
    private void CloseClicked(object sender, RoutedEventArgs e) => Close();
    [DllImport("user32.dll")] private static extern uint GetDpiForWindow(nint hwnd);
    [DllImport("dwmapi.dll")] private static extern int DwmSetWindowAttribute(nint hwnd, int attribute, ref uint value, int size);
}
