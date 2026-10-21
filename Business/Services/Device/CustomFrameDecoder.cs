using System.Buffers.Binary;
using Tomography.Business.Models;
using Tomography.Business.Models.Enums;

namespace Tomography.Business.Services;

/// <summary>固定长度或帧尾分隔的声明式帧；CRC16 Modbus 低字节在前，校验位位于帧尾之前。</summary>
public sealed class CustomFrameDecoder
{
    public long AcceptedFrames { get; private set; }
    public long RejectedFrames { get; private set; }
    private readonly CustomFrameDefinition _definition;
    private readonly byte[] _header, _footer;
    private readonly int _fieldSize;
    private readonly List<byte> _pending = new();
    public CustomFrameDecoder(CustomFrameDefinition definition)
    {
        _definition = definition;
        _header = Convert.FromHexString(definition.HeaderHex.Replace(" ", ""));
        _footer = Convert.FromHexString(definition.FooterHex.Replace(" ", ""));
        _fieldSize = definition.FieldType switch { "UInt16" or "Int16" => 2, "UInt32" or "Int32" or "Float32" => 4, "Float64" => 8, _ => throw new ArgumentException("不支持该字段类型。") };
        if (_header.Length == 0 || _header.Length > 65536 || _footer.Length > 65536 || definition.Length is < 0 or > 65536 || (definition.Length == 0 && _footer.Length == 0) ||
            definition.Checksum is not ("None" or "CRC16 Modbus") || definition.ChecksumStart < 0 || definition.ChecksumStart >= 65536 ||
            definition.ChannelMap.Any(i => i < 0 || i >= 65536) || (definition.ChannelMap.Length != 0 && definition.ChannelMap.Length != 64))
            throw new ArgumentException("请检查帧头、帧尾、长度、CRC 校验范围和通道映射。长度为 0 时必须配置帧尾。");
        var minimum = _header.Length + _footer.Length + (definition.Checksum == "None" ? 0 : 2);
        if (definition.Length > 0 && (definition.Length < minimum || definition.ChecksumStart > definition.Length - _footer.Length - (definition.Checksum == "None" ? 0 : 2)))
            throw new ArgumentException("整帧长度或校验起始偏移无效。");
        if (definition.ChannelMap.Length > 0 && (definition.Length == 0 || definition.DataOffset < _header.Length ||
            (long)definition.DataOffset + ((long)definition.ChannelMap.Max() + 1) * _fieldSize > definition.Length - _footer.Length - (definition.Checksum == "None" ? 0 : 2)))
            throw new ArgumentException("数值通道映射超出固定帧的数据区。");
    }
    private int Find(byte[] marker, int start = 0)
    {
        for (var i = start; i <= _pending.Count - marker.Length; i++)
        {
            var match = true;
            for (var j = 0; j < marker.Length; j++) if (_pending[i + j] != marker[j]) { match = false; break; }
            if (match) return i;
        }
        return -1;
    }
    public IEnumerable<double[]> Feed(byte[] bytes)
    {
        var frames = new List<double[]>();
        _pending.AddRange(bytes);
        while (_pending.Count >= _header.Length)
        {
            var header = Find(_header);
            if (header < 0) { _pending.RemoveRange(0, _pending.Count - _header.Length + 1); break; }
            if (header > 0) _pending.RemoveRange(0, header);
            var length = _definition.Length;
            if (length == 0)
            {
                var end = Find(_footer, _header.Length + (_definition.Checksum == "None" ? 0 : 2));
                if (end < 0)
                {
                    if (_pending.Count > 65536) { RejectedFrames++; _pending.RemoveRange(0, _pending.Count - _header.Length + 1); }
                    break;
                }
                length = end + _footer.Length;
            }
            if (_pending.Count < length) break;
            var frame = _pending.Take(length).ToArray();
            if (_footer.Length > 0 && !frame.AsSpan(length - _footer.Length).SequenceEqual(_footer)) { RejectedFrames++; _pending.RemoveAt(0); continue; }
            var crcOffset = length - _footer.Length - (_definition.Checksum == "None" ? 0 : 2);
            if (_definition.Checksum != "None" && (_definition.ChecksumStart > crcOffset ||
                !new ChecksumService().ValidateChecksum(frame.AsSpan(_definition.ChecksumStart, crcOffset - _definition.ChecksumStart).ToArray(), ChecksumType.Crc16, frame.AsSpan(crcOffset, 2).ToArray())))
            { RejectedFrames++; _pending.RemoveAt(0); continue; }
            AcceptedFrames++;
            _pending.RemoveRange(0, length);
            if (_definition.ChannelMap.Length == 0) continue;
            var values = _definition.ChannelMap.Select(i => Read(frame.AsSpan(_definition.DataOffset + i * _fieldSize, _fieldSize))).ToArray();
            if (values.All(double.IsFinite)) frames.Add(values);
        }
        return frames;
    }
    private double Read(ReadOnlySpan<byte> data)
    {
        if (_definition.FieldType == "Float64") return _definition.BigEndian ? BinaryPrimitives.ReadDoubleBigEndian(data) : BinaryPrimitives.ReadDoubleLittleEndian(data);
        if (_fieldSize == 2)
        {
            var bits = _definition.BigEndian ? BinaryPrimitives.ReadUInt16BigEndian(data) : BinaryPrimitives.ReadUInt16LittleEndian(data);
            return _definition.FieldType == "Int16" ? unchecked((short)bits) : bits;
        }
        var value = _definition.BigEndian ? BinaryPrimitives.ReadUInt32BigEndian(data) : BinaryPrimitives.ReadUInt32LittleEndian(data);
        return _definition.FieldType switch { "Float32" => BitConverter.Int32BitsToSingle(unchecked((int)value)), "Int32" => unchecked((int)value), _ => value };
    }
}
