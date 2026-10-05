using System.Buffers.Binary;

namespace Tomography.Business.Services;

/// <summary>将一帧 256 字节解码为 64 个小端 uint32 通道值。</summary>
public static class EmtSampleDecoder
{
    public const int ChannelCount = 64;
    public const int FrameSize = ChannelCount * sizeof(uint);

    public static uint[] Decode(ReadOnlySpan<byte> frame)
    {
        if (frame.Length != FrameSize)
            throw new ArgumentException("采样帧必须为 256 字节。", nameof(frame));
        var channels = new uint[ChannelCount];
        for (var i = 0; i < channels.Length; i++)
            channels[i] = BinaryPrimitives.ReadUInt32LittleEndian(frame.Slice(i * sizeof(uint), sizeof(uint)));
        return channels;
    }
}
