using System.IO.Ports;
using Tomography.Business.Configuration;
using Tomography.Business.Interfaces;
using Tomography.Business.Models;
using Tomography.Business.Services;

namespace Tomography.Business
{
    /// <summary>配置选项、读取保存及输入转换。</summary>
    public sealed partial class ApplicationService : IApplicationService
    {
        public ConfigurationOptions GetConfigurationOptions() => new(
            new[] { "1200", "2400", "4800", "9600", "19200", "38400", "57600", "115200" },
            new[] { "5", "6", "7", "8" },
            Enum.GetNames<Parity>(),
            Enum.GetNames<StopBits>().Where(n => n != nameof(StopBits.None)).ToArray(),
            Enum.GetNames<Handshake>(),
            new[] { "UTF-8", "GBK", "GB2312", "ASCII", "Unicode" });

        public OperationResult LoadConfig()
        {
            var result = _configService.LoadOrCreateDefault();
            if (result.IsSuccess && result.Data is AppSettings settings)
            {
                lock (_stateLock)
                {
                    _settings = settings;
                    _state = _state with
                    {
                        Configuration = ToInput(settings.DefaultSerialPortConfig),
                        LogPath = settings.LogFilePath,
                        EncodingName = settings.DefaultEncodingName
                    };
                }
            }
            return Report(result);
        }

        public OperationResult SaveConfig(SerialConfigInput input)
        {
            var validation = BuildConfig(input);
            if (!validation.IsSuccess) return Report(validation);
            var config = (SerialPortConfig)validation.Data!;
            AppSettings settings;
            lock (_stateLock)
            {
                settings = new AppSettings
                {
                    DefaultSerialPortConfig = config,
                    LogDirectoryPath = _settings.LogDirectoryPath,
                    LogFilePath = _settings.LogFilePath,
                    DefaultEncodingName = config.EncodingName,
                    RefreshIntervalMs = _settings.RefreshIntervalMs
                };
            }
            var result = _configService.Save(settings);
            if (result.IsSuccess)
            {
                lock (_stateLock)
                {
                    _settings = settings;
                    _state = _state with { Configuration = ToInput(config), EncodingName = config.EncodingName };
                }
            }
            return Report(result);
        }

        private OperationResult BuildConfig(SerialConfigInput input)
        {
            if (!int.TryParse(input.BaudRate, out var baudRate)) return Fail("波特率无效。");
            if (!int.TryParse(input.DataBits, out var dataBits)) return Fail("数据位无效。");
            if (!Enum.TryParse<Parity>(input.Parity, true, out var parity)) return Fail("校验位无效。");
            if (!Enum.TryParse<StopBits>(input.StopBits, true, out var stopBits)) return Fail("停止位无效。");
            if (!Enum.TryParse<Handshake>(input.Handshake, true, out var handshake)) return Fail("握手协议无效。");

            SerialPortConfig config;
            lock (_stateLock)
            {
                var existing = _settings.DefaultSerialPortConfig;
                config = new SerialPortConfig
                {
                    PortName = input.PortName.Trim(),
                    BaudRate = baudRate,
                    Parity = parity,
                    DataBits = dataBits,
                    StopBits = stopBits,
                    Handshake = handshake,
                    EncodingName = string.IsNullOrWhiteSpace(input.EncodingName) ? AppConstants.DefaultEncodingName : input.EncodingName,
                    ReadTimeout = existing.ReadTimeout,
                    WriteTimeout = existing.WriteTimeout,
                    DtrEnable = existing.DtrEnable,
                    RtsEnable = existing.RtsEnable
                };
            }
            var error = SerialService.ValidateConfig(config);
            return error is null ? new OperationResult { IsSuccess = true, Data = config } : Fail(error);
        }

        private static SerialConfigInput ToInput(SerialPortConfig config) => new()
        {
            PortName = config.PortName,
            BaudRate = config.BaudRate.ToString(),
            Parity = config.Parity.ToString(),
            DataBits = config.DataBits.ToString(),
            StopBits = config.StopBits.ToString(),
            Handshake = config.Handshake.ToString(),
            EncodingName = config.EncodingName
        };
    }
}
