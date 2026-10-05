using System.IO.Ports;
using Tomography.Business.Helpers;
using Tomography.Business.Interfaces;
using Tomography.Business.Models;

namespace Tomography.Business.Services
{
    /// <summary>串口配置合法性校验。</summary>
    public partial class SerialService : ISerialService
    {
        /// <summary>
        /// 校验串口配置是否合法。
        /// </summary>
        /// <returns>合法返回 <see langword="null"/>，否则返回错误消息。</returns>
        internal static string? ValidateConfig(SerialPortConfig config)
        {
            if (!ValidationHelper.IsValidPortName(config.PortName))
            {
                return "串口名称无效。";
            }

            if (!ValidationHelper.IsValidBaudRate(config.BaudRate))
            {
                return "波特率无效。";
            }

            if (!ValidationHelper.IsValidDataBits(config.DataBits))
            {
                return "数据位无效。";
            }

            if (!ValidationHelper.IsValidParity((int)config.Parity))
            {
                return "校验位无效。";
            }

            if (!ValidationHelper.IsValidStopBits((int)config.StopBits))
            {
                return "停止位无效。";
            }

            if (!ValidationHelper.IsValidHandshake((int)config.Handshake))
            {
                return "握手协议无效。";
            }

            if (!ValidationHelper.IsValidTimeout(config.ReadTimeout) || !ValidationHelper.IsValidTimeout(config.WriteTimeout))
            {
                return "超时时间无效。";
            }

            if (!ValidationHelper.IsValidEncodingName(config.EncodingName))
            {
                return "编码名称无效。";
            }

            return null;
        }
    }
}
