using System.Text.Json;
using Python.Runtime;
using Tomography.Business.Interfaces;
using Tomography.Business.Models;

namespace Tomography.Business.Services;

/// <summary>执行 Python 算法，接收并验证二维数据矩阵。</summary>
public sealed partial class PythonReconstructionService : IPythonReconstructionService
{
    private readonly string _scripts;
    private static readonly SemaphoreSlim Gate = new(1, 1);
    public PythonReconstructionService(string? scripts = null) => _scripts = scripts ?? Path.Combine(AppContext.BaseDirectory, "Python");
    public async Task<ReconstructionImage> ReconstructAsync(ReconstructionRequest request, string pythonDll)
    {
        await Gate.WaitAsync().ConfigureAwait(false);
        try
        {
            return await Task.Run(() =>
            {
                PythonRuntimeHost.Initialize(pythonDll);
                using var gil = Py.GIL();
                using var scope = Py.CreateScope();
                scope.Set("scripts", _scripts);
                scope.Set("request_json", JsonSerializer.Serialize(request));
                scope.Exec("import sys\nif scripts not in sys.path: sys.path.insert(0, scripts)\nfrom emt_bridge import run\nresponse_json = run(request_json)");
                using var response = scope.Get("response_json");
                var image = JsonSerializer.Deserialize<ReconstructionImage>(response.As<string>()) ?? throw new InvalidOperationException("Python 未返回图像矩阵。");
                image.Validate();
                return image;
            }).ConfigureAwait(false);
        }
        finally { Gate.Release(); }
    }
    public async Task CheckSyntaxAsync(string sourcePath, string pythonDll)
    {
        await Gate.WaitAsync().ConfigureAwait(false);
        try
        {
            await Task.Run(() =>
            {
                PythonRuntimeHost.Initialize(pythonDll);
                using var gil = Py.GIL();
                using var scope = Py.CreateScope();
                scope.Set("source", File.ReadAllText(sourcePath));
                scope.Set("filename", sourcePath);
                scope.Exec("compile(source, filename, 'exec')");
            }).ConfigureAwait(false);
        }
        finally { Gate.Release(); }
    }
}
