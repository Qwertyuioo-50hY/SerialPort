using Tomography.Business.Models;
using Tomography.Data;

namespace Tomography.Business.Services;

/// <summary>重建输入和结果，供采样与文件导入共用。</summary>
public sealed class ReconstructionSession
{
    public string? SensitivityPath { get; private set; }
    public string? CoordinatesPath { get; private set; }
    public string? CustomAlgorithmPath { get; private set; }
    public double[]? Measurements { get; private set; }
    public double[]? Reference { get; private set; }
    public ReconstructionImage? Image { get; set; }
    public void SetMeasurements(double[] values)
    {
        if (values.Length != 64 || values.Any(value => !double.IsFinite(value))) throw new ArgumentException("数据必须包含 64 个有限通道值。");
        Measurements = values.ToArray();
    }
    public void SetReference(double[] values)
    {
        if (values.Length != 64 || values.Any(value => !double.IsFinite(value))) throw new ArgumentException("空场参考必须为 64 个有限值。");
        Reference = values.ToArray();
    }
    public void CaptureReference()
    {
        Reference = Measurements?.ToArray() ?? throw new InvalidOperationException("请先采样或导入 64 通道空场数据。");
    }
    public void UseAsset(ImportedAsset asset, string role)
    {
        switch (role)
        {
            case "灵敏度矩阵": SensitivityPath = asset.Path; break;
            case "节点坐标": CoordinatesPath = asset.Path; break;
            case "自定义算法": CustomAlgorithmPath = asset.Path; break;
            case "物场/Demo数据": SetMeasurements(CsvMatrixStorage.Read(asset.Path).SelectMany(row => row).ToArray()); break;
            case "空场参考":
                var values = CsvMatrixStorage.Read(asset.Path).SelectMany(row => row).ToArray();
                if (values.Length != 64) throw new ArgumentException("空场参考必须包含 64 个通道值。");
                Reference = values; break;
        }
    }
    public ReconstructionRequest CreateRequest(string algorithm, string mode, double lambda, double alpha, int iterations, double tvMu)
    {
        if (SensitivityPath is null || Measurements is null || Reference is null)
            throw new InvalidOperationException("请导入灵敏度矩阵，并提供物场数据和空场参考。");
        if (algorithm == "Custom" && CustomAlgorithmPath is null) throw new InvalidOperationException("请先导入自定义 Python 算法文件。");
        return new ReconstructionRequest(algorithm, SensitivityPath, Measurements.ToArray(), Reference.ToArray(), mode,
            CoordinatesPath, CustomAlgorithmPath, lambda, alpha, iterations, TvMu: tvMu);
    }
}
