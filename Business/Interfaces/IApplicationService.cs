using Tomography.Business.Models;

namespace Tomography.Business.Interfaces
{
    public interface IApplicationService : IDisposable
    {
        ApplicationState State { get; }
        event EventHandler<ApplicationState>? StateChanged;
        event EventHandler<LogEntry>? LogAdded;
        ConfigurationOptions GetConfigurationOptions();
        void Initialize();
        OperationResult RefreshPorts();
        OperationResult LoadConfig();
        OperationResult SaveConfig(SerialConfigInput input);
        OperationResult ToggleConnection(SerialConfigInput input);
        OperationResult Send(string text, bool isHex, string encodingName);
        OperationResult ExportLogs(string targetPath);
        string GetExportFileName();
        string FormatTimestamp(DateTime timestamp);
    }
}
