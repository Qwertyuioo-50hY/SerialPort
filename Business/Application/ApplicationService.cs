using Tomography.Business.Interfaces;
using Tomography.Business.Models;
using Tomography.Business.Services;
using Tomography.Data;

namespace Tomography.Business
{
    /// <summary>业务入口、状态通知与资源生命周期。</summary>
    public sealed partial class ApplicationService : IApplicationService
    {
        private readonly ISerialService _serialService;
        private readonly ILogService _logService;
        private readonly IConfigService _configService;
        private readonly object _stateLock = new();
        private AppSettings _settings = AppSettings.CreateDefault();
        private ApplicationState _state = new();
        private bool _disposed;

        public ApplicationService(ISerialService? serialService = null, ILogService? logService = null, IConfigService? configService = null)
        {
            var storage = new FileStorage();
            _serialService = serialService ?? new SerialService();
            _logService = logService ?? new LogService(storage: storage);
            _configService = configService ?? new ConfigService(storage);
            _serialService.DataReceived += OnDataReceived;
            _serialService.DataSent += OnDataSent;
        }

        public ApplicationState State
        {
            get { lock (_stateLock) return _state; }
        }

        public event EventHandler<ApplicationState>? StateChanged;
        public event EventHandler<LogEntry>? LogAdded;

        public void Initialize()
        {
            LoadConfig();
            RefreshPorts();
        }

        private OperationResult Report(OperationResult result)
        {
            ApplicationState state;
            lock (_stateLock)
            {
                if (_disposed) return result;
                _state = _state with
                {
                    IsOpen = _serialService.IsOpen,
                    Status = result.Message,
                    ErrorCount = _state.ErrorCount + (result.IsSuccess ? 0 : 1)
                };
                state = _state;
            }
            StateChanged?.Invoke(this, state);
            return result;
        }

        private static OperationResult Fail(string message, Exception? ex = null) => new()
        {
            IsSuccess = false,
            Message = message,
            Exception = ex
        };

        public void Dispose()
        {
            lock (_stateLock)
            {
                if (_disposed) return;
                _disposed = true;
            }
            _serialService.DataReceived -= OnDataReceived;
            _serialService.DataSent -= OnDataSent;
            _serialService.Dispose();
        }
    }
}
