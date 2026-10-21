using Tomography.Business.Services;
using Tomography.Business.Interfaces;
using Tomography.Business.Models;
using Tomography.UI.SideView;

namespace Tomography.UI.windows;

public sealed partial class AppWindow
{
    private readonly SerialService _serialConnection = new();
    private readonly TcpDeviceService _tcpConnection = new();
    private readonly NetworkListenerService _networkConnection = new();
    private readonly CancellationTokenSource _connectionLifetime = new();
    private CancellationTokenSource? _deviceStop;
    private ConnectionProtocolView? _protocolSettings;
    private IDeviceChannel? _selectedChannel;
    private IDeviceChannel ActiveChannel => _selectedChannel ?? _serialConnection;
    private readonly object _decoderLock = new();
    private CustomFrameDecoder? _customFrame;
    private bool _connecting;
    private bool _deviceReady;
    private void CloseDevice()
    {
        _deviceStop?.Cancel(); _acquisitionStop?.Cancel(); _deviceReady = false;
        if (_customFrame is not null) EndStreamAcquisition();
        if (_selectedChannel is not null) _selectedChannel.DataReceived -= DeviceBytesReceived;
        _selectedChannel = null;
        _serialConnection.Close(); _tcpConnection.Close(); _networkConnection.Close();
        lock (_decoderLock) _customFrame = null;
        _deviceStop?.Dispose(); _deviceStop = null;
        _protocolSettings?.SetBusy(false); _deviceSettings.IsEnabled = true; DeviceView.FrameEditor.IsEnabled = true;
        DeviceView.SetInputState(false);
        DeviceView.SetState("未连接", false, false);
    }
    private async void ToggleDeviceConnection()
    {
        if (_connecting) { _deviceStop?.Cancel(); return; }
        if (ActiveChannel.IsOpen) { CloseDevice(); return; }
        if (_sampling || _reconstructing || _fileOperation) return;
        _connecting = true; _deviceStop = CancellationTokenSource.CreateLinkedTokenSource(_connectionLifetime.Token);
        var token = _deviceStop.Token;
        _protocolSettings!.SetBusy(true); _deviceSettings.IsEnabled = false; DeviceView.FrameEditor.IsEnabled = false;
        DeviceView.SetState("连接中", true, false);
        try
        {
            var settings = _protocolSettings.ReadSettings((int)_deviceSettings.FrequencyKhz, (int)_deviceSettings.CoilCount);
            _customFrame = _protocolSettings.Protocol == 2 ? new CustomFrameDecoder(_projectSettings.CustomFrame ?? throw new ArgumentException("请先保存自定义帧配置。")) : null;
            _selectedChannel = _protocolSettings.Transport switch { 0 => _serialConnection, 2 => _tcpConnection, _ => _networkConnection };
            _selectedChannel.DataReceived += DeviceBytesReceived;
            if (_protocolSettings.Transport == 0)
            {
                var config = _protocolSettings.GetSerialConfig();
                var opened = await Task.Run(() => _serialConnection.Open(config));
                if (!opened.IsSuccess) throw new InvalidOperationException(opened.Message, opened.Exception);
            }
            else if (_protocolSettings.Transport == 2)
                await _tcpConnection.OpenAsync(settings.Host, settings.Port, settings.TimeoutSeconds, token);
            else
                await _networkConnection.OpenAsync(_protocolSettings.Transport == 1, settings.Host, settings.Port, settings.LocalHost, settings.LocalPort, token);
            token.ThrowIfCancellationRequested();
            DeviceView.SetState(_protocolSettings.Transport == 3 ? "监听中，等待客户端" : "已连接", false, true);
        }
        catch (Exception ex)
        {
            CloseDevice();
            if (!_connectionLifetime.IsCancellationRequested) DeviceView.SetState("未连接", false, false, ex is OperationCanceledException ? "连接已取消" : ex.Message);
        }
        finally { _connecting = false; }
    }
    private async Task RepeatDeviceHandshakeAsync()
    {
        if (_connecting || _sampling || _reconstructing || _fileOperation) return;
        if (!ActiveChannel.IsOpen) { ShowDeviceError("请先连接设备。"); return; }
        if (_protocolSettings!.Protocol != 1) { ShowDeviceError("握手仅用于 EMT协议。"); return; }
        _connecting = true; _deviceReady = false;
        var channel = ActiveChannel;
        var token = _deviceStop?.Token ?? _connectionLifetime.Token;
        DeviceView.SetState("握手中", true, true);
        try
        {
            if (!IsInteger(_deviceSettings.FrequencyKhz, 1, 999) || !IsInteger(_deviceSettings.CoilCount, 1, 15)) throw new ArgumentException("激励频率需为 1–999 kHz，线圈数需为 1–15 的整数。");
            var result = await EmtSampling.ExecuteAsync(channel, (int)_deviceSettings.FrequencyKhz, (int)_deviceSettings.CoilCount, token);
            if (!result.IsSuccess) throw new InvalidOperationException(result.Message, result.Exception);
            token.ThrowIfCancellationRequested(); _deviceReady = true;
            DeviceView.SetState("握手成功", false, true);
        }
        catch (Exception ex)
        {
            CloseDevice();
            if (!_connectionLifetime.IsCancellationRequested) DeviceView.SetState("未连接", false, false, ex is OperationCanceledException ? null : ex.Message);
        }
        finally { _connecting = false; }
    }
    private void DeviceBytesReceived(object? sender, LogEntry entry)
    {
        if (!ReferenceEquals(sender, _selectedChannel)) return;
        var bytes = entry.RawData.ToArray();
        DispatcherQueue.TryEnqueue(() =>
        {
            if (ReferenceEquals(sender, _selectedChannel) && !_connectionLifetime.IsCancellationRequested)
                DeviceView.AppendReceived(bytes, _protocolSettings!.ReceiveEncoding);
        });
        lock (_decoderLock)
        {
            if (_customFrame is null) return;
            try
            {
                var accepted = _customFrame.AcceptedFrames; var rejected = _customFrame.RejectedFrames;
                foreach (var frame in _customFrame.Feed(bytes))
                    DispatcherQueue.TryEnqueue(() =>
                    {
                        if (ReferenceEquals(sender, _selectedChannel) && !_connectionLifetime.IsCancellationRequested) AcceptStreamFrame(frame);
                    });
                if (accepted != _customFrame.AcceptedFrames || rejected != _customFrame.RejectedFrames)
                {
                    var status = $"有效帧 {_customFrame.AcceptedFrames} · 校验异常 {_customFrame.RejectedFrames}";
                    DispatcherQueue.TryEnqueue(() => { if (ReferenceEquals(sender, _selectedChannel)) DeviceView.FrameEditor.SetStatus(status); });
                }
            }
            catch (Exception ex) { DispatcherQueue.TryEnqueue(() => ShowDeviceError(ex.Message)); }
        }
    }
}
