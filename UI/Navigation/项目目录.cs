using Microsoft.UI.Xaml.Controls;
using System.IO;
using Tomography.Business.Models;

namespace Tomography.UI.Navigation;

public sealed partial class ProjectDirectoryView : UserControl
{
    public ImportedAsset? SelectedAsset => null;
    public string DirectoryPath { get; private set; } = FindDirectory();
    public event EventHandler? ChooseDirectoryRequested;
    public event EventHandler? ProjectChanged;
    public ProjectDirectoryView()
    {
        InitializeComponent();
        Loaded += (_, _) => { if (ProjectTree.Items.Count == 0) RefreshTree(); };
        ChooseProjectDirectory.Click += (_, _) => ChooseDirectoryRequested?.Invoke(this, EventArgs.Empty);
    }
    public Task LoadAsync(string path)
    {
        if (!Directory.Exists(path)) throw new DirectoryNotFoundException("项目文件夹不存在。");
        DirectoryPath = Path.GetFullPath(path);
        _projectOpened = true;
        ProjectActions.Visibility = Microsoft.UI.Xaml.Visibility.Collapsed;
        ProjectSearch.Visibility = ProjectTree.Visibility = Microsoft.UI.Xaml.Visibility.Visible;
        ProjectSearch.Text = "";
        ProjectChanged?.Invoke(this, EventArgs.Empty);
        return Task.CompletedTask;
    }
    public void SetAsset(string role, ImportedAsset? asset) { }
    private static string FindDirectory()
    {
        foreach (var start in new[] { Environment.CurrentDirectory, AppContext.BaseDirectory })
            for (var folder = new DirectoryInfo(start); folder is not null; folder = folder.Parent)
                if (File.Exists(Path.Combine(folder.FullName, "Tomography.sln"))) return folder.FullName;
        var workspace = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Tomography", "Workspace");
        Directory.CreateDirectory(workspace);
        return workspace;
    }
}
