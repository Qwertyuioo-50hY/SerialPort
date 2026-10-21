using System.Runtime.InteropServices;

namespace Tomography.UI.Account;

// Let XAML occupy the entire HWND, including the resize strip Windows retains
// above an extended title bar. Keep the OS hit testing and DWM rounded corners.
internal sealed class BorderlessLoginFrame : IDisposable
{
    private readonly nint _hwnd;
    private readonly SubclassProc _procedure;
    private readonly bool _resize;
    public BorderlessLoginFrame(nint hwnd, bool resize = false)
    {
        _hwnd = hwnd; _resize = resize;
        _procedure = ProcessMessage;
        if (!SetWindowSubclass(hwnd, _procedure, 1, 0)) throw new InvalidOperationException("Unable to initialize the login window frame.");
        SetWindowPos(hwnd, 0, 0, 0, 0, 0, 0x0001 | 0x0002 | 0x0004 | 0x0010 | 0x0020);
    }
    private nint ProcessMessage(nint hwnd, uint message, nuint wParam, nint lParam, nuint id, nuint data)
    {
        if (_resize && message == 0x0084 && !IsZoomed(hwnd))
        {
            GetWindowRect(hwnd, out var rect);
            var x = (short)(lParam.ToInt64() & 0xffff); var y = (short)((lParam.ToInt64() >> 16) & 0xffff);
            var left = x < rect.Left + 10; var right = x >= rect.Right - 10;
            var top = y < rect.Top + 10; var bottom = y >= rect.Bottom - 10;
            if (top) return left ? 13 : right ? 14 : 12;
            if (bottom) return left ? 16 : right ? 17 : 15;
            if (left) return 10; if (right) return 11;
        }
        if (message == 0x0083 && wParam != 0)
        {
            if (IsZoomed(hwnd))
            {
                var info = new MonitorInfo { Size = Marshal.SizeOf<MonitorInfo>() };
                if (GetMonitorInfo(MonitorFromWindow(hwnd, 2), ref info)) Marshal.StructureToPtr(info.Work, lParam, false);
            }
            return 0;
        } // WM_NCCALCSIZE: full client area
        return DefSubclassProc(hwnd, message, wParam, lParam);
    }
    public void Dispose() => RemoveWindowSubclass(_hwnd, _procedure, 1);
    [StructLayout(LayoutKind.Sequential)] private struct Rect { public int Left, Top, Right, Bottom; }
    [StructLayout(LayoutKind.Sequential)] private struct MonitorInfo { public int Size; public Rect Monitor, Work; public uint Flags; }
    [DllImport("user32.dll")] private static extern bool GetWindowRect(nint hwnd, out Rect rect);
    [DllImport("user32.dll")] private static extern bool IsZoomed(nint hwnd);
    [DllImport("user32.dll")] private static extern nint MonitorFromWindow(nint hwnd, uint flags);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern bool GetMonitorInfo(nint monitor, ref MonitorInfo info);
    private delegate nint SubclassProc(nint hwnd, uint message, nuint wParam, nint lParam, nuint id, nuint data);
    [DllImport("comctl32.dll")][return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetWindowSubclass(nint hwnd, SubclassProc procedure, nuint id, nuint data);
    [DllImport("comctl32.dll")][return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool RemoveWindowSubclass(nint hwnd, SubclassProc procedure, nuint id);
    [DllImport("comctl32.dll")]
    private static extern nint DefSubclassProc(nint hwnd, uint message, nuint wParam, nint lParam);
    [DllImport("user32.dll")][return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetWindowPos(nint hwnd, nint after, int x, int y, int width, int height, uint flags);
}
