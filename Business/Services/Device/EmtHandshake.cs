using System.Text;
using Tomography.Business.Interfaces;
using Tomography.Business.Models;

namespace Tomography.Business.Services;

/// <summary>发送设备识别请求，等待完整的 EMT 握手回复。</summary>
public static class EmtHandshake
{
    public static async Task<OperationResult> ExecuteAsync(ISerialService serial, CancellationToken cancellationToken = default)
    {
        var expected = Encoding.ASCII.GetBytes("emt ready\r\n");
        var completion = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var received = 0;
        var sync = new object();
        void Receive(object? sender, LogEntry entry)
        {
            lock (sync)
            {
                if (completion.Task.IsCompleted) return;
                foreach (var value in entry.RawData)
                {
                    if (value != expected[received])
                    {
                        completion.TrySetResult(false);
                        return;
                    }
                    if (++received == expected.Length)
                    {
                        completion.TrySetResult(true);
                        return;
                    }
                }
            }
        }
        serial.DataReceived += Receive;
        try
        {
            var result = serial.SendText("which emt\r\n", "ASCII");
            if (!result.IsSuccess) return result;
            var ready = await completion.Task.WaitAsync(TimeSpan.FromSeconds(3), cancellationToken).ConfigureAwait(false);
            return new OperationResult { IsSuccess = ready, Message = ready ? "EMT 握手成功。" : "设备握手回复不匹配。" };
        }
        catch (TimeoutException ex)
        {
            return new OperationResult { Message = "EMT 握手超时（3 秒）。", Exception = ex };
        }
        catch (OperationCanceledException ex)
        {
            return new OperationResult { Message = "EMT 握手已取消。", Exception = ex };
        }
        finally
        {
            serial.DataReceived -= Receive;
        }
    }
}
