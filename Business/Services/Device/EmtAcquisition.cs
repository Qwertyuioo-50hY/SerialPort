using Tomography.Business.Interfaces;
using Tomography.Business.Models;

namespace Tomography.Business.Services;

/// <summary>串行请求各帧；停止时收完当前帧，避免残留字节影响下次采集。</summary>
public sealed class EmtAcquisition
{
    public async Task<OperationResult> RunAsync(ISerialService serial, int frequency, int coils, AcquisitionMode mode,
        int frameCount, Action<uint[]> onFrame, CancellationToken stopToken, CancellationToken lifetime = default)
    {
        var target = mode switch { AcquisitionMode.Reference => 10, AcquisitionMode.Single => 1,
            AcquisitionMode.Batch => frameCount, AcquisitionMode.Live => int.MaxValue, _ => throw new ArgumentOutOfRangeException(nameof(mode)) };
        if (target < 1) throw new ArgumentOutOfRangeException(nameof(frameCount));
        var sum = new double[64];
        var count = 0;
        while (count < target && !stopToken.IsCancellationRequested)
        {
            var result = await EmtSampling.ExecuteAsync(serial, frequency, coils, lifetime);
            if (!result.IsSuccess) return result;
            var frame = (uint[])result.Data!;
            count++;
            if (mode == AcquisitionMode.Reference)
                for (var i = 0; i < sum.Length; i++) sum[i] += frame[i];
            onFrame(frame);
            await Task.Yield();
        }
        return new OperationResult { IsSuccess = true,
            Message = count == target ? $"采集完成：{count} 帧。" : $"已停止采集：{count} 帧。",
            Data = mode == AcquisitionMode.Reference && count == 10 ? sum.Select(value => value / 10).ToArray() : null };
    }
}
