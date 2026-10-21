using System.Runtime.InteropServices;

namespace Tomography.UI.windows;

internal sealed class TrayIcon : IDisposable
{
    private readonly nint _hwnd;
    private readonly SubclassProc _proc;
    private readonly Action _restore, _exit;
    private NotifyData _data;
    private readonly uint _taskbarCreated;
    public TrayIcon(nint hwnd, Action restore, Action exit)
    {
        _hwnd = hwnd; _restore = restore; _exit = exit; _proc = Message;
        _taskbarCreated = RegisterWindowMessage("TaskbarCreated");
        _data = new NotifyData { Size = (uint)Marshal.SizeOf<NotifyData>(), Window = hwnd, Id = 1, Flags = 7, Callback = 0x8001,
            Icon = LoadImage(0, System.IO.Path.Combine(AppContext.BaseDirectory, "Assets", "AppIcon.ico"), 1, 0, 0, 0x10), Tip = "FieldSync", Info = "", Title = "" };
        if (!SetWindowSubclass(hwnd, _proc, 2, 0)) throw new InvalidOperationException("托盘初始化失败。");
        if (!Shell_NotifyIcon(0, ref _data)) { Dispose(); throw new InvalidOperationException("托盘图标创建失败。"); }
    }
    private nint Message(nint hwnd, uint msg, nuint wp, nint lp, nuint id, nuint data)
    {
        if (msg == _taskbarCreated) Shell_NotifyIcon(0, ref _data);
        if (msg == 0x8001)
        {
            if (lp == 0x0202) _restore();
            if (lp == 0x0205)
            {
                var menu = CreatePopupMenu();
                try
                {
                    AppendMenu(menu, 0, 1, "退出程序"); GetCursorPos(out var point); SetForegroundWindow(hwnd);
                    if (TrackPopupMenu(menu, 0x0100 | 0x0002, point.X, point.Y, 0, hwnd, 0) == 1) _exit();
                    PostMessage(hwnd, 0, 0, 0);
                }
                finally { DestroyMenu(menu); }
            }
            return 0;
        }
        return DefSubclassProc(hwnd, msg, wp, lp);
    }
    public void Dispose()
    {
        Shell_NotifyIcon(2, ref _data); RemoveWindowSubclass(_hwnd, _proc, 2);
        if (_data.Icon != 0) { DestroyIcon(_data.Icon); _data.Icon = 0; }
    }
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)] private struct NotifyData
    {
        public uint Size; public nint Window; public uint Id, Flags, Callback; public nint Icon;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)] public string Tip;
        public uint State, StateMask;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)] public string Info;
        public uint Timeout;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)] public string Title;
        public uint InfoFlags; public Guid Guid; public nint BalloonIcon;
    }
    [StructLayout(LayoutKind.Sequential)] private struct Point { public int X, Y; }
    private delegate nint SubclassProc(nint hwnd, uint msg, nuint wp, nint lp, nuint id, nuint data);
    [DllImport("shell32.dll", CharSet = CharSet.Unicode)] private static extern bool Shell_NotifyIcon(uint command, ref NotifyData data);
    [DllImport("comctl32.dll")] private static extern bool SetWindowSubclass(nint hwnd, SubclassProc proc, nuint id, nuint data);
    [DllImport("comctl32.dll")] private static extern bool RemoveWindowSubclass(nint hwnd, SubclassProc proc, nuint id);
    [DllImport("comctl32.dll")] private static extern nint DefSubclassProc(nint hwnd, uint msg, nuint wp, nint lp);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern nint LoadImage(nint instance, string name, uint type, int x, int y, uint flags);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern uint RegisterWindowMessage(string message);
    [DllImport("user32.dll")] private static extern bool DestroyIcon(nint icon);
    [DllImport("user32.dll")] private static extern nint CreatePopupMenu();
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern bool AppendMenu(nint menu, uint flags, nuint id, string text);
    [DllImport("user32.dll")] private static extern bool GetCursorPos(out Point point);
    [DllImport("user32.dll")] private static extern bool SetForegroundWindow(nint hwnd);
    [DllImport("user32.dll")] private static extern uint TrackPopupMenu(nint menu, uint flags, int x, int y, int reserved, nint hwnd, nint rect);
    [DllImport("user32.dll")] private static extern bool DestroyMenu(nint menu);
    [DllImport("user32.dll")] private static extern bool PostMessage(nint hwnd, uint msg, nuint wp, nint lp);
}
