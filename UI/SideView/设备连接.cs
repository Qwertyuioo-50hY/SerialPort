using Tomography.Business.Services;
using Tomography.Business.Interfaces;
using Tomography.Business.Models;
using Tomography.UI.SideView;

namespace Tomography.UI.windows;

public sealed partial class AppWindow
{
    private readonly SerialService _serialConnection = new();
    private readonly TcpDeviceService _tcpConnection = new();
    private readonly CancellationTokenSource _connectionLifetime = new();
    private CancellationTokenSource? _deviceStop;
    private ConnectionProtocolView? _protocolSettings;
    private IDeviceChannel? _selectedChannel;
    private IDeviceChannel ActiveChannel => _selectedChannel ?? _serialConnection;
    private readonly Queue<string> _deviceDiagnostics = new();
    private readonly object _decoderLock = new();
    private FireWaterDecoder? _fireWater;
    private CustomFrameDecoder? _customFrame;
    private bool _connecting;
    private bool _deviceReady;
    private void CloseDevice()
    {
        _deviceStop?.Cancel(); _acquisitionStop?.Cancel(); _deviceReady = false;
        if (_fireWater is not null || _customFrame is not null) EndStreamAcquisition();
        if (_selectedChannel is not null) _selectedChannel.DataReceived -= DeviceBytesReceived;
        _serialConnection.Close(); _tcpConnection.Close(); _selectedChannel = null;
        _deviceStop?.Dispose(); _deviceStop = null;
        _protocolSettings?.SetBusy(false); _deviceSettings.IsEnabled = true;
        DeviceView.SetState("未连接", false, false);
    }
    private async void ToggleDeviceConnection()
    {
        if (_connecting) { _deviceStop?.Cancel(); return; }
        if (ActiveChannel.IsOpen) { CloseDevice(); return; }
        if (_sampling || _reconstructing || _fileOperation) return;
        if (_protocolSettings!.IsSimulation) { UpdateDeviceInputs(); return; }
        if (_protocolSettings.Protocol == 2 && _projectSettings.CustomFrame is null) { ShowDeviceError("缺少自定义固件帧定义，不能连接未知协议。"); return; }
        _connecting = true; _deviceStop = CancellationTokenSource.CreateLinkedTokenSource(_connectionLifetime.Token);
        var token = _deviceStop.Token;
        _protocolSettings.SetBusy(true); _deviceSettings.IsEnabled = false;
        DeviceView.SetState("连接中", true, false);
        try
        {
            if (_protocolSettings.UsesTcpDevice)
            {
                if (!IsInteger(_protocolSettings.Port, 1, 65535) || !IsInteger(_protocolSettings.TimeoutSeconds, 1, 60))
                    throw new ArgumentException("TCP 端口和超时必须在允许范围内，且为整数。");
                _selectedChannel = _tcpConnection;
                await _tcpConnection.OpenAsync(_protocolSettings.Host, (int)_protocolSettings.Port, (int)_protocolSettings.TimeoutSeconds, token);
            }
            else
            {
                _selectedChannel = _serialConnection;
                var config = _protocolSettings.GetSerialConfig();
                var opened = await Task.Run(() => _serialConnection.Open(config));
                if (!opened.IsSuccess) throw new InvalidOperationException(opened.Message, opened.Exception);
            }
            token.ThrowIfCancellationRequested();
            _fireWater = _protocolSettings.Protocol == 1 ? new FireWaterDecoder() : null;
            _customFrame = _protocolSettings.Protocol == 2 ? new CustomFrameDecoder(_projectSettings.CustomFrame!) : null;
            _selectedChannel.DataReceived += DeviceBytesReceived;
            if (_fireWater is not null || _customFrame is not null)
            { DeviceView.SetState("流式接口已打开，等待合法 64 通道数据；设备身份未识别", false, true); return; }
            DeviceView.SetState("识别中：等待 EMT 握手", true, true);
            _deviceDiagnostics.Enqueue("TX: 776869636820656D740D0A (which emt)");
            var result = await EmtHandshake.ExecuteAsync(_selectedChannel, token);
            if (!result.IsSuccess) throw new InvalidOperationException(result.Message, result.Exception);
            _deviceReady = true;
            DeviceView.SetState("EMT 已连接：握手通过；未提供型号、序列号或固件版本", false, true);
        }
        catch (Exception ex)
        {
            CloseDevice();
            if (!_connectionLifetime.IsCancellationRequested)
                DeviceView.SetState("未连接", false, false, ex is OperationCanceledException ? "连接已取消" : ex.Message);
        }
        finally { _connecting = false; if (!ActiveChannel.IsOpen) { _protocolSettings.SetBusy(false); _deviceSettings.IsEnabled = true; } }
    }
    private void DeviceBytesReceived(object? sender, LogEntry entry)
    {
        if (!ReferenceEquals(sender, _selectedChannel)) return;
        lock (_decoderLock)
        {
            var raw = Convert.ToHexString(entry.RawData.AsSpan(0, Math.Min(entry.RawData.Length, 256)));
            DispatcherQueue.TryEnqueue(() => { if (_deviceDiagnostics.Count >= 100) _deviceDiagnostics.Dequeue(); _deviceDiagnostics.Enqueue($"RX ({entry.RawData.Length} bytes): {raw}"); });
            if (_fireWater is null && _customFrame is null) return;
            try
            {
                foreach (var frame in _fireWater is not null ? _fireWater.Feed(entry.RawData) : _customFrame!.Feed(entry.RawData))
                    DispatcherQueue.TryEnqueue(() =>
                    {
                        if (!ReferenceEquals(sender, _selectedChannel) || _connectionLifetime.IsCancellationRequested) return;
                        Workspace.ShowMeasurements(frame);
                        AcceptStreamFrame(frame);
                        DeviceView.SetState("数据流有效：64 通道；设备身份未验证", false, true);
                    });
            }
            catch (Exception ex) { DispatcherQueue.TryEnqueue(() => { CloseDevice(); ShowDeviceError(ex.Message); }); }
        }
    }
}
