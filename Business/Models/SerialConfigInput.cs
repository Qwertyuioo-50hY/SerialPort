namespace Tomography.Business.Models
{
    /// <summary>界面输入及回显数据，解析与默认值由业务层处理。</summary>
    public sealed record SerialConfigInput
    {
        public string PortName { get; init; } = string.Empty;
        public string BaudRate { get; init; } = string.Empty;
        public string Parity { get; init; } = string.Empty;
        public string DataBits { get; init; } = string.Empty;
        public string StopBits { get; init; } = string.Empty;
        public string Handshake { get; init; } = string.Empty;
        public string EncodingName { get; init; } = string.Empty;
    }
}
