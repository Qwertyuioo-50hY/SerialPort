using Tomography.Business.Services;
using Tomography.UI.Controls;

namespace Tomography.UI.Views;

public sealed partial class AppWindow
{
    private readonly SerialService _serialConnection = new();
    private readonly CancellationTokenSource _connectionLifetime = new();
    private ConnectionProtocolView? _protocolSettings;
    private bool _connecting;
    private bool _deviceReady;

    private async void ToggleDeviceConnection()
    {
        if (_connecting || _sampling || _reconstructing) return;
        if (_serialConnection.IsOpen)
        {
            _deviceReady = false;
            ConnectionStatus.Text = _serialConnection.Close().Message;
            ConnectionIcon.Glyph = "\uECCA";
            return;
        }
        if (_protocolSettings?.UsesSerialDevice != true)
        {
            ConnectionStatus.Text = "当前仅串口实际设备支持连接";
            return;
        }
        _connecting = true;
        try
        {
            var result = _serialConnection.Open(_protocolSettings.GetSerialConfig());
            if (result.IsSuccess)
            {
                ConnectionStatus.Text = "串口已打开，正在等待 EMT 握手…";
                result = await EmtHandshake.ExecuteAsync(_serialConnection, _connectionLifetime.Token);
                if (_connectionLifetime.IsCancellationRequested) return;
                _deviceReady = result.IsSuccess;
                if (!_deviceReady) _serialConnection.Close();
            }
            ConnectionIcon.Glyph = _deviceReady ? "\uECCB" : "\uECCA";
            ConnectionStatus.Text = result.Message;
        }
        finally
        {
            _connecting = false;
        }
    }
}
