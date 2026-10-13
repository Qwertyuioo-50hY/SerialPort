using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Tomography.UI.Navigation;

public sealed partial class ProjectDirectoryView
{
    private TreeViewNode[] _projectRoots = Array.Empty<TreeViewNode>();

    private void FilterProjectFiles()
    {
        if (_projectRoots.Length == 0) return;
        var query = SearchBox.Text.Trim();
        _tree.RootNodes.Clear();
        foreach (var root in _projectRoots)
        {
            var match = query.Length == 0 ? root : FilterNode(root, query);
            if (match is not null) _tree.RootNodes.Add(match);
        }
        EmptyProjectMessage.Visibility = _tree.RootNodes.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    private TreeViewNode? FilterNode(TreeViewNode source, string query)
    {
        var children = source.Children.Select(child => FilterNode(child, query)).OfType<TreeViewNode>().ToArray();
        if (children.Length == 0 && !(source.Content?.ToString()?.Contains(query, StringComparison.OrdinalIgnoreCase) ?? false)) return null;
        var node = new TreeViewNode { Content = source.Content, IsExpanded = true };
        foreach (var child in children) node.Children.Add(child);
        return node;
    }
}
