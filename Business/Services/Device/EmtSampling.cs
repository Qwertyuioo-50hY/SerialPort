using Tomography.Business.Interfaces;
using Tomography.Business.Models;

namespace Tomography.Business.Services;

/// <summary>一次采样请求：积累完整帧后解码，不将串口回调视为帧边界。</summary>
public static class EmtSampling
{
    // 调用方必须串行采样；接收失败后应断开重连，避免迟到帧影响下一次请求。
    public static async Task<OperationResult> ExecuteAsync(ISerialService serial, int frequencyKhz, int coilCount,
        CancellationToken cancellationToken = default)
    {
        var command = EmtSampleCommand.Build(frequencyKhz, coilCount);
        var frame = new byte[EmtSampleDecoder.FrameSize];
        var count = 0;
        var sync = new object();
        var completion = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        void Receive(object? sender, LogEntry entry)
        {
            lock (sync)
            {
                if (completion.Task.IsCompleted) return;
                if (entry.RawData.Length > frame.Length - count)
                {
                    completion.TrySetResult(false);
                    return;
                }
                entry.RawData.CopyTo(frame, count);
                count += entry.RawData.Length;
                if (count == frame.Length) completion.TrySetResult(true);
            }
        }
        serial.DataReceived += Receive;
        try
        {
            var sent = serial.SendText(command, "ASCII");
            if (!sent.IsSuccess) return sent;
            var complete = await completion.Task.WaitAsync(TimeSpan.FromSeconds(3), cancellationToken).ConfigureAwait(false);
            return complete
                ? new OperationResult { IsSuccess = true, Message = "已采集 64 个通道。", Data = EmtSampleDecoder.Decode(frame) }
                : new OperationResult { Message = "采样回复超过 256 字节。" };
        }
        catch (TimeoutException ex)
        {
            return new OperationResult { Message = $"采样超时，已接收 {count}/256 字节。", Exception = ex };
        }
        catch (OperationCanceledException ex)
        {
            return new OperationResult { Message = "采样已取消。", Exception = ex };
        }
        finally
        {
            serial.DataReceived -= Receive;
        }
    }
}
