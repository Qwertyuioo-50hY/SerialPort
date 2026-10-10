using Microsoft.Windows.Storage.Pickers;
using Tomography.Business.Services;

namespace Tomography.UI.Views;

public sealed partial class AppWindow
{
    private async Task ExportAcquisitionAsync(bool mat)
    {
        if (_fileOperation) return;
        var snapshot = _acquisitionRecords.Snapshot();
        if (snapshot.ObjectFrames.Length == 0 && snapshot.ReferenceFrames.Length == 0)
        { ConnectionStatus.Text = "尚无采集记录"; return; }
        _fileOperation = true;
        try
        {
            if (mat)
            {
                var path = await PickExportPathAsync("acquisition.mat", ".mat");
                if (path is null) return;
                await _pythonReconstruction.ExportAcquisitionMatAsync(snapshot, path,
                    _reconstructionSettings?.PythonDll ?? PythonRuntimeHost.FindPythonDll());
            }
            else
            {
                var folder = await new FolderPicker(AppWindow.Id).PickSingleFolderAsync();
                if (folder is null) return;
                await Task.Run(() => AcquisitionExportService.ExportCsv(snapshot, folder.Path));
            }
            if (!_connectionLifetime.IsCancellationRequested) ConnectionStatus.Text = "采集记录已导出";
        }
        catch (Exception ex) { if (!_connectionLifetime.IsCancellationRequested) ConnectionStatus.Text = ex.Message; }
        finally { _fileOperation = false; }
    }
}
