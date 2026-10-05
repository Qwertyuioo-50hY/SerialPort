using Microsoft.Windows.Storage.Pickers;

namespace Tomography.UI.Views;

public sealed partial class MainWindow
{
    private async Task<string?> PickImportPathAsync()
    {
        var picker = new FileOpenPicker(AppWindow.Id);
        foreach (var extension in Tomography.Business.Services.AssetService.Extensions) picker.FileTypeFilter.Add(extension);
        return (await picker.PickSingleFileAsync())?.Path;
    }
    private async Task<string?> PickExportPathAsync(string name, string extension)
    {
        var picker = new FileSavePicker(AppWindow.Id) { SuggestedFileName = System.IO.Path.GetFileNameWithoutExtension(name) };
        picker.FileTypeChoices.Add(extension, new List<string> { extension });
        return (await picker.PickSaveFileAsync())?.Path;
    }
}
