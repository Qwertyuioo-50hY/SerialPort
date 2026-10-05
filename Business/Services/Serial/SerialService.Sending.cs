using Tomography.Business.Helpers;
using Tomography.Business.Interfaces;
using Tomography.Business.Models;
using Tomography.Business.Models.Enums;

namespace Tomography.Business.Services
{
    /// <summary>发送字节、文本及附加校验值。</summary>
    public partial class SerialService : ISerialService
    {
        /// <summary>
        /// 发送字节数据，可按需附加校验值。
        /// </summary>
        public OperationResult SendBytes(byte[]? data, ChecksumType checksumType = ChecksumType.None, bool appendChecksum = false)
        {
            ThrowIfDisposed();

            if (data is null || data.Length == 0)
            {
                return Fail("发送数据不能为空。");
            }

            if (!IsOpen)
            {
                return Fail("串口未打开。");
            }

            try
            {
                var payload = data.ToArray();
                if (appendChecksum && checksumType != ChecksumType.None)
                {
                    var checksum = _checksumService.ComputeChecksumBytes(payload, checksumType);
                    payload = payload.Concat(checksum).ToArray();
                }

                _transport.Write(payload);

                var message = BuildMessage(payload, MessageDirection.Send, DataDisplayMode.Hex, _config?.EncodingName);
                DataSent?.Invoke(this, message);

                return Success("发送成功。", payload);
            }
            catch (Exception ex)
            {
                return Fail("发送失败。", ex);
            }
        }

        /// <summary>
        /// 发送文本数据。
        /// </summary>
        public OperationResult SendText(string? text, string? encodingName = null, ChecksumType checksumType = ChecksumType.None, bool appendChecksum = false)
        {
            ThrowIfDisposed();

            if (string.IsNullOrEmpty(text))
            {
                return Fail("发送文本不能为空。");
            }

            var finalEncoding = string.IsNullOrWhiteSpace(encodingName) ? _config?.EncodingName : encodingName;
            var bytes = EncodingHelper.GetBytes(text, finalEncoding);
            return SendBytes(bytes, checksumType, appendChecksum);
        }
    }
}
