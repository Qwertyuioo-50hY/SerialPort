namespace Tomography.Business.Models
{
    public sealed record ConfigurationOptions(
        IReadOnlyList<string> BaudRates,
        IReadOnlyList<string> DataBits,
        IReadOnlyList<string> Parities,
        IReadOnlyList<string> StopBits,
        IReadOnlyList<string> Handshakes,
        IReadOnlyList<string> Encodings);
}
