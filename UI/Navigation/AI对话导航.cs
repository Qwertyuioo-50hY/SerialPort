using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Tomography.Business.Models;

namespace Tomography.UI.Navigation;

public sealed partial class ProjectDirectoryView
{
    private readonly TreeViewNode _chats = new() { Content = "聊天", IsExpanded = true };
    private readonly Dictionary<TreeViewNode, AiConversation> _chatNodes = new();
    public event EventHandler? NewChatRequested;
    public event EventHandler? CreateProjectRequested;
    public event EventHandler<AiConversation>? ChatRequested;
    public event EventHandler? ProjectChanged;
    public void SetConversations(IEnumerable<AiConversation> conversations, AiConversation? selected)
    {
        _chatNodes.Clear(); _chats.Children.Clear();
        TreeViewNode? selection = null;
        foreach (var conversation in conversations)
        {
            var node = new TreeViewNode { Content = conversation };
            _chats.Children.Add(node); _chatNodes[node] = conversation;
            if (conversation == selected) selection = node;
        }
        FilterProjectFiles();
        if (selection is not null && string.IsNullOrWhiteSpace(SearchBox.Text))
        { _chats.IsExpanded = true; _tree.SelectedNode = selection; }
    }
    private void NewChatClicked(object sender, RoutedEventArgs e) => NewChatRequested?.Invoke(this, EventArgs.Empty);
    private void CreateProjectClicked(object sender, RoutedEventArgs e) => CreateProjectRequested?.Invoke(this, EventArgs.Empty);
}
