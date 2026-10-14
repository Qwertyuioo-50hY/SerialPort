using Tomography.Business.Services;
using Tomography.Business.Models;
using Tomography.Data;
using System.Text;
using System.Buffers.Binary;

if (args.Contains("--python-chat")) await PythonChatBridgeTests.RunAsync();
AiProjectStoreTests.Run();
await DeviceTransportTests.RunAsync();
var tests = 0;
void Check(bool condition, string message) { if (!condition) throw new Exception(message); tests++; }
void Reject(Action action, string message) { try { action(); } catch (Exception) { tests++; return; } throw new Exception(message); }
var line = "frame:" + string.Join(",", Enumerable.Range(0, 64)) + "\r\n";
var decoder = new FireWaterDecoder();
Check(!decoder.Feed(Encoding.ASCII.GetBytes(line[..17])).Any(), "FireWater decoded an incomplete line");
var frames = decoder.Feed(Encoding.ASCII.GetBytes(line[17..] + line));
Check(frames.Count() == 2 && frames.First()[63] == 63, "FireWater split/coalesced frames");
Check(new FireWaterDecoder().Feed(Encoding.ASCII.GetBytes(line[(line.IndexOf(':') + 1)..])).Single()[32] == 32, "plain CSV input");
Check(new FireWaterDecoder().Feed(Encoding.ASCII.GetBytes("prefix:" + line)).Single()[0] == 0, "last colon prefix");
Reject(() => new FireWaterDecoder().Feed(Encoding.ASCII.GetBytes("f:1,2\n")), "wrong channel count accepted");
Reject(() => new FireWaterDecoder().Feed(Encoding.ASCII.GetBytes(line.Replace(",63", ",NaN"))), "nonfinite input accepted");
Reject(() => new FireWaterDecoder().Feed(Enumerable.Repeat((byte)65, 65537).ToArray()), "unbounded line accepted");
foreach (var big in new[] { false, true })
foreach (var checksum in new[] { "None", "XOR8", "SUM8" })
{
    var definition = new CustomFrameDefinition { HeaderHex = "AA55", DataOffset = 2, Length = 258 + (checksum == "None" ? 0 : 1), BigEndian = big, Checksum = checksum };
    var bytes = new byte[definition.Length]; bytes[0] = 0xAA; bytes[1] = 0x55;
    for (var i = 0; i < 64; i++) { if (big) BinaryPrimitives.WriteUInt32BigEndian(bytes.AsSpan(2 + 4 * i), (uint)(i + 100)); else BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(2 + 4 * i), (uint)(i + 100)); }
    if (checksum != "None") { foreach (var b in bytes.Take(bytes.Length - 1)) bytes[^1] = checksum == "XOR8" ? (byte)(bytes[^1] ^ b) : unchecked((byte)(bytes[^1] + b)); }
    var custom = new CustomFrameDecoder(definition);
    Check(!custom.Feed(new byte[] { 0, 1, 0xAA }).Any(), "partial header");
    Check(!custom.Feed(bytes[1..20]).Any(), "partial custom frame");
    var result = custom.Feed(bytes[20..].Concat(bytes).ToArray()).ToArray();
    Check(result.Length == 2 && result[0][63] == 163, "custom split/coalesced/endian/checksum");
    if (checksum != "None") { bytes[^1] ^= 1; Reject(() => new CustomFrameDecoder(definition).Feed(bytes), "bad checksum accepted"); }
}
Reject(() => new CustomFrameDecoder(new CustomFrameDefinition()), "undefined custom protocol accepted");
Reject(() => new CustomFrameDecoder(new CustomFrameDefinition { HeaderHex = "AA", Length = 256, DataOffset = 1 }), "bad frame size accepted");
var folder = Path.Combine(Path.GetTempPath(), "emt-input-tests-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(folder);
try
{
    var matrix = Path.Combine(folder, "sen.csv"); var xy = Path.Combine(folder, "xy.csv"); var channels = Path.Combine(folder, "data.csv");
    CsvMatrixStorage.Write(matrix, 3, 64, new double[192]); CsvMatrixStorage.Write(xy, 2, 3, new double[6]);
    Check(ExperimentInputValidator.ValidateSensitivity(matrix, "") == (64, 3), "64 x N matrix");
    Check(ExperimentInputValidator.ValidateCoordinates(xy, matrix) == 3, "node direction");
    CsvMatrixStorage.Write(matrix, 64, 3, new double[192]);
    Check(ExperimentInputValidator.ValidateSensitivity(matrix, "") == (3, 64), "N x 64 matrix");
    Check(ExperimentInputValidator.ValidateCoordinates(xy, matrix) == 3, "transposed node direction");
    CsvMatrixStorage.Write(channels, 64, 1, Enumerable.Range(0, 64).Select(i => (double)i).ToArray());
    Check(ExperimentInputValidator.ReadChannels(channels)[63] == 63, "channels import");
    File.WriteAllText(channels, "1,2\n"); Reject(() => ExperimentInputValidator.ReadChannels(channels), "wrong simulation dimension");
    File.WriteAllText(channels, "1,2\n3\n"); Reject(() => CsvMatrixStorage.Read(channels), "ragged CSV");
    File.WriteAllText(channels, "NaN,2\n"); Reject(() => CsvMatrixStorage.Read(channels), "NaN CSV");
    File.WriteAllText(channels, ""); Reject(() => CsvMatrixStorage.Read(channels), "empty CSV");
    File.WriteAllText(channels, "label,value\n"); Reject(() => CsvMatrixStorage.Read(channels), "header CSV");
    CsvMatrixStorage.Write(xy, 2, 4, new double[8]); Reject(() => ExperimentInputValidator.ValidateCoordinates(xy, matrix), "wrong node count");
    var session = new ReconstructionSession(); session.SetReference(new double[64]); session.InvalidateReference();
    Check(session.ReferenceNeedsReview && session.Reference is not null, "reference history invalidation");
    session.SetReference(new double[64]); Check(!session.ReferenceNeedsReview, "reference refresh");
    Reject(() => session.SetReference(Enumerable.Repeat(double.NaN, 64).ToArray()), "bad reference accepted");
    Check(session.Reference!.All(v => v == 0), "invalid import changed valid reference");
    session.UseAsset(new ImportedAsset("sen.csv", matrix), "灵敏度矩阵"); session.RemoveSensitivity(); Check(session.SensitivityPath is null, "matrix removal");
    var config = new ConfigService(); var settings = new AppSettings { Device = new DeviceConfiguration { FrequencyKhz = 123, CoilCount = 9 } };
    var path = Path.Combine(folder, "settings.json"); Check(config.Save(settings, path).IsSuccess, "save project settings");
    Check(((AppSettings)config.Load(path).Data!).Device.FrequencyKhz == 123, "restore project settings");
}
finally { Directory.Delete(folder, true); }
Console.WriteLine($"PASS: {tests} decoder, matrix, reference and persistence checks; no hardware operated.");
