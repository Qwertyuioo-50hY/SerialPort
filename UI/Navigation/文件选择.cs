using Microsoft.Windows.Storage.Pickers;

namespace Tomography.UI.windows;

public sealed partial class AppWindow
{
    private async Task<string?> PickImportPathAsync(string role)
    {
        var picker = new FileOpenPicker(AppWindow.Id);
        var extensions = role switch
        {
            "自定义算法" => new[] { ".py" },
            "灵敏度矩阵" => new[] { ".mat", ".csv" },
            "节点坐标" or "物场/Demo数据" or "空场参考" => new[] { ".csv", ".txt" },
            _ => new[] { ".csv", ".txt", ".png", ".jpg", ".jpeg", ".bmp", ".tif", ".tiff" }
        };
        foreach (var extension in extensions) picker.FileTypeFilter.Add(extension);
        return (await picker.PickSingleFileAsync())?.Path;
    }
    private async Task<string?> PickExportPathAsync(string name, string extension)
    {
        var picker = new FileSavePicker(AppWindow.Id) { SuggestedFileName = System.IO.Path.GetFileNameWithoutExtension(name) };
        picker.FileTypeChoices.Add(extension, new List<string> { extension });
        return (await picker.PickSaveFileAsync())?.Path;
    }
}
