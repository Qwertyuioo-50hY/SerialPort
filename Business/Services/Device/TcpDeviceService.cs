using System.Text;
using Tomography.Data;
using Tomography.Business.Interfaces;
using Tomography.Business.Models;
using Tomography.Business.Models.Enums;

namespace Tomography.Business.Services;

public sealed class TcpDeviceService : IDeviceChannel, IDisposable
{
    private readonly TcpTransport _transport = new();
    public bool IsOpen => _transport.IsOpen;
    public event EventHandler<LogEntry>? DataReceived;
    public event EventHandler<string>? Disconnected;
    public TcpDeviceService()
    {
        _transport.DataReceived += (_, data) => DataReceived?.Invoke(this, new LogEntry { RawData = data });
        _transport.Disconnected += (_, reason) => Disconnected?.Invoke(this, reason);
    }
    public Task OpenAsync(string host, int port, int timeoutSeconds, CancellationToken token) =>
        _transport.OpenAsync(host, port, TimeSpan.FromSeconds(timeoutSeconds), token);
    public OperationResult SendText(string? text, string? encodingName = null, ChecksumType checksumType = ChecksumType.None, bool appendChecksum = false)
    {
        try
        {
            if (appendChecksum) throw new NotSupportedException("EMT TCP 指令不附加校验。");
            _transport.Write(Encoding.GetEncoding(encodingName ?? "ASCII").GetBytes(text ?? ""));
            return new OperationResult { IsSuccess = true };
        }
        catch (Exception ex) { return new OperationResult { Message = ex.Message, Exception = ex }; }
    }
    public void Close() => _transport.Close();
    public void Dispose() => _transport.Dispose();
}
