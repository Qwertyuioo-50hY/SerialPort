using Tomography.Business.Services;

namespace Tomography.UI.windows;

public sealed partial class AppWindow
{
    private async Task ExportFileAsync()
    {
        if (_fileOperation) return;
        var asset = ProjectPanel.SelectedAsset;
        if (asset is null) { ConnectionStatus.Text = "请在项目导航选择要导出的文件"; return; }
        _fileOperation = true;
        try
        {
            var target = await PickExportPathAsync(asset.Name, System.IO.Path.GetExtension(asset.Path));
            if (target is null) return;
            await Task.Run(() => _assets.Export(asset, target));
            ConnectionStatus.Text = $"已导出 {asset.Name}";
        }
        catch (Exception ex) { if (!_connectionLifetime.IsCancellationRequested) ConnectionStatus.Text = ex.Message; }
        finally { _fileOperation = false; }
    }
    private async Task ExportReconstructionAsync(bool bitmap)
    {
        if (_fileOperation) return;
        var image = _reconstruction.Image;
        if (image is null) { ConnectionStatus.Text = "请先完成二维重建"; return; }
        _fileOperation = true;
        try
        {
            var extension = bitmap ? ".bmp" : ".csv";
            var target = await PickExportPathAsync("reconstruction" + extension, extension);
            if (target is null) return;
            await Task.Run(() =>
            {
                if (bitmap) ReconstructionExportService.ExportBitmap(image, target);
                else ReconstructionExportService.ExportCsv(image, target);
            });
            ConnectionStatus.Text = "已导出重建结果";
        }
        catch (Exception ex) { if (!_connectionLifetime.IsCancellationRequested) ConnectionStatus.Text = ex.Message; }
        finally { _fileOperation = false; }
    }
}
