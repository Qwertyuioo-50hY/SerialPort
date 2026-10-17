using System.IO;
using Microsoft.UI;
using Microsoft.UI.Windowing;

namespace Tomography.UI.windows;

public sealed partial class AppWindow
{
    private TrayIcon? _trayIcon;
    private bool _exitRequested;
    private void InitializeBackgroundWindow()
    {
        try
        {
            _trayIcon = new TrayIcon(Win32Interop.GetWindowFromWindowId(AppWindow.Id),
                Path.Combine(AppContext.BaseDirectory, "Assets", "AppIcon.ico"),
                () => DispatcherQueue.TryEnqueue(RestoreFromBackground),
                () => DispatcherQueue.TryEnqueue(() => { _exitRequested = true; Close(); }));
        }
        catch (System.ComponentModel.Win32Exception) { HomePanel.SetStatus("托盘暂不可用，关闭按钮将最小化窗口。"); }
        AppWindow.Closing += BackgroundWindowClosing;
    }
    private void BackgroundWindowClosing(Microsoft.UI.Windowing.AppWindow sender, AppWindowClosingEventArgs args)
    {
        if (_exitRequested) return;
        args.Cancel = true;
        HideToBackground();
    }
    private void HideToBackground()
    {
        if (_trayIcon?.EnsureVisible() == true) AppWindow.Hide();
        else if (AppWindow.Presenter is OverlappedPresenter presenter) presenter.Minimize();
    }
    private void RestoreFromBackground()
    {
        AppWindow.Show();
        if (AppWindow.Presenter is OverlappedPresenter presenter && presenter.State == OverlappedPresenterState.Minimized) presenter.Restore();
        Activate();
    }
}
