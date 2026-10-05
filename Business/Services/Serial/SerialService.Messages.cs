using Tomography.Business.Helpers;
using Tomography.Business.Interfaces;
using Tomography.Business.Models;
using Tomography.Business.Models.Enums;

namespace Tomography.Business.Services
{
    /// <summary>将收发字节转换为日志消息。</summary>
    public partial class SerialService : ISerialService
    {
        /// <summary>
        /// 构建统一日志消息对象。
        /// </summary>
        public LogEntry BuildMessage(byte[]? rawData, MessageDirection direction, DataDisplayMode displayMode, string? encodingName = null)
        {
            var payload = rawData ?? Array.Empty<byte>();
            var text = displayMode == DataDisplayMode.Hex
                ? HexConverter.ToHexString(payload)
                : EncodingHelper.GetString(payload, encodingName);

            return new LogEntry
            {
                Timestamp = DateTime.Now,
                Direction = direction,
                DisplayMode = displayMode,
                RawData = payload,
                Message = text
            };
        }
    }
}
