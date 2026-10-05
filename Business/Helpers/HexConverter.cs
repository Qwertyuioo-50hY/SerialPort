using System.Text;

namespace Tomography.Business.Helpers
{
    /// <summary>
    /// 十六进制与字节数组互转工具。
    /// </summary>
    public static class HexConverter
    {
        /// <summary>
        /// 将字节数组转换为十六进制字符串。
        /// </summary>
        public static string ToHexString(byte[]? bytes, bool upperCase = true, string separator = " ")
        {
            if (bytes is null || bytes.Length == 0)
            {
                return string.Empty;
            }

            var format = upperCase ? "X2" : "x2";
            var sb = new StringBuilder(bytes.Length * (2 + separator.Length));

            for (var i = 0; i < bytes.Length; i++)
            {
                if (i > 0 && separator.Length > 0)
                {
                    sb.Append(separator);
                }

                sb.Append(bytes[i].ToString(format));
            }

            return sb.ToString();
        }

        /// <summary>
        /// 将十六进制字符串转换为字节数组。
        /// 允许空白字符和常见分隔符（空格、-、:）。
        /// </summary>
        public static byte[] ToByteArray(string? hexString)
        {
            if (string.IsNullOrWhiteSpace(hexString))
            {
                return Array.Empty<byte>();
            }

            var normalized = Normalize(hexString);
            if ((normalized.Length & 1) == 1)
            {
                throw new FormatException("十六进制字符串长度必须为偶数。");
            }

            var bytes = new byte[normalized.Length / 2];
            for (var i = 0; i < normalized.Length; i += 2)
            {
                bytes[i / 2] = Convert.ToByte(normalized.Substring(i, 2), 16);
            }

            return bytes;
        }

        /// <summary>
        /// 尝试将十六进制字符串转换为字节数组。
        /// </summary>
        public static bool TryToByteArray(string? hexString, out byte[] bytes)
        {
            try
            {
                bytes = ToByteArray(hexString);
                return true;
            }
            catch (FormatException)
            {
                bytes = Array.Empty<byte>();
                return false;
            }
            catch (OverflowException)
            {
                bytes = Array.Empty<byte>();
                return false;
            }
        }

        private static string Normalize(string input)
        {
            var sb = new StringBuilder(input.Length);

            foreach (var ch in input)
            {
                if (char.IsWhiteSpace(ch) || ch == '-' || ch == ':')
                {
                    continue;
                }

                if (!Uri.IsHexDigit(ch))
                {
                    throw new FormatException($"包含非法十六进制字符: '{ch}'。");
                }

                sb.Append(ch);
            }

            return sb.ToString();
        }
    }
}
