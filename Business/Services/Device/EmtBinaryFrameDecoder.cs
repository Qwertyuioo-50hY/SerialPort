using System.Buffers.Binary;

namespace Tomography.Business.Services;

/// <summary>固件输出固定 64 个 UInt32 小端数值，无帧头、帧尾和换行。</summary>
public sealed class EmtBinaryFrameDecoder
{
    public const int ChannelCount = 64;
    public const int FrameSize = ChannelCount * sizeof(uint);
    private readonly byte[] _frame = new byte[FrameSize];
    public int ReceivedBytes { get; private set; }
    public uint[]? Feed(byte[] bytes)
    {
        if (bytes.Length > FrameSize - ReceivedBytes) throw new FormatException("EMT 回复超过一帧 256 字节，请断开并重新握手，避免数据错位。");
        bytes.CopyTo(_frame, ReceivedBytes); ReceivedBytes += bytes.Length;
        if (ReceivedBytes != FrameSize) return null;
        var values = new uint[ChannelCount];
        for (var i = 0; i < values.Length; i++) values[i] = BinaryPrimitives.ReadUInt32LittleEndian(_frame.AsSpan(i * sizeof(uint), sizeof(uint)));
        return values;
    }
}
