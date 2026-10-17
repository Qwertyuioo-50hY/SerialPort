using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.Json;

namespace Tomography.UI.windows;

internal sealed class QtWindowHost : IDisposable
{
    private Process? _process;
    private bool _disposed;
    private readonly TaskCompletionSource<nint> _ready = new(TaskCreationOptions.RunContinuationsAsynchronously);
    public event Action? ExitRequested;
    public nint WindowHandle { get; private set; }
    public bool IsRunning => !_disposed && _process is { HasExited: false };

    public async Task StartAsync(string python, nint child, string theme)
    {
        var script = Path.Combine(AppContext.BaseDirectory, "QtShell", "main.py");
        if (!File.Exists(script)) throw new FileNotFoundException("安装目录缺少 QtShell/main.py，请重新部署最新构建。", script);
        var start = new ProcessStartInfo(python)
        {
            UseShellExecute = false, CreateNoWindow = true, WindowStyle = ProcessWindowStyle.Hidden,
            RedirectStandardInput = true, RedirectStandardOutput = true, RedirectStandardError = true,
            StandardOutputEncoding = Encoding.UTF8, StandardErrorEncoding = Encoding.UTF8
        };
        start.ArgumentList.Add(script);
        start.ArgumentList.Add("--window"); start.ArgumentList.Add(child.ToString());
        start.ArgumentList.Add("--theme"); start.ArgumentList.Add(theme);
        start.Environment["PYTHONUTF8"] = "1";
        start.Environment["PYTHONDONTWRITEBYTECODE"] = "1";
        _process = new Process { StartInfo = start, EnableRaisingEvents = true };
        _process.OutputDataReceived += (_, e) =>
        {
            if (e.Data is null) return;
            try
            {
                using var data = JsonDocument.Parse(e.Data);
                var message = data.RootElement;
                switch (message.GetProperty("event").GetString())
                {
                    case "ready": _ready.TrySetResult((nint)message.GetProperty("window").GetInt64()); break;
                    case "exit": if (!_disposed) ExitRequested?.Invoke(); break;
                    case "error": _ready.TrySetException(new InvalidOperationException("Qt 主窗口启动失败，请安装 UI/QtShell/requirements.txt 中的依赖。")); break;
                }
            }
            catch (JsonException) { }
        };
        // Drain Qt diagnostics without creating logs or exposing arbitrary process output.
        _process.ErrorDataReceived += (_, _) => { };
        _process.Exited += (_, _) => _ready.TrySetException(new InvalidOperationException("Qt 主窗口进程已退出。"));
        _process.Start();
        _process.BeginOutputReadLine(); _process.BeginErrorReadLine();
        WindowHandle = await _ready.Task.WaitAsync(TimeSpan.FromSeconds(20));
    }
    public void Send(string command, string? theme = null)
    {
        try
        {
            if (IsRunning) { _process!.StandardInput.WriteLine(JsonSerializer.Serialize(new { command, theme })); _process.StandardInput.Flush(); }
        }
        catch (Exception ex) when (ex is IOException or InvalidOperationException) { }
    }
    public void Dispose()
    {
        if (_disposed) return;
        Send("quit"); _disposed = true;
        _process?.Dispose();
    }
}
