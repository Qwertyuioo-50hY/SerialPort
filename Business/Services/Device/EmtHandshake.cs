using System.Text;
using System.Text.RegularExpressions;
using Tomography.Business.Interfaces;
using Tomography.Business.Models;

namespace Tomography.Business.Services;

/// <summary>只发送身份探测指令；收到明确的 EMT 身份回复后才判定握手成功。</summary>
public static class EmtHandshake
{
    public static async Task<OperationResult> ExecuteAsync(IDeviceChannel channel, CancellationToken token = default)
    {
        var completion = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var pending = new StringBuilder();
        var sync = new object();
        void Receive(object? sender, LogEntry entry)
        {
            lock (sync)
            {
                foreach (var b in entry.RawData)
                {
                    if (completion.Task.IsCompleted) return;
                    if (b != 10)
                    {
                        if (pending.Length >= 1024) { completion.TrySetResult(false); return; }
                        pending.Append((char)b); continue;
                    }
                    var line = pending.ToString().Trim(); pending.Clear();
                    if (line.Length == 0 || line == "READY") continue;
                    completion.TrySetResult(line == "emt ready" || Regex.IsMatch(line, @"^simple emt v\d+(\.\d+)*$", RegexOptions.CultureInvariant));
                }
            }
        }
        channel.DataReceived += Receive;
        try
        {
            var sent = await Task.Run(() => channel.SendText("which emt", "ASCII"), token).ConfigureAwait(false);
            if (!sent.IsSuccess) return sent;
            var ready = await completion.Task.WaitAsync(TimeSpan.FromSeconds(3), token).ConfigureAwait(false);
            return new() { IsSuccess = ready, Message = ready ? "EMT 握手成功。" : "收到的回复不是 EMT 设备身份信息，请检查端口和波特率。" };
        }
        catch (TimeoutException ex) { return new() { Message = "EMT 握手无应答，请关闭 SSCOM 等串口工具，确认端口后复位板子重试。", Exception = ex }; }
        catch (OperationCanceledException ex) { return new() { Message = "握手已取消。", Exception = ex }; }
        finally { channel.DataReceived -= Receive; }
    }
}
