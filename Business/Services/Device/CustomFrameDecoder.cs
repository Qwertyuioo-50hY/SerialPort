using System.Buffers.Binary;
using Tomography.Business.Models;

namespace Tomography.Business.Services;

/// <summary>固定长度声明式数值帧；不执行脚本，校验覆盖帧起始至校验字节之前。</summary>
public sealed class CustomFrameDecoder
{
    private readonly CustomFrameDefinition _definition;
    private readonly byte[] _header;
    private readonly int _fieldSize;
    private readonly List<byte> _pending = new();
    public CustomFrameDecoder(CustomFrameDefinition definition)
    {
        _definition = definition;
        _header = Convert.FromHexString(definition.HeaderHex.Replace(" ", ""));
        _fieldSize = definition.FieldType switch { "UInt16" or "Int16" => 2, "UInt32" or "Int32" or "Float32" => 4, "Float64" => 8, _ => throw new ArgumentException("不支持该字段类型。") };
        if (_header.Length == 0 || definition.Length is < 1 or > 65536 || definition.DataOffset < _header.Length ||
            definition.ChannelMap.Length != 64 || definition.ChannelMap.Any(i => i < 0 || i >= 65536) ||
            definition.Checksum is not ("None" or "XOR8" or "SUM8") ||
            (long)definition.DataOffset + ((long)definition.ChannelMap.Max() + 1) * _fieldSize > definition.Length - (definition.Checksum == "None" ? 0 : 1))
            throw new ArgumentException("帧头、长度、数据偏移、校验或 64 通道映射不合法。");
    }
    public IEnumerable<double[]> Feed(byte[] bytes)
    {
        var frames = new List<double[]>();
        foreach (var value in bytes)
        {
            _pending.Add(value);
            while (_pending.Count >= _header.Length && !_pending.Take(_header.Length).SequenceEqual(_header)) _pending.RemoveAt(0);
            if (_pending.Count < _definition.Length) continue;
            var frame = _pending.ToArray(); _pending.Clear();
            if (_definition.Checksum != "None")
            {
                byte expected = 0;
                foreach (var b in frame.AsSpan(0, frame.Length - 1)) expected = _definition.Checksum == "XOR8" ? (byte)(expected ^ b) : unchecked((byte)(expected + b));
                if (expected != frame[^1]) throw new FormatException("自定义帧校验失败。");
            }
            var values = _definition.ChannelMap.Select(i => Read(frame.AsSpan(_definition.DataOffset + i * _fieldSize, _fieldSize))).ToArray();
            if (values.Any(v => !double.IsFinite(v))) throw new FormatException("自定义帧含非有限数值。");
            frames.Add(values);
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
