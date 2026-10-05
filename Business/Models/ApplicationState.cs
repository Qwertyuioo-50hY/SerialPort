namespace Tomography.Business.Models
{
    /// <summary>业务状态快照，不包含控件、颜色或线程上下文。</summary>
    public sealed record ApplicationState
    {
        public bool IsOpen { get; init; }
        public long ReceivedBytes { get; init; }
        public long SentBytes { get; init; }
        public int ErrorCount { get; init; }
        public string Status { get; init; } = string.Empty;
        public string LogPath { get; init; } = string.Empty;
        public string EncodingName { get; init; } = string.Empty;
        public SerialConfigInput Configuration { get; init; } = new();
        public IReadOnlyList<string> Ports { get; init; } = Array.Empty<string>();
    }
}
