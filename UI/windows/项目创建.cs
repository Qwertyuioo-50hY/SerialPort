using System.IO;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Windows.Storage.Pickers;
using Tomography.Business.Services;

namespace Tomography.UI.windows;

public sealed partial class AppWindow
{
    private bool CanChangeProject()
    {
        if (!_aiBusy && !_connecting && !ActiveChannel.IsOpen && !_sampling && !_fileOperation && !_reconstructing) return true;
        const string message = "请先结束当前操作并断开设备，再切换项目。";
        ShowDeviceError(message); AiChatView.SetStatus(message); HomePanel.SetStatus(message); return false;
    }
    private async Task SwitchProjectAsync(string path)
    {
        if (!CanChangeProject()) return;
        _saveDeviceTimer.Stop(); await SaveDeviceSettingsAsync();
        foreach (var role in _projectSettings.ExperimentInputs.Keys) DirectoryPanel.SetAsset(role, null);
        _reconstruction.ClearInputs(); _projectSettings = new();
        await DirectoryPanel.LoadAsync(path); await RestoreDeviceSettingsAsync();
        DirectoryPanel.RefreshTree();
        ShowProjectNavigation();
        RememberProject(path);
    }
    private async Task CreateProjectAsync()
    {
        if (!CanChangeProject()) return;
        var entered = await Tomography.UI.Controls.RoundedProjectPrompt.ShowAsync(Root, _globalSettings.Language == "en");
        if (entered is null) return;
        var name = entered.Trim();
        if (name.Length == 0 || name is "." or ".." || name.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 || name.EndsWith('.'))
        { ShowProjectError("项目名称无效。"); return; }
        var parent = await new FolderPicker(PickerWindowId).PickSingleFolderAsync();
        if (parent is null || !CanChangeProject()) return;
        try
        {
            var path = ProjectStore.Create(parent.Path, name);
            await SwitchProjectAsync(path);
        }
        catch (Exception ex) { ShowProjectError("创建项目失败：" + ex.Message); }
    }
    private void ShowProjectError(string text) { ShowDeviceError(text); AiChatView.SetStatus(text); HomePanel.SetStatus(text); }
}
