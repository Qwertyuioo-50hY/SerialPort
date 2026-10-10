using Tomography.Business.Models;
using Tomography.Business.Services;

namespace Tomography.UI.windows;

public sealed partial class AppWindow
{
    private readonly EmtAcquisition _acquisition = new();
    private readonly AcquisitionRecords _acquisitionRecords = new();
    private CancellationTokenSource? _acquisitionStop;
    private bool _sampling;
    private bool _acquiringReference;
    private void StopAcquisition(object? sender, EventArgs e) => _acquisitionStop?.Cancel();
    private async void AcquisitionRequested(object? sender, AcquisitionMode mode)
    {
        if (_sampling || _connecting || _fileOperation || (mode == AcquisitionMode.Reference && _reconstructing)) return;
        if (!_deviceReady) { ConnectionStatus.Text = "请先连接设备并完成握手"; return; }
        if (mode == AcquisitionMode.Reference) SetRealtimeReconstruction(false);
        _acquiringReference = mode == AcquisitionMode.Reference;
        _sampling = true;
        using var stop = CancellationTokenSource.CreateLinkedTokenSource(_connectionLifetime.Token);
        _acquisitionStop = stop;
        var referenceFrames = new List<uint[]>();
        var count = 0;
        try
        {
            var result = await _acquisition.RunAsync(_serialConnection, 100, 8,
                mode, 10, frame =>
                {
                    if (_connectionLifetime.IsCancellationRequested) return;
                    count++; Workspace.ShowChannels(frame);
                    if (mode == AcquisitionMode.Reference) referenceFrames.Add(frame.ToArray());
                    else
                    {
                        _acquisitionRecords.AddObject(frame);
                        _reconstruction.SetMeasurements(frame.Select(value => (double)value).ToArray());
                        QueueRealtimeReconstruction();
                    }
                    ConnectionStatus.Text = $"{mode}：已采集 {count} 帧";
                }, stop.Token, _connectionLifetime.Token);
            if (_connectionLifetime.IsCancellationRequested) return;
            if (mode == AcquisitionMode.Reference && result.Data is double[] mean)
            {
                _reconstruction.SetReference(mean);
                _acquisitionRecords.SetReference(referenceFrames, mean);
                Workspace.ShowMeasurements(mean);
            }
            if (!result.IsSuccess) { _deviceReady = false; _serialConnection.Close(); ConnectionIcon.Glyph = "\uECCA"; }
            ConnectionStatus.Text = result.Message;
        }
        catch (Exception ex) { if (!_connectionLifetime.IsCancellationRequested) ConnectionStatus.Text = ex.Message; }
        finally { _acquiringReference = false; _sampling = false; _acquisitionStop = null; }
    }
    private static bool IsInteger(double value, int minimum, int maximum) => double.IsFinite(value)
        && value == Math.Truncate(value) && value >= minimum && value <= maximum;
}
