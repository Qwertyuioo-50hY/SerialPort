using System.Net;
using System.Net.Sockets;
using Tomography.Data.Interfaces;

namespace Tomography.Data;

/// <summary>UDP 显式绑定本地端点，向配置的远端发送；按原始字节转发接收报文。</summary>
public sealed class UdpTransport : IByteTransport
{
    private UdpClient? _client;
    private CancellationTokenSource? _stop;
    private IPEndPoint? _remote;
    public bool IsOpen => _client is not null;
    public event EventHandler<byte[]>? DataReceived;
    public event EventHandler<string>? Disconnected;
    public async Task OpenAsync(string localHost, int localPort, string host, int port, CancellationToken token)
    {
        Close();
        var address = IPAddress.Parse(localHost);
        var remote = (await Dns.GetHostAddressesAsync(host, token)).First(ip => ip.AddressFamily == address.AddressFamily);
        token.ThrowIfCancellationRequested();
        var client = new UdpClient(new IPEndPoint(address, localPort));
        _remote = new IPEndPoint(remote, port); _client = client;
        _stop = CancellationTokenSource.CreateLinkedTokenSource(token);
        _ = ReceiveAsync(client, _stop.Token);
    }
    private async Task ReceiveAsync(UdpClient client, CancellationToken token)
    {
        try
        {
            while (!token.IsCancellationRequested)
            {
                var packet = await client.ReceiveAsync(token).ConfigureAwait(false);
                if (packet.RemoteEndPoint.Equals(_remote)) DataReceived?.Invoke(this, packet.Buffer);
            }
        }
        catch (Exception ex) when (ex is SocketException or OperationCanceledException or ObjectDisposedException)
        { if (!token.IsCancellationRequested) Disconnected?.Invoke(this, ex.Message); }
    }
    public void Write(byte[] data) => (_client ?? throw new InvalidOperationException("UDP 未连接。")).Send(data, data.Length, _remote!);
    public void Close() { _stop?.Cancel(); _client?.Dispose(); _client = null; _stop?.Dispose(); _stop = null; }
    public void Dispose() => Close();
}
