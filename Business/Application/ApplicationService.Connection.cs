using Tomography.Business.Interfaces;
using Tomography.Business.Models;

namespace Tomography.Business
{
    /// <summary>串口列表刷新与连接切换。</summary>
    public sealed partial class ApplicationService : IApplicationService
    {
        public OperationResult RefreshPorts()
        {
            try
            {
                var ports = _serialService.GetPortNames();
                lock (_stateLock)
                {
                    var config = _state.Configuration;
                    if (ports.Length > 0 && !ports.Contains(config.PortName, StringComparer.OrdinalIgnoreCase))
                    {
                        config = config with { PortName = ports[0] };
                    }
                    _state = _state with { Ports = ports, Configuration = config };
                }
                return Report(new OperationResult { IsSuccess = true, Message = $"已刷新串口，共 {ports.Length} 个。", Data = ports });
            }
            catch (Exception ex)
            {
                return Report(Fail("刷新串口失败。", ex));
            }
        }

        public OperationResult ToggleConnection(SerialConfigInput input)
        {
            if (_serialService.IsOpen) return Report(_serialService.Close());
            var validation = BuildConfig(input);
            if (!validation.IsSuccess) return Report(validation);
            var config = (SerialPortConfig)validation.Data!;
            var result = _serialService.Open(config);
            if (result.IsSuccess)
            {
                lock (_stateLock)
                {
                    _state = _state with { Configuration = ToInput(config), EncodingName = config.EncodingName };
                }
            }
            return Report(result);
        }
    }
}
