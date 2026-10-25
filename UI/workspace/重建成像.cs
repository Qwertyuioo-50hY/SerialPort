using Microsoft.UI.Xaml.Controls;
using Tomography.Business.Interfaces;
using Tomography.Business.Services;
using Tomography.UI.SideView;

namespace Tomography.UI.windows;

public sealed partial class AppWindow
{
    private readonly ReconstructionSession _reconstruction = new();
    private readonly IPythonReconstructionService _pythonReconstruction = new PythonReconstructionService();
    private AlgorithmParametersView? _reconstructionSettings;
    private bool _reconstructing;
    private void WorkspaceReconstructionRequested(object? sender, EventArgs e) => ReconstructAsync();
    private void AddReconstructionSettings(Panel parent)
    {
        _reconstructionSettings ??= new AlgorithmParametersView();
        parent.Children.Add(_reconstructionSettings);
    }
    private void ReconstructAsync() => RunReconstruction(false);
    private async void RunReconstruction(bool automatic)
    {
        if (_reconstructing || _connecting || _fileOperation || _acquiringReference) return;
        if (_reconstructionSettings is null) { ViewModel.Section = "算法"; ViewModel.ParametersVisible = true; return; }
        var settings = _reconstructionSettings;
        _reconstructing = true; settings.SetBusy(true);
        try
        {
            _pendingRealtimeReconstruction = false;
            if ((settings.Algorithm is "Landweber" or "TV") && !IsInteger(settings.Iterations, 1, 10000)) throw new ArgumentException("迭代次数必须为 1–10000 的整数。");
            var request = _reconstruction.CreateRequest(settings.Algorithm, settings.DifferenceMode,
                settings.Algorithm == "Tikhonov" ? settings.Lambda : 0.1,
                settings.Algorithm == "Landweber" ? settings.Alpha : 0.1,
                settings.Algorithm is "Landweber" or "TV" ? (int)settings.Iterations : 50,
                settings.Algorithm == "TV" ? settings.TvMu : 0.05);
            var image = await _pythonReconstruction.ReconstructAsync(request, settings.PythonDll);
            if (_connectionLifetime.IsCancellationRequested || (automatic && !_realtimeEnabled)) return;
            _reconstruction.Image = image;
            Workspace.SetReconstruction(image, _deviceSettings.ArrayType == "R");
            ConnectionStatus.Text = $"{request.Algorithm} 重建完成：{image.Width} × {image.Height}";
        }
        catch (Exception ex)
        {
            _pendingRealtimeReconstruction = false;
            if (!_connectionLifetime.IsCancellationRequested) { SetRealtimeReconstruction(false); ConnectionStatus.Text = ex.Message; }
        }
        finally { _reconstructing = false; if (!_connectionLifetime.IsCancellationRequested) settings.SetBusy(_realtimeEnabled); }
    }
}
