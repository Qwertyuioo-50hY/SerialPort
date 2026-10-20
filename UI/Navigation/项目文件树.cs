using System.IO;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Tomography.UI.Navigation;

public sealed partial class ProjectDirectoryView
{
    private bool _projectOpened;
    private int _searchVersion, _treeVersion;
    private ProjectEntry? _root;
    private readonly System.Collections.ObjectModel.ObservableCollection<ProjectEntry> _visible = new();
    private readonly HashSet<ProjectEntry> _loading = new();
    public async void RefreshTree()
    {
        if (!_projectOpened) return;
        var version = ++_treeVersion;
        var path = DirectoryPath;
        TreeStatus.Text = "";
        try
        {
            var root = new ProjectEntry(path, true, false, 0) { Expanded = true };
            root.Children = await ReadChildrenAsync(root);
            if (version != _treeVersion || path != DirectoryPath) return;
            _root = root; RenderTree();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { TreeStatus.Text = "部分目录无法读取，请检查访问权限。"; }
    }
    private static Task<List<ProjectEntry>> ReadChildrenAsync(ProjectEntry folder) => Task.Run(() =>
        new DirectoryInfo(folder.Path).EnumerateFileSystemInfos()
            .Select(e => new ProjectEntry(e.FullName, e is DirectoryInfo, (e.Attributes & FileAttributes.ReparsePoint) != 0, folder.Depth + 1))
            .OrderByDescending(e => e.Folder).ThenBy(e => e.Name, StringComparer.OrdinalIgnoreCase).ToList());
    private void RenderTree()
    {
        if (_root is null) return;
        var visible = new List<ProjectEntry>();
        void Append(ProjectEntry node)
        {
            visible.Add(node);
            if (node.Expanded && node.Children is not null) foreach (var child in node.Children) Append(child);
        }
        Append(_root); UpdateVisible(visible);
    }
    private void UpdateVisible(IReadOnlyList<ProjectEntry> entries)
    {
        if (!ReferenceEquals(ProjectTree.ItemsSource, _visible)) ProjectTree.ItemsSource = _visible;
        for (var i = 0; i < entries.Count; i++)
        {
            if (i < _visible.Count && ReferenceEquals(_visible[i], entries[i])) continue;
            var existing = _visible.IndexOf(entries[i]);
            if (existing >= 0) _visible.Move(existing, i); else _visible.Insert(i, entries[i]);
        }
        while (_visible.Count > entries.Count) _visible.RemoveAt(_visible.Count - 1);
    }
    private async void ProjectItemClicked(object sender, ItemClickEventArgs args)
    {
        if (args.ClickedItem is not ProjectEntry entry || !entry.Folder || entry.Link || ProjectSearch.Text.Trim().Length > 0 || !_loading.Add(entry)) return;
        var version = _treeVersion;
        try
        {
            if (entry.Children is null) entry.Children = await ReadChildrenAsync(entry);
            if (version != _treeVersion) return;
            entry.Expanded = !entry.Expanded; RenderTree();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { TreeStatus.Text = "目录无法读取，请检查访问权限。"; }
        finally { _loading.Remove(entry); }
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
                .Where(p => System.IO.Path.GetFileName(p).Contains(query, StringComparison.OrdinalIgnoreCase)).Take(100).ToArray());
            if (version != _searchVersion || root != DirectoryPath) return;
            TreeStatus.Text = "";
            UpdateVisible(paths.Select(p => new ProjectEntry(p, Directory.Exists(p), false, 0, System.IO.Path.GetRelativePath(root, p))).ToArray());
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { if (version == _searchVersion) TreeStatus.Text = "搜索失败，请检查目录访问权限。"; }
    }
}

public sealed class ProjectEntry(string path, bool folder, bool link, int depth, string? label = null)
{
    public string Path { get; } = path;
    public string Name => label ?? System.IO.Path.GetFileName(Path.TrimEnd(System.IO.Path.DirectorySeparatorChar));
    public string Glyph => Folder ? "\uE8B7" : "\uE8A5";
    public Thickness Indent => new(Depth * 12, 0, 0, 0);
    public bool Folder { get; } = folder;
    public bool Link { get; } = link;
    public int Depth { get; } = depth;
    public bool Expanded { get; set; }
    public List<ProjectEntry>? Children { get; set; }
    public override string ToString() => Name;
}
