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
        _hwnd = hwnd;  //保留窗口句柄, 后续用于移除消息处理器
        _resize = resize; //表示是否启用自定义边缘命中测试
        _procedure = ProcessMessage;  //保存委托,作为原生窗口消息回调
        if (!SetWindowSubclass(hwnd, _procedure, 1, 0)) throw new InvalidOperationException("Unable to initialize the login window frame.");
        SetWindowPos(hwnd, 0, 0, 0, 0, 0, 0x0001 | 0x0002 | 0x0004 | 0x0010 | 0x0020);
    }
    private nint ProcessMessage(nint hwnd, uint message, nuint wParam, nint lParam, nuint id, nuint data)
    {
        //实现边缘缩放
        if (_resize && message == 0x0084 && !IsZoomed(hwnd))
        {
            GetWindowRect(hwnd, out var rect); //提取窗口矩形
            //提取当前鼠标位置 IParam为当前鼠标坐标 , 低16位为x坐标 高16位为y坐标
            var x = (short)(lParam.ToInt64() & 0xffff);
            var y = (short)((lParam.ToInt64() >> 16) & 0xffff);
            //判断鼠标是否在窗口矩阵周围
            var left = x < rect.Left + 20; var right = x >= rect.Right - 20;
            var top = y < rect.Top + 20; var bottom = y >= rect.Bottom - 20;

            if (top) return left ? 13 : right ? 14 : 12;
            if (bottom) return left ? 16 : right ? 17 : 15;
            if (left) return 10; if (right) return 11;
        }
        //让 XAML 占据整个窗口 
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
