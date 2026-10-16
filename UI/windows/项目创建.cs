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
        ShowDeviceError(message); AiChatView.SetStatus(message); return false;
    }
    private async Task SwitchProjectAsync(string path)
    {
        if (!CanChangeProject()) return;
        _saveDeviceTimer.Stop(); await SaveDeviceSettingsAsync();
        foreach (var role in _projectSettings.ExperimentInputs.Keys) DirectoryPanel.SetAsset(role, null);
        _reconstruction.ClearInputs(); _projectSettings = new();
        await DirectoryPanel.LoadAsync(path); await RestoreDeviceSettingsAsync();
    }
    private async Task CreateProjectAsync()
    {
        if (!CanChangeProject()) return;
        var nameField = new TextBox { Header = "项目名称", PlaceholderText = "输入项目名称" };
        Microsoft.UI.Xaml.Automation.AutomationProperties.SetAutomationId(nameField, "ProjectName");
        var dialog = new ContentDialog { XamlRoot = Root.XamlRoot, RequestedTheme = Root.ActualTheme,
            Title = "新建项目", Content = nameField, PrimaryButtonText = "选择保存位置", CloseButtonText = "取消" };
        if (await dialog.ShowAsync() != ContentDialogResult.Primary) return;
        var name = nameField.Text.Trim();
        if (name.Length == 0 || name is "." or ".." || name.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 || name.EndsWith('.'))
        { ShowProjectError("项目名称无效。"); return; }
        var parent = await new FolderPicker(AppWindow.Id).PickSingleFolderAsync();
        if (parent is null || !CanChangeProject()) return;
        try
        {
            var path = ProjectStore.Create(parent.Path, name);
            await SwitchProjectAsync(path);
        }
        catch (Exception ex) { ShowProjectError("创建项目失败：" + ex.Message); }
    }
    private void ShowProjectError(string text) { ShowDeviceError(text); AiChatView.SetStatus(text); }
}
