using System.Net;
using System.Net.Sockets;
using Tomography.Data.Interfaces;

namespace Tomography.Data;

/// <summary>单客户端 TCP 监听；对端断开后继续等待下一次连接。</summary>
public sealed class TcpServerTransport : IByteTransport
{
    private TcpListener? _listener;
    private TcpClient? _client;
    private CancellationTokenSource? _stop;
    public bool IsOpen => _listener is not null;
    public event EventHandler<byte[]>? DataReceived;
    public event EventHandler<string>? Disconnected;
    public void Open(string host, int port, CancellationToken token)
    {
        Close(); token.ThrowIfCancellationRequested();
        var listener = new TcpListener(IPAddress.Parse(host), port);
        listener.Start(1); _listener = listener;
        _stop = CancellationTokenSource.CreateLinkedTokenSource(token);
        _ = AcceptAsync(listener, _stop.Token);
    }
    private async Task AcceptAsync(TcpListener listener, CancellationToken token)
    {
        try
        {
            var buffer = new byte[4096];
            while (!token.IsCancellationRequested)
            {
                using var client = await listener.AcceptTcpClientAsync(token).ConfigureAwait(false);
                _client = client;
                try
                {
                    while (!token.IsCancellationRequested)
                    {
                        var size = await client.GetStream().ReadAsync(buffer, token).ConfigureAwait(false);
                        if (size == 0) break;
                        DataReceived?.Invoke(this, buffer.AsSpan(0, size).ToArray());
                    }
                }
                catch (Exception ex) when (ex is IOException or SocketException) { }
                finally { Interlocked.CompareExchange(ref _client, null, client); }
            }
        }
        catch (Exception ex) when (ex is SocketException or OperationCanceledException or ObjectDisposedException)
        { if (!token.IsCancellationRequested) Disconnected?.Invoke(this, ex.Message); }
    }
    public void Write(byte[] data) => (_client ?? throw new InvalidOperationException("尚无 TCP 客户端连接。")).GetStream().Write(data);
    public void Close()
    {
        _stop?.Cancel(); _listener?.Stop(); _listener = null;
        Interlocked.Exchange(ref _client, null)?.Dispose(); _stop?.Dispose(); _stop = null;
    }
    public void Dispose() => Close();
}
