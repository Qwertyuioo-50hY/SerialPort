using System.Text;
using Tomography.Business.Interfaces;
using Tomography.Business.Models;
using Tomography.Business.Models.Enums;
using Tomography.Data;
using Tomography.Data.Interfaces;

namespace Tomography.Business.Services;

public sealed class NetworkListenerService : IDeviceChannel, IDisposable
{
    private IByteTransport? _transport;
    public bool IsOpen => _transport?.IsOpen == true;
    public event EventHandler<LogEntry>? DataReceived;
    public event EventHandler<string>? Disconnected;
    public async Task OpenAsync(bool udp, string host, int port, string localHost, int localPort, CancellationToken token)
    {
        Close();
        if (udp)
        {
            var transport = new UdpTransport(); _transport = transport;
            transport.DataReceived += Forward; transport.Disconnected += ForwardDisconnect;
            await transport.OpenAsync(localHost, localPort, host, port, token);
        }
        else
        {
            var transport = new TcpServerTransport(); _transport = transport;
            transport.DataReceived += Forward; transport.Disconnected += ForwardDisconnect;
            transport.Open(host, port, token);
        }
    }
    private void Forward(object? sender, byte[] bytes) => DataReceived?.Invoke(this, new LogEntry { RawData = bytes });
    private void ForwardDisconnect(object? sender, string reason) => Disconnected?.Invoke(this, reason);
    public OperationResult SendText(string? text, string? encodingName = null, ChecksumType checksumType = ChecksumType.None, bool appendChecksum = false)
    {
        try
        {
            if (appendChecksum) throw new NotSupportedException("网络文本命令不附加校验。");
            (_transport ?? throw new InvalidOperationException("网络未连接。")).Write(Encoding.GetEncoding(encodingName ?? "ASCII").GetBytes(text ?? ""));
            return new() { IsSuccess = true };
        }
        catch (Exception ex) { return new() { Message = ex.Message, Exception = ex }; }
    }
    public void Close() { _transport?.Dispose(); _transport = null; }
    public void Dispose() => Close();
}
