using System.ComponentModel;
using System.Runtime.InteropServices;

namespace Tomography.UI.windows;

/// <summary>Owns the native notification icon and its window callback on the UI thread.</summary>
internal sealed class TrayIcon : IDisposable
{
    private const uint CallbackMessage = 0x8000 + 43;
    private readonly nint _window;
    private readonly nint _icon;
    private readonly SubclassProc _callback;
    private readonly Action _restore;
    private readonly Action _exit;
    private readonly uint _taskbarCreated = RegisterWindowMessage("TaskbarCreated");
    private bool _added;
    private bool _disposed;

    public TrayIcon(nint window, string iconPath, Action restore, Action exit)
    {
        _window = window; _restore = restore; _exit = exit; _callback = WindowCallback;
        _icon = LoadImage(0, iconPath, 1, 32, 32, 0x10);
        if (_icon == 0) throw new Win32Exception(Marshal.GetLastWin32Error());
        if (!SetWindowSubclass(window, _callback, 43, 0))
        { DestroyIcon(_icon); throw new Win32Exception(Marshal.GetLastWin32Error()); }
        if (!EnsureVisible())
        { Dispose(); throw new Win32Exception("无法创建托盘图标。"); }
    }
    private NotifyIconData Data() => new()
    {
        Size = (uint)Marshal.SizeOf<NotifyIconData>(), Window = _window, Id = 43,
        Flags = 1 | 2 | 4, Callback = CallbackMessage, Icon = _icon, Tip = "FieldSync — 双击打开，右键退出",
        Info = "", InfoTitle = ""
    };
    public bool EnsureVisible()
    {
        if (_disposed) return false;
        var data = Data();
        if (_added && Shell_NotifyIcon(1, ref data)) return true;
        _added = Shell_NotifyIcon(0, ref data);
        if (_added) { data.Version = 3; Shell_NotifyIcon(4, ref data); }
        return _added;
    }
    private nint WindowCallback(nint window, uint message, nuint wParam, nint lParam, nuint id, nuint data)
    {
        if (message == _taskbarCreated) { _added = false; EnsureVisible(); }
        if (message == CallbackMessage)
        {
            var notification = (uint)(long)lParam;
            if (notification is 0x202 or 0x203 or 0x400 or 0x401) _restore(); // mouse/keyboard activation
            else if (notification is 0x205 or 0x7B) ShowMenu();
            return 0;
        }
        return DefSubclassProc(window, message, wParam, lParam);
    }
    private void ShowMenu()
    {
        var menu = CreatePopupMenu();
        if (menu == 0) return;
        try
        {
            AppendMenu(menu, 0, 1, "打开 FieldSync");
            AppendMenu(menu, 0x800, 0, "");
            AppendMenu(menu, 0, 2, "退出");
            GetCursorPos(out var point);
            SetForegroundWindow(_window);
            var command = TrackPopupMenu(menu, 0x100 | 0x2, point.X, point.Y, 0, _window, 0);
            PostMessage(_window, 0, 0, 0);
            if (command == 1) _restore();
            else if (command == 2) _exit();
        }
        finally { DestroyMenu(menu); }
    }
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        var data = Data();
        if (_added) Shell_NotifyIcon(2, ref data);
        RemoveWindowSubclass(_window, _callback, 43);
        DestroyIcon(_icon);
    }
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct NotifyIconData
    {
        public uint Size; public nint Window; public uint Id, Flags, Callback; public nint Icon;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)] public string Tip;
        public uint State, StateMask;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)] public string Info;
        public uint Version;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)] public string InfoTitle;
        public uint InfoFlags; public Guid Guid; public nint BalloonIcon;
    }
    [StructLayout(LayoutKind.Sequential)] private struct Point { public int X, Y; }
    private delegate nint SubclassProc(nint window, uint message, nuint wParam, nint lParam, nuint id, nuint data);
    [DllImport("shell32.dll", CharSet = CharSet.Unicode)] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool Shell_NotifyIcon(uint message, ref NotifyIconData data);
    [DllImport("comctl32.dll")] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool SetWindowSubclass(nint window, SubclassProc callback, nuint id, nuint data);
    [DllImport("comctl32.dll")] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool RemoveWindowSubclass(nint window, SubclassProc callback, nuint id);
    [DllImport("comctl32.dll")] private static extern nint DefSubclassProc(nint window, uint message, nuint wParam, nint lParam);
    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)] private static extern nint LoadImage(nint instance, string name, uint type, int width, int height, uint flags);
    [DllImport("user32.dll")] private static extern bool DestroyIcon(nint icon);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern uint RegisterWindowMessage(string name);
    [DllImport("user32.dll")] private static extern nint CreatePopupMenu();
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern bool AppendMenu(nint menu, uint flags, nuint id, string text);
    [DllImport("user32.dll")] private static extern uint TrackPopupMenu(nint menu, uint flags, int x, int y, int reserved, nint window, nint rectangle);
    [DllImport("user32.dll")] private static extern bool DestroyMenu(nint menu);
    [DllImport("user32.dll")] private static extern bool GetCursorPos(out Point point);
    [DllImport("user32.dll")] private static extern bool SetForegroundWindow(nint window);
    [DllImport("user32.dll")] private static extern bool PostMessage(nint window, uint message, nuint wParam, nint lParam);
}
