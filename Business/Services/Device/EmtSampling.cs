using System.Globalization;
using System.Text;
using Tomography.Business.Interfaces;
using Tomography.Business.Models;

namespace Tomography.Business.Services;

/// <summary>USART 文本采样：匹配 START，接收完整行，等待 END 后才允许下一次请求。</summary>
public static class EmtSampling
{
    public static async Task<OperationResult> ExecuteAsync(IDeviceChannel channel, int frequencyKhz, int coilCount,
        CancellationToken cancellationToken = default, string arrayType = "R", string excitationMode = "V")
    {
        var command = EmtSampleCommand.Build(frequencyKhz, coilCount, arrayType, excitationMode);
        var completion = new TaskCompletionSource<OperationResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        var pending = new StringBuilder();
        var values = new List<uint>();
        var started = false;
        var rows = 0;
        var sync = new object();
        void Receive(object? sender, LogEntry entry)
        {
            lock (sync)
            {
                if (completion.Task.IsCompleted) return;
                foreach (var b in entry.RawData)
                {
                    if (b != 10)
                    {
                        if (pending.Length >= 65536) { completion.TrySetResult(new() { Message = "EMT 回复行过长。" }); return; }
                        pending.Append((char)b); continue;
                    }
                    var line = pending.ToString().TrimEnd('\r'); pending.Clear();
                    if (line.StartsWith("ERROR,", StringComparison.Ordinal)) { completion.TrySetResult(new() { Message = "下位机回复：" + line }); return; }
                    if (!started)
                    {
                        if (line == "READY" || line.Length == 0) continue;
                        if (line != $"START,{frequencyKhz},{coilCount}") { completion.TrySetResult(new() { Message = "EMT START 回复与发送参数不匹配。" }); return; }
                        started = true; continue;
                    }
                    if (line == "END")
                    {
                        completion.TrySetResult(rows == coilCount
                            ? new() { IsSuccess = true, Data = values.ToArray(), Message = "EMT 握手成功，采样结束。" }
                            : new() { Message = "EMT 返回的数据行数不匹配。" });
                        return;
                    }
                    var fields = line.Split(',');
                    if (fields.Length != coilCount || rows >= coilCount) { completion.TrySetResult(new() { Message = "EMT 数据列数或行数不匹配。" }); return; }
                    foreach (var field in fields)
                    {
                        if (!uint.TryParse(field, NumberStyles.None, CultureInfo.InvariantCulture, out var value))
                        { completion.TrySetResult(new() { Message = "EMT 数据必须是无符号十进制整数。" }); return; }
                        values.Add(value);
                    }
                    rows++;
                }
            }
        }
        channel.DataReceived += Receive;
        try
        {
            var sent = await Task.Run(() => channel.SendText(command, "ASCII"), cancellationToken).ConfigureAwait(false);
            if (!sent.IsSuccess) return sent;
            return await completion.Task.WaitAsync(TimeSpan.FromSeconds(30), cancellationToken).ConfigureAwait(false);
        }
        catch (TimeoutException ex) { return new() { Message = "EMT 回复超时，未收到完整 START / 数据 / END。", Exception = ex }; }
        catch (OperationCanceledException ex) { return new() { Message = "EMT 操作已取消。", Exception = ex }; }
        finally { channel.DataReceived -= Receive; }
    }
}
