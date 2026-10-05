using System;
using Tomography.Business.Models;

namespace Tomography.Business.Interfaces
{
    /// <summary>
    /// 日志服务接口，负责日志格式化与持久化。
    /// </summary>
    public interface ILogService
    {
        /// <summary>
        /// 追加写入一条日志。
        /// </summary>
        /// <param name="entry">日志条目。</param>
        /// <param name="filePath">日志文件路径，空则使用默认路径。</param>
        /// <param name="encodingName">编码名称，空则使用默认编码。</param>
        /// <returns>操作结果。</returns>
        OperationResult Append(LogEntry entry, string? filePath = null, string? encodingName = null);

        /// <summary>
        /// 异步追加写入一条日志。
        /// </summary>
        /// <param name="entry">日志条目。</param>
        /// <param name="filePath">日志文件路径，空则使用默认路径。</param>
        /// <param name="encodingName">编码名称，空则使用默认编码。</param>
        /// <param name="cancellationToken">取消令牌。</param>
        /// <returns>操作结果。</returns>
        Task<OperationResult> AppendAsync(LogEntry entry, string? filePath = null, string? encodingName = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// 将日志条目格式化为可写入文本。
        /// </summary>
        /// <param name="entry">日志条目。</param>
        /// <returns>格式化后的日志文本（不包含换行）。</returns>
        string Format(LogEntry entry);

        OperationResult Export(string sourcePath, string targetPath);
    }
}
