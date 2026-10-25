using Tomography.Business.Interfaces;
using Tomography.Business.Models;

namespace Tomography.Business.Services;

/// <summary>一次采样指令对应一个 256 字节二进制帧，不把串口回调当作完整帧。</summary>
public static class EmtSampling
{
    public static async Task<OperationResult> ExecuteAsync(IDeviceChannel channel, int frequencyKhz, int coilCount,
        CancellationToken cancellationToken = default, string arrayType = "R", string excitationMode = "V")
    {
        var command = EmtSampleCommand.Build(frequencyKhz, coilCount, arrayType, excitationMode);
        var completion = new TaskCompletionSource<OperationResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        var decoder = new EmtBinaryFrameDecoder();
        var sync = new object();
        void Receive(object? sender, LogEntry entry)
        {
            lock (sync)
            {
                if (completion.Task.IsCompleted) return;
                try
                {
                    var frame = decoder.Feed(entry.RawData);
                    if (frame is not null) completion.TrySetResult(new() { IsSuccess = true, Data = frame, Message = "已收到 64 通道二进制数据。" });
                }
                catch (FormatException ex) { completion.TrySetResult(new() { Message = ex.Message, Exception = ex }); }
            }
        }
        channel.DataReceived += Receive;
        try
        {
            var sent = await Task.Run(() => channel.SendText(command, "ASCII"), cancellationToken).ConfigureAwait(false);
            if (!sent.IsSuccess) return sent;
            return await completion.Task.WaitAsync(TimeSpan.FromSeconds(30), cancellationToken).ConfigureAwait(false);
        }
        catch (TimeoutException ex) { return new() { Message = $"EMT 采样超时，已收到 {decoder.ReceivedBytes}/256 字节。请检查下位机采样状态。", Exception = ex }; }
        catch (OperationCanceledException ex) { return new() { Message = "采样已取消。", Exception = ex }; }
        finally { channel.DataReceived -= Receive; }
    }
}
