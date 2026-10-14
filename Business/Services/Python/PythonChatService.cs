using System.Text.Json;
using Python.Runtime;
using Tomography.Business.Models;

namespace Tomography.Business.Services;

public sealed class PythonChatService
{
    public async Task<string> SendAsync(string configPath, string pythonDll, string apiKey, IReadOnlyList<AiMessage> messages)
    {
        if (!File.Exists(configPath)) throw new FileNotFoundException("请先在设置中保存 AI 配置。", configPath);
        return await Task.Run(() =>
        {
            PythonRuntimeHost.Initialize(pythonDll);
            using var gil = Py.GIL();
            using var scope = Py.CreateScope();
            scope.Set("scripts", Path.Combine(AppContext.BaseDirectory, "Python"));
            scope.Set("config_path", configPath);
            scope.Set("api_key", apiKey);
            scope.Set("request_json", JsonSerializer.Serialize(new {
                messages = messages.Select(message => new { role = message.Role, content = message.Content })
            }));
            try { scope.Exec("import sys\nif scripts not in sys.path: sys.path.insert(0, scripts)\nfrom AI.config_api import run_from_file\nresponse_json = run_from_file(config_path, request_json, api_key)"); }
            catch (PythonException ex) { throw new InvalidOperationException(ex.Message, ex); }
            using var response = scope.Get("response_json");
            using var json = JsonDocument.Parse(response.As<string>());
            return json.RootElement.GetProperty("reply").GetString() ?? "";
        }).ConfigureAwait(false);
    }
}
