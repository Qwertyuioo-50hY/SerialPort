using System;
using System.Text.RegularExpressions;

namespace Tomography.Business.Helpers
{
    /// <summary>
    /// 参数校验工具。
    /// </summary>
    public static class ValidationHelper
    {
        private static readonly Regex ComPortRegex = new("^COM\\d+$", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly string[] ParityNames = { "none", "odd", "even", "mark", "space" };
        private static readonly string[] StopBitsNames = { "none", "one", "two", "onepointfive", "1", "2", "1.5" };
        private static readonly string[] HandshakeNames = { "none", "xonxoff", "requesttosend", "requesttosendxonxoff" };

        /// <summary>
        /// 判断串口名称是否有效（如 COM1）。
        /// </summary>
        public static bool IsValidPortName(string? portName)
        {
            return !string.IsNullOrWhiteSpace(portName) && ComPortRegex.IsMatch(portName.Trim());
        }

        /// <summary>
        /// 判断波特率是否有效。
        /// </summary>
        public static bool IsValidBaudRate(int baudRate)
        {
            return baudRate > 0;
        }

        /// <summary>
        /// 判断数据位是否有效（常用 5~8）。
        /// </summary>
        public static bool IsValidDataBits(int dataBits)
        {
            return dataBits is >= 5 and <= 8;
        }

        /// <summary>
        /// 判断超时时间是否有效，-1 表示无限等待。
        /// </summary>
        public static bool IsValidTimeout(int timeoutMilliseconds)
        {
            return timeoutMilliseconds >= -1;
        }

        /// <summary>
        /// 判断校验位是否有效。
        /// </summary>
        public static bool IsValidParity(int parity)
        {
            return parity is >= 0 and <= 4;
        }

        /// <summary>
        /// 判断校验位字符串是否有效（None/Odd/Even/Mark/Space 或数字 0~4）。
        /// </summary>
        public static bool IsValidParity(string? parity)
        {
            if (string.IsNullOrWhiteSpace(parity))
            {
                return false;
            }

            var normalized = parity.Trim();
            if (int.TryParse(normalized, out var parityValue))
            {
                return IsValidParity(parityValue);
            }

            return ParityNames.Contains(normalized, StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// 判断停止位是否有效。
        /// </summary>
        public static bool IsValidStopBits(int stopBits)
        {
            return stopBits is 0 or 1 or 2 or 3;
        }

        /// <summary>
        /// 判断停止位字符串是否有效（None/One/Two/OnePointFive 或数字 0~3）。
        /// </summary>
        public static bool IsValidStopBits(string? stopBits)
        {
            if (string.IsNullOrWhiteSpace(stopBits))
            {
                return false;
            }

            var normalized = stopBits.Trim();
            if (int.TryParse(normalized, out var stopBitsValue))
            {
                return IsValidStopBits(stopBitsValue);
            }

            return StopBitsNames.Contains(NormalizeToken(normalized), StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// 判断握手协议是否有效。
        /// </summary>
        public static bool IsValidHandshake(int handshake)
        {
            return handshake is >= 0 and <= 3;
        }

        /// <summary>
        /// 判断握手协议字符串是否有效（None/XOnXOff/RequestToSend/RequestToSendXOnXOff 或数字 0~3）。
        /// </summary>
        public static bool IsValidHandshake(string? handshake)
        {
            if (string.IsNullOrWhiteSpace(handshake))
            {
                return false;
            }

            var normalized = handshake.Trim();
            if (int.TryParse(normalized, out var handshakeValue))
            {
                return IsValidHandshake(handshakeValue);
            }

            return HandshakeNames.Contains(NormalizeToken(normalized), StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// 判断编码名称是否有效。
        /// </summary>
        public static bool IsValidEncodingName(string? encodingName)
        {
            return EncodingHelper.IsSupportedEncoding(encodingName);
        }

        /// <summary>
        /// 判断十六进制字符串是否有效。
        /// </summary>
        public static bool IsValidHexString(string? hexString, bool allowEmpty = true)
        {
            if (string.IsNullOrWhiteSpace(hexString))
            {
                return allowEmpty;
            }

            return HexConverter.TryToByteArray(hexString, out _);
        }

        /// <summary>
        /// 判断值是否在区间范围内。
        /// </summary>
        public static bool IsInRange(int value, int minValue, int maxValue)
        {
            return value >= minValue && value <= maxValue;
        }

        private static string NormalizeToken(string value)
        {
            return value.Replace("_", string.Empty)
                        .Replace("-", string.Empty)
                        .Replace(" ", string.Empty);
        }
    }
}
