using System.Net.Sockets;
using Tomography.Data.Interfaces;

namespace Tomography.Data;

public sealed class TcpTransport : IByteTransport
{
    private TcpClient? _client;
    private CancellationTokenSource? _stop;
    private readonly SemaphoreSlim _writes = new(1, 1);
    public bool IsOpen => _client is not null;
    public event EventHandler<byte[]>? DataReceived;
    public event EventHandler<string>? Disconnected;
    public async Task OpenAsync(string host, int port, TimeSpan timeout, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(host) || port is < 1 or > 65535 || timeout <= TimeSpan.Zero)
            throw new ArgumentException("TCP 主机、端口或超时无效。");
        Close();
        var client = new TcpClient();
        using var connecting = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        connecting.CancelAfter(timeout);
        try { await client.ConnectAsync(host, port, connecting.Token).ConfigureAwait(false); }
        catch { client.Dispose(); throw; }
        _client = client;
        _stop = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _ = ReceiveAsync(client, _stop.Token);
    }
    private async Task ReceiveAsync(TcpClient client, CancellationToken token)
    {
        var reason = "TCP 对端已断开。";
        try
        {
            var buffer = new byte[4096];
            while (!token.IsCancellationRequested)
            {
                var count = await client.GetStream().ReadAsync(buffer, token).ConfigureAwait(false);
                if (count == 0) break;
                DataReceived?.Invoke(this, buffer.AsSpan(0, count).ToArray());
            }
        }
        catch (Exception ex) when (ex is IOException or SocketException or OperationCanceledException or ObjectDisposedException)
        { reason = token.IsCancellationRequested ? "TCP 已断开。" : ex.Message; }
        finally
        {
            if (ReferenceEquals(Interlocked.CompareExchange(ref _client, null, client), client))
            {
                client.Dispose();
                if (!token.IsCancellationRequested) Disconnected?.Invoke(this, reason);
            }
        }
    }
    public void Write(byte[] data)
    {
        _writes.Wait();
        try { (_client ?? throw new InvalidOperationException("TCP 未连接。")).GetStream().Write(data); }
        finally { _writes.Release(); }
    }
    public void Close()
    {
        _stop?.Cancel();
        Interlocked.Exchange(ref _client, null)?.Dispose();
        _stop?.Dispose(); _stop = null;
    }
    public void Dispose() => Close();
}
