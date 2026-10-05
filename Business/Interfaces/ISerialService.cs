using System;
using Tomography.Business.Models;
using Tomography.Business.Models.Enums;

namespace Tomography.Business.Interfaces
{
    /// <summary>
    /// 串口服务接口，负责串口连接、收发与资源管理。
    /// </summary>
    public interface ISerialService : IDisposable
    {
        /// <summary>
        /// 当前串口是否处于打开状态。
        /// </summary>
        bool IsOpen { get; }

        string[] GetPortNames();

        /// <summary>
        /// 串口接收到数据时触发。
        /// </summary>
        event EventHandler<LogEntry>? DataReceived;

        /// <summary>
        /// 串口发送数据后触发。
        /// </summary>
        event EventHandler<LogEntry>? DataSent;

        /// <summary>
        /// 打开串口。
        /// </summary>
        /// <param name="config">串口配置。</param>
        /// <returns>操作结果。</returns>
        OperationResult Open(SerialPortConfig config);

        /// <summary>
        /// 关闭串口。
        /// </summary>
        /// <returns>操作结果。</returns>
        OperationResult Close();

        /// <summary>
        /// 发送字节数据。
        /// </summary>
        /// <param name="data">待发送字节。</param>
        /// <param name="checksumType">校验类型。</param>
        /// <param name="appendChecksum">是否在末尾附加校验值。</param>
        /// <returns>操作结果。</returns>
        OperationResult SendBytes(byte[]? data, ChecksumType checksumType = ChecksumType.None, bool appendChecksum = false);

        /// <summary>
        /// 发送文本数据。
        /// </summary>
        /// <param name="text">待发送文本。</param>
        /// <param name="encodingName">编码名称，空则使用配置编码。</param>
        /// <param name="checksumType">校验类型。</param>
        /// <param name="appendChecksum">是否在末尾附加校验值。</param>
        /// <returns>操作结果。</returns>
        OperationResult SendText(string? text, string? encodingName = null, ChecksumType checksumType = ChecksumType.None, bool appendChecksum = false);

        /// <summary>
        /// 构建统一日志消息对象。
        /// </summary>
        /// <param name="rawData">原始字节数据。</param>
        /// <param name="direction">消息方向。</param>
        /// <param name="displayMode">显示模式。</param>
        /// <param name="encodingName">文本模式下使用的编码名称。</param>
        /// <returns>日志条目对象。</returns>
        LogEntry BuildMessage(byte[]? rawData, MessageDirection direction, DataDisplayMode displayMode, string? encodingName = null);
    }
}
