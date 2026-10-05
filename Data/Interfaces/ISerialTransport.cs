using Tomography.Data.Models;

namespace Tomography.Data.Interfaces
{
    public interface ISerialTransport : IDisposable
    {
        bool IsOpen { get; }
        event EventHandler<byte[]>? DataReceived;
        string[] GetPortNames();
        void Open(SerialPortParameters config);
        void Close();
        void Write(byte[] data);
    }
}
