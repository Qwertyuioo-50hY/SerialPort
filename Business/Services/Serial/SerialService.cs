using System.IO.Ports;
using Tomography.Business.Interfaces;
using Tomography.Business.Models;
using Tomography.Business.Models.Enums;
using Tomography.Data;
using Tomography.Data.Interfaces;
using Tomography.Data.Models;

namespace Tomography.Business.Services
{
    /// <summary>串口连接管理、事件及资源生命周期。</summary>
    public partial class SerialService : ISerialService, IDeviceChannel
    {
        private readonly IChecksumService _checksumService;
        private readonly ISerialTransport _transport;
        private SerialPortConfig? _config;
        private bool _disposed;

        /// <summary>
        /// 初始化串口服务。
        /// </summary>
        /// <param name="checksumService">校验服务，空则使用默认实现。</param>
        public SerialService(IChecksumService? checksumService = null, ISerialTransport? transport = null)
        {
            _checksumService = checksumService ?? new ChecksumService();
            _transport = transport ?? new SerialPortTransport();
            _transport.DataReceived += OnDataReceived;
        }

        /// <summary>
        /// 当前串口是否已打开。
        /// </summary>
        public bool IsOpen => _transport.IsOpen;

        public string[] GetPortNames() => _transport.GetPortNames().OrderBy(x => x).ToArray();

        /// <summary>
        /// 接收数据事件。
        /// </summary>
        public event EventHandler<LogEntry>? DataReceived;

        /// <summary>
        /// 发送数据事件。
        /// </summary>
        public event EventHandler<LogEntry>? DataSent;

        /// <summary>
        /// 按配置打开串口。
        /// </summary>
        public OperationResult Open(SerialPortConfig config)
        {
            ThrowIfDisposed();

            if (config is null)
            {
                return Fail("串口配置不能为空。");
            }

            var validationMessage = ValidateConfig(config);
            if (!string.IsNullOrEmpty(validationMessage))
            {
                return Fail(validationMessage);
            }

            try
            {
                if (IsOpen) return Success("串口已打开。", _config?.PortName);
                if (!GetPortNames().Contains(config.PortName, StringComparer.OrdinalIgnoreCase))
                {
                    return Fail($"串口不存在: {config.PortName}");
                }

                _config = config;
                _transport.Open(new SerialPortParameters
                {
                    PortName = config.PortName,
                    BaudRate = config.BaudRate,
                    Parity = config.Parity,
                    DataBits = config.DataBits,
                    StopBits = config.StopBits,
                    Handshake = config.Handshake,
                    ReadTimeout = config.ReadTimeout,
                    WriteTimeout = config.WriteTimeout,
                    DtrEnable = config.DtrEnable,
                    RtsEnable = config.RtsEnable
                });
                return Success("串口打开成功。", config.PortName);
            }
            catch (Exception ex)
            {
                return Fail("串口打开失败。", ex);
            }
        }

        /// <summary>
        /// 关闭并释放当前串口实例。
        /// </summary>
        public OperationResult Close()
        {
            if (_disposed)
            {
                return Success("串口已释放。", null);
            }

            try
            {
                _transport.Close();
                _config = null;
                return Success("串口关闭成功。", null);
            }
            catch (Exception ex)
            {
                return Fail("串口关闭失败。", ex);
            }
        }


        /// <summary>
        /// 释放串口资源。
        /// </summary>
        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            Close();
            _transport.DataReceived -= OnDataReceived;
            _transport.Dispose();
            _disposed = true;
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// 串口底层接收事件处理。
        /// </summary>
        private void OnDataReceived(object? sender, byte[] data)
        {
            var message = BuildMessage(data, MessageDirection.Receive, DataDisplayMode.Text, _config?.EncodingName);
            DataReceived?.Invoke(this, message);
        }


        /// <summary>
        /// 构造成功结果对象。
        /// </summary>
        private static OperationResult Success(string message, object? data)
        {
            return new OperationResult
            {
                IsSuccess = true,
                Message = message,
                Data = data
            };
        }

        /// <summary>
        /// 构造失败结果对象。
        /// </summary>
        private static OperationResult Fail(string message, Exception? ex = null)
        {
            return new OperationResult
            {
                IsSuccess = false,
                Message = message,
                Exception = ex
            };
        }

        /// <summary>
        /// 若对象已释放则抛出异常。
        /// </summary>
        private void ThrowIfDisposed()
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
        }
    }
}
