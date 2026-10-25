using Microsoft.UI.Xaml;
using Tomography.Business.Services;

namespace Tomography.UI.windows;

public sealed partial class AppWindow
{
    private Task _rawDataSaveTail = Task.CompletedTask;
    private Task QueueRawDataSave(bool reference, double[] values)
    {
        var project = DirectoryPanel.DirectoryPath;
        var frame = values.ToArray();
        var previous = _rawDataSaveTail;
        var dispatcher = DispatcherQueue;
        _rawDataSaveTail = Task.Run(async () =>
        {
            await previous.ConfigureAwait(false);
            string status;
            try { AcquisitionRawStore.Save(project, reference, frame); status = "已保存至 data · 原始单位 µV"; }
            catch (Exception ex) { status = "数据保存失败：" + ex.Message; }
            dispatcher.TryEnqueue(() =>
            {
                if (_loaded && DirectoryPanel.DirectoryPath == project) DataSaveStatus.Text = status;
            });
        });
        return _rawDataSaveTail;
    }
    private async void SaveDataClicked(object sender, RoutedEventArgs e)
    {
        if (_reconstruction.Reference is null && _reconstruction.Measurements is null) { DataSaveStatus.Text = "尚无可保存的采集数据"; return; }
        Task saved = Task.CompletedTask;
        if (_reconstruction.Reference is { } reference) saved = QueueRawDataSave(true, reference);
        if (_reconstruction.Measurements is { } measurements) saved = QueueRawDataSave(false, measurements);
        await saved;
    }
}
