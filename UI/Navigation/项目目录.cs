using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Controls;
using System.IO;
using Tomography.Business.Models;

namespace Tomography.UI.Navigation;

public sealed partial class ProjectDirectoryView : UserControl
{
    private TreeViewNode? _lastInvoked;
    private TreeView _tree => ProjectTree;
    private readonly Dictionary<TreeViewNode, string> _paths = new();
    private readonly Dictionary<string, TreeViewNode> _assets = new();
    private TextBlock _message => DirectoryMessage;
    private readonly TreeViewNode _inputs = new() { Content = "实验输入", IsExpanded = true };
    private readonly Dictionary<TreeViewNode, ImportedAsset> _importedAssets = new();
    public ImportedAsset? SelectedAsset => _tree.SelectedNode is TreeViewNode node && _importedAssets.TryGetValue(node, out var asset) ? asset : null;
    public string DirectoryPath { get; private set; } = FindDirectory();
    public event EventHandler? ChooseDirectoryRequested;
    public ProjectDirectoryView()
    {
        InitializeComponent();
        ChooseProjectDirectory.Click += (_, _) => ChooseDirectoryRequested?.Invoke(this, EventArgs.Empty);
        SearchBox.TextChanged += (_, _) => FilterProjectFiles();
        _tree.ItemInvoked += (_, e) =>
        {
            if (e.InvokedItem is not TreeViewNode node) return;
            var selected = node != _lastInvoked;
            _lastInvoked = selected ? node : null;
            node.IsExpanded = selected;
            // ItemClick can precede the native selection update.
            DispatcherQueue.TryEnqueue(() => _tree.SelectedNode = selected ? node : null);
        };
        _tree.Expanding += async (_, e) => { if (e.Node.HasUnrealizedChildren) await FillAsync(e.Node); };
        Loaded += async (_, _) => { if (_projectRoots.Length == 0) await LoadAsync(DirectoryPath); };
    }
    private void NodeContentLoaded(object sender, RoutedEventArgs e)
    {
        DependencyObject? parent = (DependencyObject)sender;
        while (parent is not null && parent is not TreeViewItem)
            parent = VisualTreeHelper.GetParent(parent);
        if (parent is TreeViewItem item && FindChevron(item) is FrameworkElement chevron)
            chevron.Visibility = Visibility.Collapsed;
    }
    private static FrameworkElement? FindChevron(DependencyObject parent)
    {
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            if (child is FrameworkElement { Name: "ExpandCollapseChevron" } chevron) return chevron;
            if (FindChevron(child) is { } nested) return nested;
        }
        return null;
    }
    public async Task LoadAsync(string path)
    {
        SearchBox.Text = "";
        _lastInvoked = null;
        DirectoryPath = Path.GetFullPath(path); _paths.Clear(); _tree.RootNodes.Clear();
        var root = Node(DirectoryPath, true); _tree.RootNodes.Add(root); _tree.RootNodes.Add(_inputs);
        await FillAsync(root);
        _projectRoots = _tree.RootNodes.ToArray();
        FilterProjectFiles();
    }
    private TreeViewNode Node(string path, bool directory)
    {
        var node = new TreeViewNode { Content = path, HasUnrealizedChildren = directory };
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
            _message.Text = "";
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { _message.Text = ex.Message; }
    }
    public void SetAsset(string role, ImportedAsset? asset)
    {
        if (_assets.Remove(role, out var old)) _inputs.Children.Remove(old);
        if (asset is null) { FilterProjectFiles(); return; }
        var node = Node(asset.Path, false); _importedAssets[node] = asset; _assets[role] = node; _inputs.Children.Add(node); FilterProjectFiles();
    }
    private static string FindDirectory()
    {
        foreach (var start in new[] { Environment.CurrentDirectory, AppContext.BaseDirectory })
            for (var folder = new DirectoryInfo(start); folder is not null; folder = folder.Parent)
                if (File.Exists(Path.Combine(folder.FullName, "Tomography.sln"))) return folder.FullName;
        return Environment.CurrentDirectory;
    }
}
