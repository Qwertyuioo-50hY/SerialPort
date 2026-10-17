using Python.Runtime;

namespace Tomography.Business.Services;

/// <summary>进程内 CPython 生命周期和 GIL 初始化。</summary>
public static class PythonRuntimeHost
{
    private static readonly object Sync = new();
    private static string? _dll;
    public static string FindPythonDll()
    {
        var bundled = Path.Combine(AppContext.BaseDirectory, "Runtime", "Python", "python313.dll");
        if (File.Exists(bundled)) return bundled;
        var configured = Environment.GetEnvironmentVariable("PYTHONNET_PYDLL");
        if (!string.IsNullOrWhiteSpace(configured)) return configured;
        var root = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Python");
        return Directory.Exists(root) ? Directory.EnumerateFiles(root, "python3??.dll", SearchOption.AllDirectories)
            .OrderByDescending(path => path, StringComparer.OrdinalIgnoreCase).FirstOrDefault() ?? "" : "";
    }
    public static void Initialize(string pythonDll)
    {
        lock (Sync)
        {
            var path = Path.GetFullPath(pythonDll);
            if (_dll is not null)
            {
                if (!path.Equals(_dll, StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("Python 已初始化，更换运行时需要重启应用。");
                return;
            }
            if (!File.Exists(path)) throw new FileNotFoundException("请选择本机 64 位 CPython 的运行时 DLL。", path);
            Runtime.PythonDLL = path;
            PythonEngine.PythonHome = Path.GetDirectoryName(path)!;
            PythonEngine.Initialize();
            // Installation directories may be read-only; importing modules must not write bytecode there.
            PythonEngine.Exec("import sys; sys.dont_write_bytecode = True");
            PythonEngine.BeginAllowThreads();
            _dll = path;
        }
    }
}
