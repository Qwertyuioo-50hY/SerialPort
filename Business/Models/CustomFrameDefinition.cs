namespace Tomography.Business.Models;

public sealed class CustomFrameDefinition
{
    public string HeaderHex { get; set; } = "";
    public string FooterHex { get; set; } = "";
    public int ChecksumStart { get; set; }
    public int Length { get; set; }
    public int DataOffset { get; set; }
    public string FieldType { get; set; } = "UInt32";
    public bool BigEndian { get; set; }
    public string Checksum { get; set; } = "None";
    public int[] ChannelMap { get; set; } = Enumerable.Range(0, 64).ToArray();
}
