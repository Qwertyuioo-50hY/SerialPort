using System.IO.Ports;

namespace Tomography.Business.Models
{
    /// <summary>
    /// 串口配置实体
    /// </summary>
    public class SerialPortConfig
    {
        /// <summary>
        /// 串口名称，如 COM1。
        /// </summary>
        public string PortName { get; set; } = string.Empty;

        /// <summary>
        /// 波特率。
        /// </summary>
        public int BaudRate { get; set; } = 9600;

        /// <summary>
        /// 校验位。
        /// </summary>
        public Parity Parity { get; set; } = Parity.None;

        /// <summary>
        /// 数据位。
        /// </summary>
        public int DataBits { get; set; } = 8;

        /// <summary>
        /// 停止位。
        /// </summary>
        public StopBits StopBits { get; set; } = StopBits.One;

        /// <summary>
        /// 握手协议。
        /// </summary>
        public Handshake Handshake { get; set; } = Handshake.None;

        /// <summary>
        /// 读取超时时间，单位为毫秒。
        /// </summary>
        public int ReadTimeout { get; set; } = 1000;

        /// <summary>
        /// 写入超时时间，单位为毫秒。
        /// </summary>
        public int WriteTimeout { get; set; } = 1000;

        /// <summary>
        /// 是否启用 DTR。
        /// </summary>
        public bool DtrEnable { get; set; }

        /// <summary>
        /// 是否启用 RTS。
        /// </summary>
        public bool RtsEnable { get; set; }

        /// <summary>
        /// 编码名称。
        /// </summary>
        public string EncodingName { get; set; } = "UTF-8";
    }
}