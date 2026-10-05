using Tomography.Business.Helpers;
using Tomography.Business.Interfaces;
using Tomography.Business.Models;
using Tomography.Business.Models.Enums;

namespace Tomography.Business.Services
{
    /// <summary>日志文本格式与内容解析。</summary>
    public partial class LogService : ILogService
    {
        /// <summary>
        /// 格式化日志文本。
        /// </summary>
        public string Format(LogEntry entry)
        {
            var timestamp = TimeHelper.Format(entry.Timestamp);
            var content = ResolveContent(entry);
            return $"[{timestamp}] [{entry.Direction}] [{entry.DisplayMode}] {content}";
        }

        /// <summary>
        /// 解析日志内容文本；优先使用 <see cref="LogEntry.Message"/>。
        /// </summary>
        private static string ResolveContent(LogEntry entry)
        {
            if (!string.IsNullOrWhiteSpace(entry.Message))
            {
                return entry.Message;
            }

            if (entry.RawData is { Length: > 0 })
            {
                return entry.DisplayMode == DataDisplayMode.Hex
                    ? HexConverter.ToHexString(entry.RawData)
                    : EncodingHelper.GetString(entry.RawData);
            }

            return string.Empty;
        }
    }
}
