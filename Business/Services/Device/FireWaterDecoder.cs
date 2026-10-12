using System.Globalization;
using System.Text;

namespace Tomography.Business.Services;

/// <summary>FireWater 数值流：换行分帧，取最后一个冒号后的 CSV，按原始顺序映射通道。</summary>
public sealed class FireWaterDecoder
{
    private readonly StringBuilder _pending = new();
    public IEnumerable<double[]> Feed(byte[] bytes)
    {
        var frames = new List<double[]>();
        foreach (var value in bytes)
        {
            if (value != 10)
            {
                if (_pending.Length >= 65536) { _pending.Clear(); throw new FormatException("FireWater 行超过 64 KiB。"); }
                _pending.Append((char)value); continue;
            }
            var line = _pending.ToString().Trim(); _pending.Clear();
            if (line.Length == 0) continue;
            var payload = line[(line.LastIndexOf(':') + 1)..];
            var fields = payload.Split(',');
            if (fields.Length != 64) throw new FormatException("本项目 FireWater 输入必须包含 64 个通道，按 CSV 顺序映射。");
            var frame = fields.Select(field => double.Parse(field.Trim(), CultureInfo.InvariantCulture)).ToArray();
            if (frame.Any(value => !double.IsFinite(value))) throw new FormatException("FireWater 通道必须为有限数值。");
            frames.Add(frame);
        }
        return frames;
    }
}
