using Tomography.Business.Models;

namespace Tomography.Business.Services;

/// <summary>本次会话的空场原始帧、空场均值和物场原始帧。</summary>
public sealed class AcquisitionRecords
{
    private double[][] _referenceFrames = Array.Empty<double[]>();
    private double[]? _referenceMean;
    private readonly List<double[]> _objectFrames = new();
    public void AddObject(double[] frame) => _objectFrames.Add(frame.ToArray());
    public void SetReference(IEnumerable<double[]> frames, double[] mean)
    { _referenceFrames = frames.Select(frame => frame.ToArray()).ToArray(); _referenceMean = mean.ToArray(); }
    public void AddObject(uint[] frame) => _objectFrames.Add(frame.Select(value => (double)value).ToArray());
    public void SetReference(IEnumerable<uint[]> frames, double[] mean)
    {
        _referenceFrames = frames.Select(frame => frame.Select(value => (double)value).ToArray()).ToArray();
        _referenceMean = mean.ToArray();
    }
    public AcquisitionSnapshot Snapshot() => new(_referenceFrames.Select(frame => frame.ToArray()).ToArray(),
        _objectFrames.Select(frame => frame.ToArray()).ToArray(), _referenceMean?.ToArray());
}
