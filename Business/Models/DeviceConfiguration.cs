namespace Tomography.Business.Models;

public sealed class DeviceConfiguration
{
    public int ProtocolVersion { get; set; }
    public string LocalHost { get; set; } = "0.0.0.0";
    public int LocalPort { get; set; } = 1347;
    public int Source { get; set; }
    public int Connection { get; set; }
    public int Protocol { get; set; }
    public string Host { get; set; } = "127.0.0.1";
    public int Port { get; set; } = 1346;
    public int TimeoutSeconds { get; set; } = 3;
    public int FrequencyKhz { get; set; } = 100;
    public string ArrayType { get; set; } = "R";
    public string ExcitationMode { get; set; } = "V";
    public int CoilCount { get; set; } = 8;
}
