using Tomography.Business.Models;

namespace Tomography.Business.Interfaces;

/// <summary>C# 图像显示接口，接收 Python 返回的二维矩阵。</summary>
public interface IReconstructionImageSink
{
    void SetImage(ReconstructionImage matrix);
}
