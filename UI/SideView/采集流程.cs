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
    private void StopAcquisition(object? sender, EventArgs e) => _acquisitionStop?.Cancel();
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
        if (_protocolSettings?.IsSimulation == true)
        {
            if (!_projectSettings.ExperimentInputs.TryGetValue("物场/Demo数据", out var input)) { ShowDeviceError("请先导入仿真数据。"); return; }
            try { _reconstruction.SetMeasurements(ExperimentInputValidator.ReadChannels(input));
                if (mode == AcquisitionMode.Reference) _reconstruction.CaptureReference();
                if (mode == AcquisitionMode.Reference) Workspace.ShowReference(_reconstruction.Reference!); else Workspace.ShowMeasurements(_reconstruction.Measurements!); ConnectionStatus.Text = "仿真数据已载入；未操作实物设备"; }
            catch (Exception ex) { ShowDeviceError(ex.Message); }
            return;
        }
        if (_protocolSettings?.Protocol is 1 or 2 && ActiveChannel.IsOpen) { BeginStreamAcquisition(mode); return; }
        if (!_deviceReady) { ConnectionStatus.Text = "请先连接设备并完成握手"; return; }
        if (mode == AcquisitionMode.Reference) SetRealtimeReconstruction(false);
        _acquiringReference = mode == AcquisitionMode.Reference;
        _sampling = true; _deviceSettings.IsEnabled = false;
        using var stop = CancellationTokenSource.CreateLinkedTokenSource(_deviceStop?.Token ?? _connectionLifetime.Token);
        _acquisitionStop = stop;
        var referenceFrames = new List<uint[]>();
        var count = 0;
        try
        {
            if (!IsInteger(_deviceSettings.FrequencyKhz, 1, 999) || !IsInteger(_deviceSettings.CoilCount, 1, 99))
                throw new ArgumentException("激励频率必须为 1–999 kHz 的整数，线圈数必须为 1–99 的整数。");
            var result = await _acquisition.RunAsync(ActiveChannel, (int)_deviceSettings.FrequencyKhz, (int)_deviceSettings.CoilCount,
                mode, _batchFrameTarget, frame =>
                {
                    if (_connectionLifetime.IsCancellationRequested) return;
                    count++; if (mode != AcquisitionMode.Reference) Workspace.ShowChannels(frame);
                    if (mode == AcquisitionMode.Reference) referenceFrames.Add(frame.ToArray());
                    else
                    {
                        _acquisitionRecords.AddObject(frame);
                        _reconstruction.SetMeasurements(frame.Select(value => (double)value).ToArray());
                        QueueRealtimeReconstruction();
                    }
                    ConnectionStatus.Text = $"{mode}：已采集 {count} 帧";
                }, stop.Token, _deviceStop?.Token ?? _connectionLifetime.Token);
            if (_connectionLifetime.IsCancellationRequested) return;
            if (mode == AcquisitionMode.Reference && result.Data is double[] mean)
            {
                _reconstruction.SetReference(mean);
                _acquisitionRecords.SetReference(referenceFrames, mean);
                Workspace.ShowReference(mean);
            }
            if (!result.IsSuccess) { CloseDevice(); ConnectionIcon.Glyph = "\uECCA"; }
            else if (count > 0) DeviceView.SetParameterStatus("本次 EMT 采样已收到应答；所用参数已由采样链路确认（无独立参数回读）。");
            ConnectionStatus.Text = result.Message;
        }
        catch (Exception ex) { if (!_connectionLifetime.IsCancellationRequested) ConnectionStatus.Text = ex.Message; }
        finally { _acquiringReference = false; _sampling = false; _acquisitionStop = null; }
    }
    private static bool IsInteger(double value, int minimum, int maximum) => double.IsFinite(value)
        && value == Math.Truncate(value) && value >= minimum && value <= maximum;
}
