using Microsoft.UI.Xaml.Controls;
using Tomography.Business.Interfaces;
using Tomography.Business.Services;
using Tomography.UI.Controls;

namespace Tomography.UI.Views;

public sealed partial class MainWindow
{
    private readonly ReconstructionSession _reconstruction = new();
    private readonly IPythonReconstructionService _pythonReconstruction = new PythonReconstructionService();
    private ReconstructionSettingsView? _reconstructionSettings;
    private bool _reconstructing;
    private void WorkspaceReconstructionRequested(object? sender, EventArgs e) => ReconstructAsync();
    private void AddReconstructionSettings(Panel parent)
    {
        if (_reconstructionSettings is null)
        {
            _reconstructionSettings = new ReconstructionSettingsView();
            _reconstructionSettings.ReconstructRequested += (_, _) => ReconstructAsync();
            _reconstructionSettings.ReferenceRequested += (_, _) => AcquisitionRequested(this, Tomography.Business.Models.AcquisitionMode.Reference);
            _reconstructionSettings.RealtimeChanged += (_, _) =>
            {
                if (!_reconstructionSettings.RealtimeEnabled) _pendingRealtimeReconstruction = false;
                else QueueRealtimeReconstruction();
            };
            _reconstructionSettings.SyntaxCheckRequested += (_, _) => CheckPythonSyntaxAsync();
        }
        _reconstructionSettings.UpdateInputs(_reconstruction);
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
            do
            {
                _pendingRealtimeReconstruction = false;
                if (!IsInteger(settings.Iterations, 1, 10000)) throw new ArgumentException("迭代次数必须为 1–10000 的整数。");
                var request = _reconstruction.CreateRequest(settings.Algorithm, settings.DifferenceMode, settings.Lambda,
                    settings.Alpha, (int)settings.Iterations, settings.TvMu);
                var image = await _pythonReconstruction.ReconstructAsync(request, settings.PythonDll);
                if (_connectionLifetime.IsCancellationRequested || (automatic && !settings.RealtimeEnabled)) return;
                _reconstruction.Image = image;
                Workspace.SetReconstruction(image);
                ConnectionStatus.Text = $"{request.Algorithm} 重建完成：{image.Width} × {image.Height}";
                automatic = settings.RealtimeEnabled;
            } while (automatic && _pendingRealtimeReconstruction);
        }
        catch (Exception ex)
        {
            _pendingRealtimeReconstruction = false;
            if (!_connectionLifetime.IsCancellationRequested) { settings.StopRealtime(); ConnectionStatus.Text = ex.Message; }
        }
        finally { _reconstructing = false; if (!_connectionLifetime.IsCancellationRequested) settings.SetBusy(false); }
    }
}
