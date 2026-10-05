using System;
namespace Tomography.Business.Helpers
{
    /// <summary>
    /// 时间处理工具。
    /// </summary>
    public static class TimeHelper
    {
        /// <summary>
        /// 默认时间格式。
        /// </summary>
        public const string DefaultDateTimeFormat = "yyyy-MM-dd HH:mm:ss.fff";

        /// <summary>
        /// 将时间按指定格式转换为字符串。
        /// </summary>
        public static string Format(DateTime dateTime, string? format = null)
        {
            return dateTime.ToString(string.IsNullOrWhiteSpace(format) ? DefaultDateTimeFormat : format);
        }

        /// <summary>
        /// 获取当前时间的格式化字符串。
        /// </summary>
        public static string FormatNow(string? format = null)
        {
            return Format(DateTime.Now, format);
        }

        /// <summary>
        /// 将 DateTime 转换为 Unix 毫秒时间戳。
        /// </summary>
        public static long ToUnixTimeMilliseconds(DateTime dateTime)
        {
            return new DateTimeOffset(dateTime).ToUnixTimeMilliseconds();
        }

        /// <summary>
        /// 将 Unix 毫秒时间戳转换为 DateTime。
        /// </summary>
        public static DateTime FromUnixTimeMilliseconds(long unixTimeMilliseconds, bool toLocalTime = true)
        {
            var dateTimeOffset = DateTimeOffset.FromUnixTimeMilliseconds(unixTimeMilliseconds);
            return toLocalTime ? dateTimeOffset.LocalDateTime : dateTimeOffset.UtcDateTime;
        }

        /// <summary>
        /// 格式化时间间隔。
        /// </summary>
        public static string FormatDuration(TimeSpan duration)
        {
            return duration.ToString(@"hh\:mm\:ss\.fff");
        }
    }
}
