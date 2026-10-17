using System.IO;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Tomography.Business.Services;
using Windows.Graphics;

namespace Tomography.UI.windows;

public sealed partial class AppWindow
{
    private QtWindowHost? _qtHost;
    private bool _exitFromQt;
    private WindowId PickerWindowId => _qtHost is { WindowHandle: not 0 } host
        ? Win32Interop.GetWindowIdFromWindow(host.WindowHandle) : AppWindow.Id;

    public async Task StartQtWindowAsync()
    {
        var dll = File.Exists(_aiSettings.PythonDll) ? _aiSettings.PythonDll : PythonRuntimeHost.FindPythonDll();
        var python = Path.Combine(Path.GetDirectoryName(dll) ?? "", "python.exe");
        try
        {
            if (!File.Exists(python)) throw new FileNotFoundException("Qt 主窗口需要已安装 PySide6 的 Python 运行时。");
            WindowControls.Visibility = Visibility.Collapsed;
            WindowDragRegion.Visibility = Visibility.Visible;
            WindowDragRegion.PointerPressed += (_, e) =>
            {
                if (e.GetCurrentPoint(WindowDragRegion).Properties.IsLeftButtonPressed) _qtHost?.Send("move");
            };
            WindowDragRegion.DoubleTapped += (_, _) => _qtHost?.Send("maximize");
            _titleBarInput?.SetRegionRects(Microsoft.UI.Input.NonClientRegionKind.Caption, Array.Empty<RectInt32>());
            _qtHost = new QtWindowHost();
            _qtHost.ExitRequested += () => DispatcherQueue.TryEnqueue(() => { _exitFromQt = true; Close(); });
            AppWindow.Hide();
            await _qtHost.StartAsync(python, Win32Interop.GetWindowFromWindowId(AppWindow.Id), Root.ActualTheme.ToString());
            AppWindow.Closing += QtContentClosing;
        }
        catch (Exception ex) when (ex is IOException or InvalidOperationException or TimeoutException or System.ComponentModel.Win32Exception)
        {
            _qtHost?.Dispose(); _qtHost = null;
            WindowControls.Visibility = WindowDragRegion.Visibility = Visibility.Visible;
            AppWindow.Show(); Activate(); UpdateTitleBar();
            HomePanel.SetStatus(ex is FileNotFoundException ? ex.Message : "Qt 主窗口未能启动；已保留现有界面。请确认 Python 已安装 PySide6。");
        }
    }
    private void QtContentClosing(Microsoft.UI.Windowing.AppWindow sender, AppWindowClosingEventArgs args)
    {
        if (_exitFromQt || _qtHost?.IsRunning != true) return;
        args.Cancel = true;
        _qtHost.Send("hide");
    }
    private void HideToBackground()
    {
        if (_qtHost?.IsRunning == true) _qtHost.Send("hide");
        else Close();
    }
}
