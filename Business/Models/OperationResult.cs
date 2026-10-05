using System;

namespace Tomography.Business.Models
{
    /// <summary>
    /// 表示一个操作的结果。
    /// </summary>
    public class OperationResult
    {
        /// <summary>
        /// 操作是否成功。
        /// </summary>
        public bool IsSuccess { get; set; }

        /// <summary>
        /// 操作相关的消息，通常用于错误说明或提示信息。
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// 可选的返回数据，操作成功时可携带结果对象。
        /// </summary>
        public object? Data { get; set; }

        /// <summary>
        /// 如果操作抛出异常，则包含该异常信息，供调试使用。
        /// </summary>
        public Exception? Exception { get; set; }
    }
}
