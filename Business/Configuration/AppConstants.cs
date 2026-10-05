using System;
using System.IO.Ports;

namespace Tomography.Business.Configuration
{
    /// <summary>
    /// 应用级常量与默认值。
    /// </summary>
    public static class AppConstants
    {
        /// <summary>
        /// 应用名称。
        /// </summary>
        public const string ApplicationName = "Tomography";

        /// <summary>
        /// 默认串口名称。
        /// </summary>
        public const string DefaultPortName = "COM1";

        /// <summary>
        /// 默认波特率。
        /// </summary>
        public const int DefaultBaudRate = 9600;

        /// <summary>
        /// 默认校验位。
        /// </summary>
        public const Parity DefaultParity = Parity.None;

        /// <summary>
        /// 默认数据位。
        /// </summary>
        public const int DefaultDataBits = 8;

        /// <summary>
        /// 默认停止位。
        /// </summary>
        public const StopBits DefaultStopBits = StopBits.One;

        /// <summary>
        /// 默认握手协议。
        /// </summary>
        public const Handshake DefaultHandshake = Handshake.None;

        /// <summary>
        /// 默认读取超时时间，单位毫秒。
        /// </summary>
        public const int DefaultReadTimeoutMs = 1000;

        /// <summary>
        /// 默认写入超时时间，单位毫秒。
        /// </summary>
        public const int DefaultWriteTimeoutMs = 1000;

        /// <summary>
        /// 默认是否启用 DTR。
        /// </summary>
        public const bool DefaultDtrEnable = false;

        /// <summary>
        /// 默认是否启用 RTS。
        /// </summary>
        public const bool DefaultRtsEnable = false;

        /// <summary>
        /// 默认编码名称。
        /// </summary>
        public const string DefaultEncodingName = "UTF-8";

        /// <summary>
        /// 默认刷新间隔，单位毫秒。
        /// </summary>
        public const int DefaultRefreshIntervalMs = 500;

        /// <summary>
        /// 日志目录名。
        /// </summary>
        public const string LogDirectoryName = "Logs";

        /// <summary>
        /// 默认日志文件名。
        /// </summary>
        public const string LogFileName = "serial.log";

        /// <summary>
        /// 默认日志目录路径。
        /// </summary>
        public static string DefaultLogDirectoryPath => Path.Combine(AppContext.BaseDirectory, LogDirectoryName);

        /// <summary>
        /// 默认日志文件路径。
        /// </summary>
        public static string DefaultLogFilePath => Path.Combine(DefaultLogDirectoryPath, LogFileName);
    }
}
