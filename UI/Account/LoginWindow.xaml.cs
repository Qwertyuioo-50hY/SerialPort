using System.Runtime.InteropServices;
using Microsoft.UI;
using Microsoft.UI.Input;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Media;
using Windows.Graphics;

namespace Tomography.UI.Account;

public sealed partial class LoginWindow : Window
{
    public LoginViewModel ViewModel { get; }
    private readonly bool _allowAutoLogin;
    private readonly bool _switchAccount;
    public event EventHandler? Authenticated;
    private bool _closed;
    private readonly BorderlessLoginFrame _frame;

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
        _frame = new BorderlessLoginFrame(hwnd);
        Root.RequestedTheme = ElementTheme.Dark;
        Root.ActualThemeChanged += (_, _) => UpdateFrameColors();
        Activated += (_, _) => UpdateFrameColors();
        UpdateFrameColors();
        var corner = 2u;
        DwmSetWindowAttribute(hwnd, 33, ref corner, sizeof(uint));

        ViewModel.Authenticated += ViewModelAuthenticated;
        ViewModel.PropertyChanged += ViewModelChanged;
        Closed += (_, _) => { ViewModel.PropertyChanged -= ViewModelChanged; _frame.Dispose(); _closed = true; ViewModel.ClearSecrets(); ViewModel.Authenticated -= ViewModelAuthenticated; };
    }

    public static Visibility ToVisibility(bool value) => value ? Visibility.Visible : Visibility.Collapsed;
    private async void RootLoaded(object sender, RoutedEventArgs e)
    {
        Root.Language = ViewModel.English ? "en-US" : "zh-CN";
        LanguageMenu.SetLanguage(ViewModel.English ? "en" : "zh");
        UpdateFrameColors();
        RootSizeChanged(sender, null!);
        await ViewModel.InitializeAsync(_allowAutoLogin);
        if (_switchAccount) ViewModel.Username = "";
    }
    private void RootSizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (Root.XamlRoot is null) return;
        var scale = Root.XamlRoot.RasterizationScale;
        InputNonClientPointerSource.GetForWindowId(AppWindow.Id).SetRegionRects(NonClientRegionKind.Caption,
            [new RectInt32(0, 0, (int)(Math.Max(0, Root.ActualWidth - 152) * scale), (int)(48 * scale))]);
    }
    public static Visibility MessageVisibility(string message) => ToVisibility(!string.IsNullOrWhiteSpace(message));
    private void LanguageSelected(object? sender, string language) => ChangeLanguage(language == "en");
    private void ChangeLanguage(bool english)
    {
        ViewModel.SetLanguage(english);
        Root.Language = english ? "en-US" : "zh-CN";
        LanguageMenu.SetLanguage(english ? "en" : "zh");
        UpdatePasswordAppearance();
    }
    private void PasswordChanged(object sender, RoutedEventArgs e) => UpdatePasswordAppearance();
    private void PasswordEyeClicked(object sender, RoutedEventArgs e)
    {
        PasswordInput.PasswordRevealMode = PasswordInput.PasswordRevealMode == PasswordRevealMode.Visible
            ? PasswordRevealMode.Hidden : PasswordRevealMode.Visible;
        UpdatePasswordAppearance();
    }
    private void ConfirmationEyeClicked(object sender, RoutedEventArgs e)
    {
        ConfirmationInput.PasswordRevealMode = ConfirmationInput.PasswordRevealMode == PasswordRevealMode.Visible
            ? PasswordRevealMode.Hidden : PasswordRevealMode.Visible;
        UpdatePasswordAppearance();
    }
    private void ViewModelChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(LoginViewModel.IsRegistering))
        {
            PasswordInput.PasswordRevealMode = ConfirmationInput.PasswordRevealMode = PasswordRevealMode.Hidden;
            UpdatePasswordAppearance();
        }
    }
    private void UpdatePasswordAppearance()
    {
        if (ConfirmationInput is null || PasswordEyeIcon is null || ConfirmationStatusIcon is null) return;
        var entered = ConfirmationInput.Password.Length > 0;
        var matches = entered && PasswordInput.Password == ConfirmationInput.Password;
        var neutral = Root.ActualTheme == ElementTheme.Dark ? ColorHelper.FromArgb(255, 160, 160, 168) : ColorHelper.FromArgb(255, 100, 100, 110);
        var color = !entered ? neutral : matches ? ColorHelper.FromArgb(255, 48, 185, 103) : ColorHelper.FromArgb(255, 239, 83, 80);
        ConfirmationStatusIcon.Glyph = !entered ? "\uE72E" : matches ? "\uE73E" : "\uE711";
        ConfirmationStatusIcon.Foreground = ConfirmationEyeIcon.Foreground = new SolidColorBrush(color);
        AutomationProperties.SetName(ConfirmationStatusIcon, !entered ? ViewModel.ConfirmationText : matches ? ViewModel.MatchText : ViewModel.MismatchText);
        PasswordEyeIcon.Glyph = PasswordInput.PasswordRevealMode == PasswordRevealMode.Visible ? "\uED1A" : "\uE890";
        ConfirmationEyeIcon.Glyph = ConfirmationInput.PasswordRevealMode == PasswordRevealMode.Visible ? "\uED1A" : "\uE890";
        AutomationProperties.SetName(PasswordEye, PasswordInput.PasswordRevealMode == PasswordRevealMode.Visible ? ViewModel.HidePasswordText : ViewModel.ShowPasswordText);
        AutomationProperties.SetName(ConfirmationEye, ConfirmationInput.PasswordRevealMode == PasswordRevealMode.Visible ? ViewModel.HidePasswordText : ViewModel.ShowPasswordText);
    }
    private void UpdateFrameColors()
    {
        var hwnd = Win32Interop.GetWindowFromWindowId(AppWindow.Id);
        var dark = Root.RequestedTheme == ElementTheme.Dark ? 1u : 0u;
        var color = dark == 1 ? 0u : 0xFFFFFFu;
        Root.Background = new SolidColorBrush(dark == 1 ? Colors.Black : Colors.White);
        DwmSetWindowAttribute(hwnd, 20, ref dark, sizeof(uint));
        DwmSetWindowAttribute(hwnd, 34, ref color, sizeof(uint));
        DwmSetWindowAttribute(hwnd, 35, ref color, sizeof(uint));
        UpdatePasswordAppearance();
    }
    private void ViewModelAuthenticated(object? sender, EventArgs e) { if (!_closed) Authenticated?.Invoke(this, EventArgs.Empty); }
    private void MinimizeClicked(object sender, RoutedEventArgs e) { if (AppWindow.Presenter is OverlappedPresenter presenter) presenter.Minimize(); }
    private void CloseClicked(object sender, RoutedEventArgs e) => Close();
    [DllImport("user32.dll")] private static extern uint GetDpiForWindow(nint hwnd);
    [DllImport("dwmapi.dll")] private static extern int DwmSetWindowAttribute(nint hwnd, int attribute, ref uint value, int size);
}
