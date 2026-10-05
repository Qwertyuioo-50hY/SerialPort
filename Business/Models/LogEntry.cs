using System;
using Tomography.Business.Models.Enums;

namespace Tomography.Business.Models
{
    /// <summary>
    /// 日志条目，表示一条串口通信或系统日志记录。
    /// </summary>
    public class LogEntry
    {
        /// <summary>
        /// 日志时间戳，记录条目创建时间。
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.Now;

        /// <summary>
        /// 消息方向（接收/发送/系统）。
        /// </summary>
        public MessageDirection Direction { get; set; } = MessageDirection.System;

        /// <summary>
        /// 数据展示模式（文本或十六进制）。
        /// </summary>
        public DataDisplayMode DisplayMode { get; set; } = DataDisplayMode.Text;

        /// <summary>
        /// 人类可读的消息文本，用于显示在日志视图中。
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// 原始字节数据（如果有）。
        /// </summary>
        public byte[] RawData { get; set; } = Array.Empty<byte>();
    }
}
