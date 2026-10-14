using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Tomography.Business.Models;

namespace Tomography.UI.Navigation;

public sealed partial class ProjectDirectoryView
{
    private bool _updatingChats;
    public event EventHandler? NewChatRequested;
    public event EventHandler<AiConversation>? ChatRequested;
    public event EventHandler? ProjectChanged;
    public void SetConversations(IEnumerable<AiConversation> conversations, AiConversation? selected)
    {
        _updatingChats = true;
        try
        {
            ChatConversations.Items.Clear();
            foreach (var conversation in conversations)
            {
                var item = new ListViewItem { Content = conversation.Title, Tag = conversation };
                ChatConversations.Items.Add(item);
                if (conversation == selected) ChatConversations.SelectedItem = item;
            }
        }
        finally { _updatingChats = false; }
    }
    private void NewChatClicked(object sender, RoutedEventArgs e) => NewChatRequested?.Invoke(this, EventArgs.Empty);
    private void ChatSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!_updatingChats && ChatConversations.SelectedItem is ListViewItem { Tag: AiConversation conversation })
            ChatRequested?.Invoke(this, conversation);
    }
}
