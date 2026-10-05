using System;
using System.Text;

namespace Tomography.Business.Helpers
{
    /// <summary>
    /// 编码相关工具。
    /// </summary>
    public static class EncodingHelper
    {
        static EncodingHelper()
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        }

        /// <summary>
        /// 根据编码名称获取编码对象，获取失败时返回回退编码（默认 UTF-8）。
        /// </summary>
        public static Encoding GetEncodingOrDefault(string? encodingName, Encoding? fallback = null)
        {
            var defaultEncoding = fallback ?? Encoding.UTF8;
            if (string.IsNullOrWhiteSpace(encodingName))
            {
                return defaultEncoding;
            }

            try
            {
                return Encoding.GetEncoding(encodingName);
            }
            catch (ArgumentException)
            {
                return defaultEncoding;
            }
        }

        /// <summary>
        /// 将字符串编码为字节数组。
        /// </summary>
        public static byte[] GetBytes(string? text, string? encodingName = null)
        {
            var encoding = GetEncodingOrDefault(encodingName);
            return encoding.GetBytes(text ?? string.Empty);
        }

        /// <summary>
        /// 将字节数组解码为字符串。
        /// </summary>
        public static string GetString(byte[]? bytes, string? encodingName = null)
        {
            if (bytes is null || bytes.Length == 0)
            {
                return string.Empty;
            }

            var encoding = GetEncodingOrDefault(encodingName);
            return encoding.GetString(bytes);
        }

        /// <summary>
        /// 判断编码名称是否受支持。
        /// </summary>
        public static bool IsSupportedEncoding(string? encodingName)
        {
            if (string.IsNullOrWhiteSpace(encodingName))
            {
                return false;
            }

            try
            {
                _ = Encoding.GetEncoding(encodingName);
                return true;
            }
            catch (ArgumentException)
            {
                return false;
            }
        }

        /// <summary>
        /// 获取常用编码名称。
        /// </summary>
        public static IReadOnlyList<string> GetCommonEncodingNames()
        {
            return new[]
            {
                "UTF-8",
                "ASCII",
                "Unicode",
                "UTF-32",
                "GB2312",
                "GBK"
            };
        }
    }
}
