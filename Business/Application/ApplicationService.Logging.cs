using Tomography.Business.Helpers;
using Tomography.Business.Interfaces;
using Tomography.Business.Models;

namespace Tomography.Business
{
    /// <summary>日志事件、收发统计、导出及时间显示。</summary>
    public sealed partial class ApplicationService : IApplicationService
    {
        public OperationResult ExportLogs(string targetPath) => Report(_logService.Export(State.LogPath, targetPath));
        public string GetExportFileName() => $"serial_{DateTime.Now:yyyyMMdd_HHmmss}.log";
        public string FormatTimestamp(DateTime timestamp) => TimeHelper.Format(timestamp);

        private async void OnDataReceived(object? sender, LogEntry entry) => await HandleLogEntryAsync(entry, true);
        private async void OnDataSent(object? sender, LogEntry entry) => await HandleLogEntryAsync(entry, false);

        private async Task HandleLogEntryAsync(LogEntry entry, bool isReceived)
        {
            string path;
            string encoding;
            lock (_stateLock)
            {
                if (_disposed) return;
                path = _settings.LogFilePath;
                encoding = _settings.DefaultEncodingName;
            }
            var result = await _logService.AppendAsync(entry, path, encoding).ConfigureAwait(false);
            ApplicationState state;
            lock (_stateLock)
            {
                if (_disposed) return;
                _state = _state with
                {
                    ReceivedBytes = _state.ReceivedBytes + (isReceived ? entry.RawData.Length : 0),
                    SentBytes = _state.SentBytes + (isReceived ? 0 : entry.RawData.Length),
                    ErrorCount = _state.ErrorCount + (result.IsSuccess ? 0 : 1),
                    Status = result.IsSuccess ? _state.Status : result.Message
                };
                state = _state;
            }
            LogAdded?.Invoke(this, entry);
            StateChanged?.Invoke(this, state);
        }
    }
}
