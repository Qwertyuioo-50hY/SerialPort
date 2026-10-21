using Tomography.Business.Models;
using Microsoft.UI.Xaml;

namespace Tomography.UI.windows;

public sealed partial class AppWindow
{
    private AcquisitionMode _streamMode;
    private readonly List<double[]> _streamReference = new();
    private int _streamCount;
    private readonly DispatcherTimer _streamTimeout = new() { Interval = TimeSpan.FromSeconds(3) };
    private void BeginStreamAcquisition(AcquisitionMode mode)
    {
        _streamMode = mode; _streamCount = 0; _streamReference.Clear(); _sampling = true;
        _acquiringReference = mode == AcquisitionMode.Reference;
        _deviceSettings.IsEnabled = false;
        _streamTimeout.Stop(); _streamTimeout.Tick -= StreamTimedOut; _streamTimeout.Tick += StreamTimedOut;
        _streamTimeout.Start(); ConnectionStatus.Text = "等待 数值流 数据流；未发送 EMT 采样指令";
    }
    private void StreamTimedOut(object? sender, object e) { EndStreamAcquisition(); CloseDevice(); ShowDeviceError("数值流 数据接收超时（3 秒）。"); }
    private void EndStreamAcquisition()
    { _sampling = false; _acquiringReference = false; _streamTimeout.Stop(); _streamReference.Clear(); }
    private void AcceptStreamFrame(double[] frame)
    {
        if (!_sampling) return;
        _streamTimeout.Stop(); _streamTimeout.Start(); _streamCount++;
        if (_streamMode == AcquisitionMode.Reference)
        {
            _streamReference.Add(frame.ToArray());
            if (_streamReference.Count == 10)
            {
                var mean = Enumerable.Range(0, 64).Select(i => _streamReference.Average(row => row[i])).ToArray();
                _reconstruction.SetReference(mean); _acquisitionRecords.SetReference(_streamReference, mean); Workspace.ShowReference(mean);
                EndStreamAcquisition(); DeviceView.SetParameterStatus("数值流 空场参考已更新；不代表下位机 EMT 参数已应用。");
            }
        }
        else
        {
            _reconstruction.SetMeasurements(frame); Workspace.ShowMeasurements(frame); _acquisitionRecords.AddObject(frame); QueueRealtimeReconstruction();
            if (_streamMode == AcquisitionMode.Single || (_streamMode == AcquisitionMode.Batch && _streamCount >= _batchFrameTarget)) EndStreamAcquisition();
        }
    }
    private void AcquisitionButtonClicked(object sender, RoutedEventArgs e)
    {
        var command = (string)((Microsoft.UI.Xaml.Controls.Button)sender).Tag;
        if (command == "Stop" || (command == "Live" && _sampling && _activeAcquisitionMode == AcquisitionMode.Live)) { if (_customFrame is not null) EndStreamAcquisition(); else StopAcquisition(sender, EventArgs.Empty); }
        else AcquisitionRequested(sender, Enum.Parse<AcquisitionMode>(command));
    }
}
