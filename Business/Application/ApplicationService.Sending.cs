using Tomography.Business.Helpers;
using Tomography.Business.Interfaces;
using Tomography.Business.Models;

namespace Tomography.Business
{
    /// <summary>发送输入检查及 HEX、文本发送。</summary>
    public sealed partial class ApplicationService : IApplicationService
    {
        public OperationResult Send(string text, bool isHex, string encodingName)
        {
            if (string.IsNullOrWhiteSpace(text)) return Report(Fail("发送内容不能为空。"));
            if (isHex)
            {
                if (!HexConverter.TryToByteArray(text, out var bytes)) return Report(Fail("HEX 内容格式错误。"));
                return Report(_serialService.SendBytes(bytes));
            }
            if (!EncodingHelper.IsSupportedEncoding(encodingName)) return Report(Fail("编码名称无效。"));
            return Report(_serialService.SendText(text, encodingName));
        }
    }
}
