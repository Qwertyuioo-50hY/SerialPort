using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using Tomography.Business.Models;
using Tomography.Business.Services;

public static class PythonChatBridgeTests
{
    public static async Task RunAsync()
    {
        var dll = PythonRuntimeHost.FindPythonDll();
        if (!File.Exists(dll)) throw new FileNotFoundException("Python bridge test requires the configured CPython DLL.");
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "python-chat-test-" + Guid.NewGuid().ToString("N")));
        Directory.CreateDirectory(root);
        var listener = new TcpListener(IPAddress.Loopback, 0); listener.Start();
        try
        {
            var port = ((IPEndPoint)listener.LocalEndpoint).Port;
            var config = Path.Combine(root, "config.json");
            File.WriteAllText(config, JsonSerializer.Serialize(new AiApiSettings { BaseUrl = $"http://127.0.0.1:{port}/v1", Model = "gpt-6.1-sol" }));
            var server = Task.Run(async () =>
            {
                using var peer = await listener.AcceptTcpClientAsync();
                var stream = peer.GetStream();
                using var reader = new StreamReader(stream, Encoding.UTF8, leaveOpen: true);
                if (!(await reader.ReadLineAsync())!.StartsWith("POST /v1/responses ")) throw new Exception("wrong bridge request path");
                while (!string.IsNullOrEmpty(await reader.ReadLineAsync())) { }
                var response = new { id = "resp_test", @object = "response", created_at = 0, status = "completed", model = "gpt-6.1-sol", output_text = "完成", output = new object[] {
                    new { type = "reasoning", summary = new[] { new { type = "summary_text", text = "摘要" } } },
                    new { type = "message", role = "assistant", content = new[] { new { type = "output_text", text = "完成" } } }
                } };
                var events = new object[] {
                    new { type = "response.reasoning_summary_text.delta", delta = "摘要" },
                    new { type = "response.output_text.delta", delta = "完成" },
                    new { type = "response.completed", response }
                };
                var body = Encoding.UTF8.GetBytes(string.Concat(events.Select(item => "data: " + JsonSerializer.Serialize(item) + "\n\n")));
                var headers = Encoding.ASCII.GetBytes($"HTTP/1.1 200 OK\r\nContent-Type: text/event-stream\r\nContent-Length: {body.Length}\r\nConnection: close\r\n\r\n");
                await stream.WriteAsync(headers); await stream.WriteAsync(body); await stream.FlushAsync();
            });
            var received = new List<(string, string)>();
            var result = await new PythonChatService().SendAsync(config, dll, "test-only-key", new[] { new AiMessage("user", "测试") }, (kind, delta) => received.Add((kind, delta)));
            await server.WaitAsync(TimeSpan.FromSeconds(10));
            if (result.Reply != "完成" || result.ReasoningSummary != "摘要" || !received.SequenceEqual(new[] { ("reasoning_summary", "摘要"), ("reply", "完成") }))
                throw new Exception("Python.NET callback or final reply failed");
            Console.WriteLine("PASS: C# -> Python.NET -> SDK SSE -> managed callback -> final reply and reasoning summary; loopback only.");
        }
        finally
        {
            listener.Stop();
            if (!root.StartsWith(Path.GetFullPath(AppContext.BaseDirectory), StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("test cleanup outside workspace");
            Directory.Delete(root, true);
        }
    }
}
