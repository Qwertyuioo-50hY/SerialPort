using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Tomography.Business.Models;

namespace Tomography.UI.Bottom;

public sealed partial class AiChatPanelView : UserControl
{
    public event EventHandler<string>? SendRequested;
    public AiChatPanelView()
    {
        InitializeComponent();
        var style = new Style(typeof(ListViewItem));
        style.Setters.Add(new Setter(Control.HorizontalContentAlignmentProperty, HorizontalAlignment.Stretch));
        Messages.ItemContainerStyle = style;
    }
    public void SetModel(string model) => ModelLabel.Text = string.IsNullOrWhiteSpace(model) ? "尚未配置模型" : model;
    public void SetBusy(bool busy) { SendButton.IsEnabled = !busy; MessageInput.IsEnabled = !busy; }
    public void SetStatus(string text) => ChatStatus.Text = text;
    public void ClearInput() => MessageInput.Text = "";
    public void ShowConversation(AiConversation? conversation)
    {
        ConversationTitle.Text = conversation?.Title ?? "新对话";
        Messages.Items.Clear();
        foreach (var message in conversation?.Messages ?? new List<AiMessage>())
        {
            var bubble = new Border { CornerRadius = new CornerRadius(20), Padding = new Thickness(16, 12, 16, 12),
                Margin = new Thickness(0, 4, 0, 4), MaxWidth = 760,
                HorizontalAlignment = message.Role == "user" ? HorizontalAlignment.Right : HorizontalAlignment.Left,
                Child = new TextBlock { Text = message.Content, TextWrapping = TextWrapping.Wrap, IsTextSelectionEnabled = true } };
            bubble.Style = (Style)Resources["ChatBubbleStyle"];
            Messages.Items.Add(bubble);
        }
        EmptyMessage.Visibility = Messages.Items.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        if (Messages.Items.Count > 0) Messages.ScrollIntoView(Messages.Items[Messages.Items.Count - 1]);
    }
    private void SendClicked(object sender, RoutedEventArgs e)
    {
        if (!string.IsNullOrWhiteSpace(MessageInput.Text)) SendRequested?.Invoke(this, MessageInput.Text.Trim());
    }
}
