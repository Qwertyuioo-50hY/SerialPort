using System.IO.Ports;

namespace Tomography.Data.Models
{
    /// <summary>
    /// 串口配置实体
    /// </summary>
    public class SerialPortParameters
    {
        /// <summary>
        /// 串口名称，如 COM1。
        /// </summary>
        public string PortName { get; set; } = string.Empty;

        /// <summary>
        /// 波特率。
        /// </summary>
        public int BaudRate { get; set; }

        /// <summary>
        /// 校验位。
        /// </summary>
        public Parity Parity { get; set; }

        /// <summary>
        /// 数据位。
        /// </summary>
        public int DataBits { get; set; }

        /// <summary>
        /// 停止位。
        /// </summary>
        public StopBits StopBits { get; set; }

        /// <summary>
        /// 握手协议。
        /// </summary>
        public Handshake Handshake { get; set; }

        /// <summary>
        /// 读取超时时间，单位为毫秒。
        /// </summary>
        public int ReadTimeout { get; set; }

        /// <summary>
        /// 写入超时时间，单位为毫秒。
        /// </summary>
        public int WriteTimeout { get; set; }

        /// <summary>
        /// 是否启用 DTR。
        /// </summary>
        public bool DtrEnable { get; set; }

        /// <summary>
        /// 是否启用 RTS。
        /// </summary>
        public bool RtsEnable { get; set; }

    }
}
