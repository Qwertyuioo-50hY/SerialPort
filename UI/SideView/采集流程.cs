using Tomography.Business.Models;
using Tomography.Business.Services;

namespace Tomography.UI.windows;

public sealed partial class AppWindow
{
    private readonly EmtAcquisition _acquisition = new();
    private readonly AcquisitionRecords _acquisitionRecords = new();
    private CancellationTokenSource? _acquisitionStop;
    private bool _sampling;
    private AcquisitionMode _activeAcquisitionMode;
    private int _batchFrameTarget = 10;
    private bool _acquiringReference;
    private void StopAcquisition(object? sender, EventArgs e)
    { SetRealtimeReconstruction(false); _acquisitionStop?.Cancel(); }
    private async void AcquisitionRequested(object? sender, AcquisitionMode mode)
    {
        if (_sampling || _connecting || _fileOperation || (mode == AcquisitionMode.Reference && _reconstructing)) return;
        _activeAcquisitionMode = mode;
        if (mode == AcquisitionMode.Batch)
        {
            if (!IsInteger(BatchFrameCount.Value, 1, int.MaxValue))
            { ShowDeviceError("采集数量必须为正整数。"); return; }
            _batchFrameTarget = (int)BatchFrameCount.Value;
        }
        if (_protocolSettings?.Protocol == 0) { ShowDeviceError("默认协议仅显示原始接收数据。"); return; }
        if (mode == AcquisitionMode.Live && (_reconstruction.Reference is null || _reconstruction.SensitivityPath is null))
        { ShowDeviceError("实时采集成像需要先导入灵敏度矩阵并完成空场采集。"); return; }
        if (_protocolSettings?.Protocol == 2 && ActiveChannel.IsOpen)
        {
            if (_projectSettings.CustomFrame?.ChannelMap.Length != 64) { ShowDeviceError("当前自定义帧仅用于原始数据显示，未定义成像通道。"); return; }
            BeginStreamAcquisition(mode); return;
        }
        if (!_deviceReady) { DeviceView.SetState("请先手动握手", false, ActiveChannel.IsOpen, "请先连接设备，再点击握手，成功后进行采集。"); return; }
        if (_deviceSettings.CoilCount != 8) { ShowDeviceError("当前成像算法需要 8 线圈、64 通道；其他线圈数量可握手并查看原始数据。"); return; }
        if (mode == AcquisitionMode.Reference) SetRealtimeReconstruction(false);
        _acquiringReference = mode == AcquisitionMode.Reference;
        if (mode == AcquisitionMode.Live) SetRealtimeReconstruction(true);
        _sampling = true; _receivingEmtMeasurements = true; _deviceSettings.IsEnabled = false;
        using var stop = CancellationTokenSource.CreateLinkedTokenSource(_deviceStop?.Token ?? _connectionLifetime.Token);
        _acquisitionStop = stop;
        var referenceFrames = new List<uint[]>();
        var count = 0;
        try
        {
            if (!IsInteger(_deviceSettings.FrequencyKhz, 1, 500) || !IsInteger(_deviceSettings.CoilCount, 1, 32))
                throw new ArgumentException("激励频率必须为 1–500 kHz 的整数，线圈数必须为 1–32 的整数。");
            var result = await _acquisition.RunAsync(ActiveChannel, (int)_deviceSettings.FrequencyKhz, (int)_deviceSettings.CoilCount,
                mode, _batchFrameTarget, frame =>
                {
                    if (_connectionLifetime.IsCancellationRequested) return;
                    DeviceView.AppendEmtFrame(frame);
                    count++; if (mode != AcquisitionMode.Reference) Workspace.ShowChannels(frame);
                    if (mode == AcquisitionMode.Reference) referenceFrames.Add(frame.ToArray());
                    else
                    {
                        _acquisitionRecords.AddObject(frame);
                        _ = QueueRawDataSave(false, frame.Select(value => (double)value).ToArray());
                        _reconstruction.SetMeasurements(frame.Select(value => (double)value).ToArray());
                        QueueRealtimeReconstruction();
                    }
                    ConnectionStatus.Text = $"{mode}：已采集 {count} 帧";
                }, stop.Token, _deviceStop?.Token ?? _connectionLifetime.Token, _deviceSettings.ArrayType, _deviceSettings.ExcitationMode);
            if (_connectionLifetime.IsCancellationRequested) return;
            if (mode == AcquisitionMode.Reference && result.Data is double[] mean)
            {
                _reconstruction.SetReference(mean);
                _acquisitionRecords.SetReference(referenceFrames, mean);
                Workspace.ShowReference(mean);
                await QueueRawDataSave(true, mean);
            }
            await _rawDataSaveTail;
            if (_connectionLifetime.IsCancellationRequested) return;
            if (!result.IsSuccess) { CloseDevice(); ConnectionIcon.Glyph = "\uECCA"; }
            else if (count > 0) DeviceView.SetParameterStatus("本次 EMT 采样已收到应答；所用参数已由采样链路确认（无独立参数回读）。");
            if (result.IsSuccess) DeviceView.SetState(result.Message, false, ActiveChannel.IsOpen);
            else ShowDeviceError(string.IsNullOrWhiteSpace(result.Message) ? "采集失败，未收到有效的 256 字节数据帧。" : result.Message);
        }
        catch (Exception ex) { if (!_connectionLifetime.IsCancellationRequested) ShowDeviceError(ex.Message); }
        finally { if (mode == AcquisitionMode.Live) SetRealtimeReconstruction(false); _receivingEmtMeasurements = false; _acquiringReference = false; _sampling = false; _acquisitionStop = null; }
    }
    private static bool IsInteger(double value, int minimum, int maximum) => double.IsFinite(value)
        && value == Math.Truncate(value) && value >= minimum && value <= maximum;
}
