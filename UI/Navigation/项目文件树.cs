using System.IO;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;

namespace Tomography.UI.Navigation;

public sealed partial class ProjectDirectoryView
{
    private bool _projectOpened;
    private int _searchVersion;
    private readonly Dictionary<TreeViewNode, string> _folders = new();
    public void RefreshTree()
    {
        if (!_projectOpened) return;
        ProjectTree.RootNodes.Clear(); _folders.Clear(); TreeStatus.Text = "";
        var root = new TreeViewNode { Content = Row(DirectoryPath, true, 0), IsExpanded = true };
        _folders[root] = DirectoryPath;
        Populate(root);
        ProjectTree.RootNodes.Add(root);
    }
    private void FolderExpanding(TreeView sender, TreeViewExpandingEventArgs args)
    {
        if (args.Node.HasUnrealizedChildren) Populate(args.Node);
    }
    private void Populate(TreeViewNode node)
    {
        node.Children.Clear(); node.HasUnrealizedChildren = false;
        if (!_folders.TryGetValue(node, out var path)) return;
        try
        {
            foreach (var entry in new DirectoryInfo(path).EnumerateFileSystemInfos().OrderByDescending(e => e is DirectoryInfo).ThenBy(e => e.Name, StringComparer.OrdinalIgnoreCase))
            {
                var folder = entry is DirectoryInfo;
                var child = new TreeViewNode { Content = Row(entry.FullName, folder, Depth(node) + 1), HasUnrealizedChildren = folder && (entry.Attributes & FileAttributes.ReparsePoint) == 0 };
                if (folder) _folders[child] = entry.FullName;
                node.Children.Add(child);
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { TreeStatus.Text = "部分目录无法读取，请检查访问权限。"; }
    }
    private static int Depth(TreeViewNode node) => node.Parent is null ? 0 : Depth(node.Parent) + 1;
    private static FrameworkElement Row(string path, bool folder, int depth, string? label = null)
    {
        var row = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 10, Margin = new Thickness(depth * 12, 0, 0, 0) };
        row.Children.Add(new FontIcon { Glyph = folder ? "\uE8B7" : "\uE8A5", FontSize = 20, VerticalAlignment = VerticalAlignment.Center });
        row.Children.Add(new TextBlock { Text = label ?? Path.GetFileName(path), FontSize = 16, VerticalAlignment = VerticalAlignment.Center, TextTrimming = TextTrimming.CharacterEllipsis });
        Microsoft.UI.Xaml.Automation.AutomationProperties.SetName(row, label ?? Path.GetFileName(path));
        return row;
    }
    private void ProjectItemInvoked(TreeView sender, TreeViewItemInvokedEventArgs args)
    {
        if (args.InvokedItem is TreeViewNode node && _folders.ContainsKey(node)) node.IsExpanded = !node.IsExpanded;
    }
    private async void ProjectSearchChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
    {
        var version = ++_searchVersion;
        var query = sender.Text.Trim();
        if (!_projectOpened) return;
        if (query.Length == 0) { RefreshTree(); return; }
        var root = DirectoryPath;
        try
        {
            var paths = await Task.Run(() => Directory.EnumerateFileSystemEntries(root, "*", new EnumerationOptions { RecurseSubdirectories = true, IgnoreInaccessible = true, AttributesToSkip = FileAttributes.ReparsePoint })
                .Where(p => Path.GetFileName(p).Contains(query, StringComparison.OrdinalIgnoreCase)).Take(100).ToArray());
            if (version != _searchVersion || root != DirectoryPath) return;
            ProjectTree.RootNodes.Clear(); _folders.Clear(); TreeStatus.Text = "";
            foreach (var path in paths) ProjectTree.RootNodes.Add(new TreeViewNode { Content = Row(path, Directory.Exists(path), 0, Path.GetRelativePath(root, path)) });
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { if (version == _searchVersion) TreeStatus.Text = "搜索失败，请检查目录访问权限。"; }
    }

}
