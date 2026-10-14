using System.Text.Json;
using Python.Runtime;
using Tomography.Business.Models;

namespace Tomography.Business.Services;

public sealed class PythonChatService
{
    public async Task<string> SendAsync(AiApiSettings settings, string apiKey, IReadOnlyList<AiMessage> messages)
    {
        if (string.IsNullOrWhiteSpace(apiKey) || string.IsNullOrWhiteSpace(settings.Model))
            throw new InvalidOperationException("请先在设置中填写 API 密钥和模型。");
        return await Task.Run(() =>
        {
            PythonRuntimeHost.Initialize(settings.PythonDll);
            using var gil = Py.GIL();
            using var scope = Py.CreateScope();
            scope.Set("scripts", Path.Combine(AppContext.BaseDirectory, "Python"));
            scope.Set("request_json", JsonSerializer.Serialize(new {
                provider = "openai", base_url = settings.BaseUrl, api_key = apiKey, model = settings.Model,
                messages = messages.Select(message => new { role = message.Role, content = message.Content })
            }));
            scope.Exec("import sys\nif scripts not in sys.path: sys.path.insert(0, scripts)\nfrom AI.ai_bridge import run\nresponse_json = run(request_json)");
            using var response = scope.Get("response_json");
            using var json = JsonDocument.Parse(response.As<string>());
            return json.RootElement.GetProperty("reply").GetString() ?? "";
        }).ConfigureAwait(false);
    }
}
