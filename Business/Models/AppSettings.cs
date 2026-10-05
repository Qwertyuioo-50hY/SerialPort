using Tomography.Business.Configuration;

namespace Tomography.Business.Models
{
    /// <summary>
    /// 应用默认设置。
    /// </summary>
    public sealed class AppSettings
    {
        /// <summary>
        /// 默认串口配置。
        /// </summary>
        public SerialPortConfig DefaultSerialPortConfig { get; set; } = new()
        {
            PortName = AppConstants.DefaultPortName,//串口名称
            BaudRate = AppConstants.DefaultBaudRate,//波特率
            Parity = AppConstants.DefaultParity,//校验位
            DataBits = AppConstants.DefaultDataBits,//数据位
            StopBits = AppConstants.DefaultStopBits,//停止位
            Handshake = AppConstants.DefaultHandshake,//握手协议
            ReadTimeout = AppConstants.DefaultReadTimeoutMs,//读取超时时间
            WriteTimeout = AppConstants.DefaultWriteTimeoutMs,//写入超时时间
            DtrEnable = AppConstants.DefaultDtrEnable,//是否启用 DTR
            RtsEnable = AppConstants.DefaultRtsEnable,//是否启用 RTS
            EncodingName = AppConstants.DefaultEncodingName//编码名称
        };

        /// <summary>
        /// 默认日志目录。
        /// </summary>
        public string LogDirectoryPath { get; set; } = AppConstants.DefaultLogDirectoryPath;

        /// <summary>
        /// 默认日志文件路径。
        /// </summary>
        public string LogFilePath { get; set; } = AppConstants.DefaultLogFilePath;

        /// <summary>
        /// 默认编码名称。
        /// </summary>
        public string DefaultEncodingName { get; set; } = AppConstants.DefaultEncodingName;

        /// <summary>
        /// 默认刷新间隔，单位毫秒。
        /// </summary>
        public int RefreshIntervalMs { get; set; } = AppConstants.DefaultRefreshIntervalMs;

        /// <summary>
        /// 创建一份新的默认设置副本。
        /// </summary>
        public static AppSettings CreateDefault() => new();
    }
}
