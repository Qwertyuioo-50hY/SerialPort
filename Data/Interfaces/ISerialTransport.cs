using Tomography.Data.Models;

namespace Tomography.Data.Interfaces
{
    public interface ISerialTransport : IByteTransport
    {
        string[] GetPortNames();
        void Open(SerialPortParameters config);
    }
}
