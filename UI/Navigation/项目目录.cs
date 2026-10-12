using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using System.IO;
using Tomography.Business.Models;

namespace Tomography.UI.Navigation;

public sealed class ProjectDirectoryView : UserControl
{
    private readonly TreeView _tree = new() { SelectionMode = TreeViewSelectionMode.Single, CanDragItems = false, AllowDrop = false };
    private readonly Dictionary<TreeViewNode, string> _paths = new();
    private readonly Dictionary<string, TreeViewNode> _assets = new();
    private readonly TextBlock _message = new() { TextWrapping = TextWrapping.Wrap };
    private readonly TreeViewNode _inputs = new() { Content = "实验输入", IsExpanded = true };
    public string DirectoryPath { get; private set; } = FindDirectory();
    public event EventHandler? ChooseDirectoryRequested;
    public ProjectDirectoryView()
    {
        AutomationProperties.SetAutomationId(_tree, "ProjectDirectory");
        AutomationProperties.SetName(_tree, "当前项目目录");
        var choose = new Button { Content = "打开文件夹", HorizontalAlignment = HorizontalAlignment.Stretch };
        AutomationProperties.SetAutomationId(choose, "ChooseProjectDirectory");
        choose.Click += (_, _) => ChooseDirectoryRequested?.Invoke(this, EventArgs.Empty);
        var grid = new Grid { Padding = new Thickness(8), RowSpacing = 8 };
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition());
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        Grid.SetRow(_tree, 1); Grid.SetRow(_message, 2);
        grid.Children.Add(choose); grid.Children.Add(_tree); grid.Children.Add(_message); Content = grid;
        _tree.Expanding += async (_, e) => { if (e.Node.HasUnrealizedChildren) await FillAsync(e.Node); };
        Loaded += async (_, _) => { if (_tree.RootNodes.Count == 0) await LoadAsync(DirectoryPath); };
    }
    public async Task LoadAsync(string path)
    {
        DirectoryPath = Path.GetFullPath(path); _paths.Clear(); _tree.RootNodes.Clear();
        var root = Node(DirectoryPath, true); _tree.RootNodes.Add(root); _tree.RootNodes.Add(_inputs);
        await FillAsync(root);
    }
    private TreeViewNode Node(string path, bool directory)
    {
        var content = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        content.Children.Add(new FontIcon { Glyph = directory ? "\uE8B7" : "\uE8A5", FontSize = 16 });
        content.Children.Add(new TextBlock { Text = Path.GetFileName(path), VerticalAlignment = VerticalAlignment.Center });
        var node = new TreeViewNode { Content = content, HasUnrealizedChildren = directory };
        _paths[node] = path; return node;
    }
    private async Task FillAsync(TreeViewNode node)
    {
        node.HasUnrealizedChildren = false;
        try
        {
            var entries = await Task.Run(() => Directory.EnumerateFileSystemEntries(_paths[node])
                .Where(path => Path.GetFileName(path) != ".git")
                .OrderByDescending(Directory.Exists).ThenBy(path => path, StringComparer.OrdinalIgnoreCase).ToArray());
            foreach (var path in entries) node.Children.Add(Node(path, Directory.Exists(path)));
            node.IsExpanded = true; _message.Text = "";
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { _message.Text = ex.Message; }
    }
    public void SetAsset(string role, ImportedAsset? asset)
    {
        if (_assets.Remove(role, out var old)) _inputs.Children.Remove(old);
        if (asset is null) return;
        var node = Node(asset.Path, false); _assets[role] = node; _inputs.Children.Add(node);
    }
    private static string FindDirectory()
    {
        foreach (var start in new[] { Environment.CurrentDirectory, AppContext.BaseDirectory })
            for (var folder = new DirectoryInfo(start); folder is not null; folder = folder.Parent)
                if (File.Exists(Path.Combine(folder.FullName, "Tomography.sln"))) return folder.FullName;
        return Environment.CurrentDirectory;
    }
}
