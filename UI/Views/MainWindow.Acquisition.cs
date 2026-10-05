using Microsoft.UI.Xaml.Controls;
using Tomography.Business.Models;
using Tomography.Business.Services;
using Tomography.UI.Controls;

namespace Tomography.UI.Views;

public sealed partial class MainWindow
{
    private readonly EmtAcquisition _acquisition = new();
    private readonly AcquisitionRecords _acquisitionRecords = new();
    private AcquisitionSettingsView? _acquisitionSettings;
    private CancellationTokenSource? _acquisitionStop;
    private bool _sampling;
    private bool _acquiringReference;
    private void AddAcquisitionSettings(Panel parent)
    {
        if (_acquisitionSettings is null)
        {
            _acquisitionSettings = new AcquisitionSettingsView();
            _acquisitionSettings.AcquisitionRequested += AcquisitionRequested;
            _acquisitionSettings.StopRequested += StopAcquisition;
        }
        parent.Children.Add(_acquisitionSettings);
    }
    private void StopAcquisition(object? sender, EventArgs e) => _acquisitionStop?.Cancel();
    private async void AcquisitionRequested(object? sender, AcquisitionMode mode)
    {
        if (_sampling || _connecting || _fileOperation || (mode == AcquisitionMode.Reference && _reconstructing)) return;
        if (!_deviceReady) { ConnectionStatus.Text = "请先连接设备并完成握手"; return; }
        if (_acquisitionSettings is null) { ViewModel.Section = "接口/协议"; ViewModel.ParametersVisible = true; return; }
        var settings = _acquisitionSettings;
        if (!IsInteger(settings.FrequencyKhz, 1, 999) || !IsInteger(settings.CoilCount, 1, 99)
            || !IsInteger(settings.FrameCount, 1, 10000))
        { ConnectionStatus.Text = "频率、线圈数量及帧数必须为范围内整数"; return; }
        if (mode == AcquisitionMode.Reference) _reconstructionSettings?.StopRealtime();
        _acquiringReference = mode == AcquisitionMode.Reference;
        _sampling = true; settings.SetBusy(true);
        using var stop = CancellationTokenSource.CreateLinkedTokenSource(_connectionLifetime.Token);
        _acquisitionStop = stop;
        var referenceFrames = new List<uint[]>();
        var count = 0;
        try
        {
            var result = await _acquisition.RunAsync(_serialConnection, (int)settings.FrequencyKhz, (int)settings.CoilCount,
                mode, (int)settings.FrameCount, frame =>
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
                    _reconstructionSettings?.UpdateInputs(_reconstruction);
                    ConnectionStatus.Text = $"{mode}：已采集 {count} 帧";
                }, stop.Token, _connectionLifetime.Token);
            if (_connectionLifetime.IsCancellationRequested) return;
            if (mode == AcquisitionMode.Reference && result.Data is double[] mean)
            {
                _reconstruction.SetReference(mean);
                _acquisitionRecords.SetReference(referenceFrames, mean);
                Workspace.ShowMeasurements(mean);
                _reconstructionSettings?.UpdateInputs(_reconstruction);
            }
            if (!result.IsSuccess) { _deviceReady = false; _serialConnection.Close(); ConnectionIcon.Glyph = "\uECCA"; }
            ConnectionStatus.Text = result.Message;
        }
        catch (Exception ex) { if (!_connectionLifetime.IsCancellationRequested) ConnectionStatus.Text = ex.Message; }
        finally { _acquiringReference = false; _sampling = false; _acquisitionStop = null; if (!_connectionLifetime.IsCancellationRequested) settings.SetBusy(false); }
    }
    private static bool IsInteger(double value, int minimum, int maximum) => double.IsFinite(value)
        && value == Math.Truncate(value) && value >= minimum && value <= maximum;
}
