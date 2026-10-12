using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Tomography.Business.Services;

namespace Tomography.UI.windows;

public sealed partial class AppWindow
{
    private readonly AssetService _assets = new();
    private bool _fileOperation;
    private async Task ImportFileAsync(string role)
    {
        if (role is "灵敏度矩阵" or "节点坐标" or "物场/Demo数据" or "空场参考")
        { await ImportExperimentalInputAsync(role); return; }
        if (_fileOperation || _reconstructing || _sampling) return;
        _fileOperation = true;
        try
        {
            var path = await PickImportPathAsync(role);
            if (path is null) return;
            var extension = System.IO.Path.GetExtension(path).ToLowerInvariant();
            if (extension is ".csv" or ".txt")
            {
                var roles = role == "灵敏度矩阵" ? new[] { "灵敏度矩阵", "节点坐标" } : new[] { "物场/Demo数据", "空场参考", "仅保存文件" };
                var choice = new ComboBox { Header = "文件用途", ItemsSource = roles, SelectedIndex = 0, HorizontalAlignment = HorizontalAlignment.Stretch };
                var dialog = new ContentDialog { XamlRoot = Root.XamlRoot, RequestedTheme = Root.ActualTheme,
                    Title = "导入数值文件", Content = choice, PrimaryButtonText = "导入", CloseButtonText = "取消" };
                if (await dialog.ShowAsync() != ContentDialogResult.Primary) return;
                role = (string)choice.SelectedItem;
            }
            var asset = await Task.Run(() => _assets.Import(path));
            _reconstruction.UseAsset(asset, role);
            ProjectPanel.AddAsset(asset);
            ProjectPanel.Visibility = Visibility.Visible;
            ProjectColumn.Width = new GridLength(240);
            DirectoryPanel.SetAsset(role, asset);
            if (role == "物场/Demo数据") Workspace.ShowMeasurements(_reconstruction.Measurements!);
            if (extension is ".png" or ".jpg" or ".jpeg" or ".bmp" or ".tif" or ".tiff") Workspace.PreviewImage(asset.Path);
            ConnectionStatus.Text = $"已导入 {asset.Name}（{role}）";
        }
        catch (Exception ex) { if (!_connectionLifetime.IsCancellationRequested) ConnectionStatus.Text = ex.Message; }
        finally { _fileOperation = false; }
    }
}
