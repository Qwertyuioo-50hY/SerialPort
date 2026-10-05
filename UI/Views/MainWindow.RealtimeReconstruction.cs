namespace Tomography.UI.Views;

public sealed partial class MainWindow
{
    private bool _pendingRealtimeReconstruction;
    private void QueueRealtimeReconstruction()
    {
        if (_reconstructionSettings?.RealtimeEnabled != true) return;
        if (_acquiringReference) { _reconstructionSettings.StopRealtime(); return; }
        if (_reconstruction.Measurements is null || _reconstruction.Reference is null || _reconstruction.SensitivityPath is null)
        { _reconstructionSettings.StopRealtime(); ConnectionStatus.Text = "实时重建需要灵敏度矩阵、空场参考和物场数据"; return; }
        _pendingRealtimeReconstruction = true;
        if (!_reconstructing) RunReconstruction(true);
    }
}
