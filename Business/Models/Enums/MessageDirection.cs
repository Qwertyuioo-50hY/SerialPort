using System;
namespace Tomography.Business.Models.Enums
{
    /// <summary>
    /// 消息方向
    /// </summary>
    public enum MessageDirection
    {
        /// <summary>
        /// 接收消息，表示从串口接收到的数据或系统日志消息。
        /// </summary>
        Receive = 0,
        /// <summary>
        /// 发送消息，表示向串口发送的数据或系统日志消息。
        /// </summary>
        Send = 1,
        /// <summary>
        /// 系统内部/控制消息。
        /// </summary>
        System = 2
    }
}
