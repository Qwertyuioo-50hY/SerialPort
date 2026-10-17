using System.IO;
using Tomography.Business.Services;

namespace Tomography.UI.windows;

public sealed partial class AppWindow
{
    private void InitializeHome()
    {
        HomePanel.SetRecentProjects(_globalSettings.RecentProjects);
        HomePanel.BrowseProjectRequested += async (_, _) => await BrowseProjectAsync();
        HomePanel.NewProjectRequested += async (_, _) => await CreateProjectAsync();
        HomePanel.OpenProjectRequested += async (_, path) =>
        {
            if (!Directory.Exists(path)) { HomePanel.SetStatus("项目文件夹不存在或暂时不可访问。"); return; }
            try { await SwitchProjectAsync(path); }
            catch (Exception) { HomePanel.SetStatus("项目打开失败，请检查文件夹权限和配置。"); }
        };
    }
    private void RememberProject(string path)
    {
        RecentProjectStore.Remember(_globalSettings, path);
        HomePanel.SetRecentProjects(_globalSettings.RecentProjects);
        HomePanel.SetStatus("已打开项目：" + Path.GetFileName(path));
        try { SaveGlobalSettings(); }
        catch (Exception) { HomePanel.SetStatus("项目已打开，但最近项目列表保存失败。"); }
    }
}
