using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Buffers.Binary;
using Tomography.Business.Services;
using Tomography.Business.Interfaces;
using Tomography.Business.Models;
using Tomography.Business.Models.Enums;
using Tomography.Data;

public static class DeviceTransportTests
{
    public static async Task RunAsync()
    {
        var fake = new FakeChannel();
        fake.Response = bytes => { var expected = Encoding.ASCII.GetBytes("emt ready\r\n"); fake.Emit(expected[..4]); fake.Emit(expected[4..]); };
        if (!(await EmtHandshake.ExecuteAsync(fake)).IsSuccess || fake.Last != "which emt\r\n") throw new Exception("split handshake");
        fake.Response = _ => fake.Emit(Encoding.ASCII.GetBytes("other ready\r\n"));
        if ((await EmtHandshake.ExecuteAsync(fake)).IsSuccess) throw new Exception("wrong device marked ready");
        fake.Response = _ => { }; using var cancel = new CancellationTokenSource(); cancel.Cancel();
        if ((await EmtHandshake.ExecuteAsync(fake, cancel.Token)).IsSuccess) throw new Exception("cancelled handshake marked ready");
        if ((await EmtHandshake.ExecuteAsync(fake)).IsSuccess) throw new Exception("timeout handshake marked ready");
        if (fake.SubscriberCount != 0) throw new Exception("leaked handshake subscription");
        var binary = new byte[256]; for (var i = 0; i < 64; i++) BinaryPrimitives.WriteUInt32LittleEndian(binary.AsSpan(4 * i), (uint)(i + 1));
        fake.Response = _ => { fake.Emit(binary[..13]); fake.Emit(binary[13..]); };
        var sampled = await EmtSampling.ExecuteAsync(fake, 100, 8);
        if (!sampled.IsSuccess || ((uint[])sampled.Data!)[63] != 64 || fake.Last != "10008sample\r\n") throw new Exception("split binary sample");
        foreach (var target in new[] { 1, 3, 12 })
        {
            var received = 0;
            var result = await new EmtAcquisition().RunAsync(fake, 100, 8, AcquisitionMode.Batch,
                target, _ => received++, CancellationToken.None);
            if (!result.IsSuccess || received != target || fake.SubscriberCount != 0)
                throw new Exception($"batch target {target}: received {received}");
        }
        fake.Response = _ => fake.Emit(new byte[257]); if ((await EmtSampling.ExecuteAsync(fake, 100, 8)).IsSuccess) throw new Exception("ambiguous binary overflow accepted");
        if (fake.SubscriberCount != 0) throw new Exception("leaked sample subscription");
        var listener = new TcpListener(IPAddress.Loopback, 0); listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        try
        {
            var server = Task.Run(async () =>
            {
                using var peer = await listener.AcceptTcpClientAsync(); var stream = peer.GetStream();
                if (await ReadLine(stream) != "which emt\r\n") throw new Exception("TCP handshake request");
                await stream.WriteAsync(Encoding.ASCII.GetBytes("emt ")); await Task.Delay(15); await stream.WriteAsync(Encoding.ASCII.GetBytes("ready\r\n"));
                if (await ReadLine(stream) != "10008sample\r\n") throw new Exception("TCP sample request");
                await stream.WriteAsync(binary[..7]); await Task.Delay(15); await stream.WriteAsync(binary[7..]);
                await Task.Delay(50);
            });
            using var client = new TcpDeviceService();
            var closed = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
            client.Disconnected += (_, reason) => closed.TrySetResult(reason);
            await client.OpenAsync("127.0.0.1", port, 2, CancellationToken.None);
            if (!(await EmtHandshake.ExecuteAsync(client)).IsSuccess) throw new Exception("TCP split handshake");
            if (!(await EmtSampling.ExecuteAsync(client, 100, 8)).IsSuccess) throw new Exception("shared TCP binary sampling");
            await server; await closed.Task.WaitAsync(TimeSpan.FromSeconds(2));
            if (client.IsOpen) throw new Exception("TCP disconnect state remained open");
        }
        finally { listener.Stop(); }
        using var refused = new TcpTransport();
        try { await refused.OpenAsync("127.0.0.1", port, TimeSpan.FromSeconds(1), CancellationToken.None); throw new Exception("refused TCP accepted"); }
        catch (SocketException) { }
        catch (OperationCanceledException) { }
        if (refused.IsOpen) throw new Exception("failed TCP leaked state");
        try { await refused.OpenAsync("127.0.0.1", port, TimeSpan.FromSeconds(1), cancel.Token); throw new Exception("cancelled TCP accepted"); }
        catch (OperationCanceledException) { }
        Console.WriteLine("PASS: handshake correct/wrong/timeout/cancel; shared serial-style/TCP split sampling, overflow rejection, selectable batch counts (1/3/12), subscription cleanup, refused/disconnected/cancelled TCP.");
    }
    private static async Task<string> ReadLine(NetworkStream stream)
    {
        var bytes = new List<byte>(); var buffer = new byte[1];
        while (true) { if (await stream.ReadAsync(buffer) == 0) throw new IOException("peer closed"); bytes.Add(buffer[0]); if (buffer[0] == 10) return Encoding.ASCII.GetString(bytes.ToArray()); }
    }
    private sealed class FakeChannel : IDeviceChannel
    {
        public bool IsOpen => true;
        private EventHandler<LogEntry>? _received;
        public int SubscriberCount => _received?.GetInvocationList().Length ?? 0;
        public event EventHandler<LogEntry>? DataReceived { add => _received += value; remove => _received -= value; }
        public Action<byte[]> Response { get; set; } = _ => { };
        public string? Last { get; private set; }
        public void Emit(byte[] bytes) => _received?.Invoke(this, new LogEntry { RawData = bytes });
        public OperationResult SendText(string? text, string? encodingName = null, ChecksumType checksumType = ChecksumType.None, bool appendChecksum = false)
        { Last = text; Response(Encoding.ASCII.GetBytes(text!)); return new OperationResult { IsSuccess = true }; }
    }
}
