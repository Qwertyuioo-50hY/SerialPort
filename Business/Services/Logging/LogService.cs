using Tomography.Business.Configuration;
using Tomography.Business.Helpers;
using Tomography.Business.Interfaces;
using Tomography.Business.Models;
using Tomography.Data;
using Tomography.Data.Interfaces;

namespace Tomography.Business.Services
{
    /// <summary>日志追加、导出及文件路径管理。</summary>
    public partial class LogService : ILogService
    {
        private readonly IFileStorage _storage;
        private readonly string _defaultFilePath;
        private readonly string _defaultEncodingName;

        /// <summary>
        /// 初始化日志服务。
        /// </summary>
        /// <param name="defaultFilePath">默认日志文件路径。</param>
        /// <param name="defaultEncodingName">默认日志编码名称。</param>
        public LogService(string? defaultFilePath = null, string? defaultEncodingName = null, IFileStorage? storage = null)
        {
            _storage = storage ?? new FileStorage();
            _defaultFilePath = string.IsNullOrWhiteSpace(defaultFilePath) ? AppConstants.DefaultLogFilePath : defaultFilePath;
            _defaultEncodingName = string.IsNullOrWhiteSpace(defaultEncodingName) ? AppConstants.DefaultEncodingName : defaultEncodingName;
        }

        /// <summary>
        /// 同步追加日志。
        /// </summary>
        public OperationResult Append(LogEntry entry, string? filePath = null, string? encodingName = null)
        {
            return AppendAsync(entry, filePath, encodingName).GetAwaiter().GetResult();
        }

        /// <summary>
        /// 异步追加日志，内部使用锁保证并发顺序。
        /// </summary>
        public async Task<OperationResult> AppendAsync(LogEntry entry, string? filePath = null, string? encodingName = null, CancellationToken cancellationToken = default)
        {
            if (entry is null)
            {
                return Fail("日志条目不能为空。");
            }

            var targetPath = ResolveFilePath(filePath);
            var line = Format(entry) + Environment.NewLine;
            try
            {
                var bytes = EncodingHelper.GetBytes(line, encodingName ?? _defaultEncodingName);
                await _storage.AppendAsync(targetPath, bytes, cancellationToken).ConfigureAwait(false);
                return new OperationResult { IsSuccess = true, Message = "日志写入成功。", Data = targetPath };
            }
            catch (Exception ex)
            {
                return Fail("日志写入失败。", ex);
            }
        }

        public OperationResult Export(string sourcePath, string targetPath)
        {
            try
            {
                var bytes = _storage.Exists(sourcePath) ? _storage.Read(sourcePath) : Array.Empty<byte>();
                _storage.Write(targetPath, bytes);
                return new OperationResult { IsSuccess = true, Message = $"日志已导出: {targetPath}", Data = targetPath };
            }
            catch (Exception ex)
            {
                return Fail($"导出日志失败: {ex.Message}", ex);
            }
        }


        /// <summary>
        /// 解析日志文件路径。
        /// </summary>
        private string ResolveFilePath(string? filePath)
        {
            return string.IsNullOrWhiteSpace(filePath) ? _defaultFilePath : filePath;
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
    }
}
