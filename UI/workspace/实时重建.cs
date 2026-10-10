namespace Tomography.UI.windows;

public sealed partial class AppWindow
{
    private bool _pendingRealtimeReconstruction;
    private bool _realtimeEnabled;
    private void SetRealtimeReconstruction(bool enabled)
    {
        _realtimeEnabled = enabled;
        AppTitleBar.RealtimeEnabled = enabled;
        if (enabled) QueueRealtimeReconstruction();
        else _pendingRealtimeReconstruction = false;
    }
    private void QueueRealtimeReconstruction()
    {
        if (!_realtimeEnabled) return;
        if (_acquiringReference) { SetRealtimeReconstruction(false); return; }
        if (_reconstruction.Measurements is null || _reconstruction.Reference is null || _reconstruction.SensitivityPath is null)
        { SetRealtimeReconstruction(false); ConnectionStatus.Text = "实时重建需要灵敏度矩阵、空场参考和物场数据"; return; }
        _pendingRealtimeReconstruction = true;
        if (!_reconstructing) RunReconstruction(true);
    }
}
