using Microsoft.UI.Xaml;

namespace Tomography.UI.windows;

public sealed partial class AppWindow
{
    private bool _pendingRealtimeReconstruction;
    private bool _realtimeEnabled;
    private DispatcherTimer? _reconstructionTimer;
    private void UpdateReconstructionInterval()
    {
        if (_reconstructionTimer is not null)
            _reconstructionTimer.Interval = TimeSpan.FromSeconds(1 / Workspace.ReconstructionFramesPerSecond);
    }
    private void SetRealtimeReconstruction(bool enabled)
    {
        if (enabled && (_acquiringReference || _reconstruction.Reference is null || _reconstruction.SensitivityPath is null))
        {
            ConnectionStatus.Text = "实时重建需要先导入灵敏度矩阵并完成空场采集";
            enabled = false;
        }
        _realtimeEnabled = enabled;
        AppTitleBar.RealtimeEnabled = enabled;
        _reconstructionSettings?.SetBusy(enabled || _reconstructing);
        if (enabled)
        {
            if (_reconstructionTimer is null)
            {
                _reconstructionTimer = new DispatcherTimer();
                _reconstructionTimer.Tick += (_, _) =>
                {
                    if (_realtimeEnabled && _pendingRealtimeReconstruction && !_reconstructing)
                        RunReconstruction(true);
                };
            }
            UpdateReconstructionInterval();
            _reconstructionTimer.Start();
            if (_reconstruction.Measurements is not null) QueueRealtimeReconstruction();
        }
        else
        {
            _reconstructionTimer?.Stop();
            _pendingRealtimeReconstruction = false;
        }
    }
    private void QueueRealtimeReconstruction()
    {
        if (_realtimeEnabled && !_acquiringReference && _reconstruction.Measurements is not null)
            _pendingRealtimeReconstruction = true;
    }
}
