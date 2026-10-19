using System.IO;
using Tomography.Business.Services;

namespace Tomography.UI.windows;

public sealed partial class AppWindow
{
    private void InitializeHome()
    {
        HomePanel.SetRecentProjects(_globalSettings.RecentProjects);
        RefreshRecentChats();
        HomePanel.OpenChatRequested += async (_, chat) =>
        {
            if (!CanChangeProject()) return;
            try
            {
                if (!string.Equals(DirectoryPanel.DirectoryPath, chat.Project, StringComparison.OrdinalIgnoreCase)) await SwitchProjectAsync(chat.Project);
                _conversation = _conversations.FirstOrDefault(c => c.Id == chat.Id);
                if (_conversation is null) { HomePanel.SetStatus("聊天记录不存在或已被移动。"); return; }
                ShowAiConversation();
            }
            catch (Exception) { HomePanel.SetStatus("聊天打开失败，请检查项目和对话文件。"); }
        };
        HomePanel.BrowseProjectRequested += async (_, _) => await BrowseProjectAsync();
        HomePanel.NewProjectRequested += async (_, _) => await CreateProjectAsync();
        HomePanel.OpenProjectRequested += async (_, path) =>
        {
            if (!Directory.Exists(path)) { HomePanel.SetStatus("项目文件夹不存在或暂时不可访问。"); return; }
            try { await SwitchProjectAsync(path); }
            catch (Exception) { HomePanel.SetStatus("项目打开失败，请检查文件夹权限和配置。"); }
        };
    }
    private int _chatIndexVersion;
    private async void RefreshRecentChats()
    {
        var version = ++_chatIndexVersion;
        var paths = _globalSettings.RecentProjects.Prepend(DirectoryPanel.DirectoryPath).ToArray();
        var chats = await Task.Run(() => RecentChatStore.Read(paths));
        if (!_aiClosed && version == _chatIndexVersion) HomePanel.SetRecentChats(chats);
    }
    private void RememberProject(string path)
    {
        RecentProjectStore.Remember(_globalSettings, path);
        RefreshRecentChats();
        HomePanel.SetRecentProjects(_globalSettings.RecentProjects);
        HomePanel.SetStatus("已打开项目：" + Path.GetFileName(path));
        try { SaveGlobalSettings(); }
        catch (Exception) { HomePanel.SetStatus("项目已打开，但最近项目列表保存失败。"); }
    }
}
