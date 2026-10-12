using Tomography.Business.Models;
using Tomography.Business.Models.Enums;

namespace Tomography.Business.Interfaces;

public interface IDeviceChannel
{
    bool IsOpen { get; }
    event EventHandler<LogEntry>? DataReceived;
    OperationResult SendText(string? text, string? encodingName = null, ChecksumType checksumType = ChecksumType.None, bool appendChecksum = false);
}
