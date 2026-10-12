namespace Tomography.Data.Interfaces;

public interface IByteTransport : IDisposable
{
    bool IsOpen { get; }
    event EventHandler<byte[]>? DataReceived;
    void Write(byte[] data);
    void Close();
}
