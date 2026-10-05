using System.IO.Ports;
using Tomography.Data.Interfaces;
using Tomography.Data.Models;

namespace Tomography.Data
{
    /// <summary>
    /// 仅负责串口硬件访问，协议、校验和消息格式留在业务层。
    /// </summary>
    public class SerialPortTransport : ISerialTransport
    {
        private readonly object _syncRoot = new();
        private SerialPort? _serialPort;

        public bool IsOpen => _serialPort?.IsOpen == true;
        public event EventHandler<byte[]>? DataReceived;

        public string[] GetPortNames() => SerialPort.GetPortNames();

        public void Open(SerialPortParameters config)
        {
            lock (_syncRoot)
            {
                if (IsOpen) return;

                var port = new SerialPort(config.PortName, config.BaudRate, config.Parity, config.DataBits, config.StopBits)
                {
                    Handshake = config.Handshake,
                    ReadTimeout = config.ReadTimeout,
                    WriteTimeout = config.WriteTimeout,
                    DtrEnable = config.DtrEnable,
                    RtsEnable = config.RtsEnable
                };
                port.DataReceived += OnDataReceived;
                _serialPort = port;
                try
                {
                    port.Open();
                }
                catch
                {
                    Close();
                    throw;
                }
            }
        }

        public void Close()
        {
            lock (_syncRoot)
            {
                var port = _serialPort;
                _serialPort = null;
                if (port is null) return;
                port.DataReceived -= OnDataReceived;
                port.Dispose();
            }
        }

        public void Write(byte[] data)
        {
            lock (_syncRoot)
            {
                if (_serialPort is null) throw new InvalidOperationException("串口未打开。");
                _serialPort.Write(data, 0, data.Length);
            }
        }

        private void OnDataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            var port = _serialPort;
            if (port is null || !port.IsOpen) return;

            byte[] buffer;
            try
            {
                var count = port.BytesToRead;
                if (count <= 0) return;
                buffer = new byte[count];
                var read = port.Read(buffer, 0, count);
                if (read <= 0) return;
                if (read < count) Array.Resize(ref buffer, read);
            }
            catch
            {
                // 保留原有行为：关闭串口期间的读取异常不产生消息。
                return;
            }

            DataReceived?.Invoke(this, buffer);
        }

        public void Dispose() => Close();
    }
}
