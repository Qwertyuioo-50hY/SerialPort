using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Tomography.Business.Models;

namespace Tomography.UI.Bottom;

public sealed partial class AiChatPanelView : UserControl
{
    public event EventHandler<string>? SendRequested;
    public event EventHandler? NewConversationRequested;
    public event EventHandler? SettingsRequested;
    public event EventHandler<(string Model, string Effort)>? ConfigurationRequested;
    private AiApiSettings _configuration = new();
    public AiChatPanelView()
    {
        InitializeComponent();
        var style = new Style(typeof(ListViewItem));
        style.Setters.Add(new Setter(Control.HorizontalContentAlignmentProperty, HorizontalAlignment.Stretch));
        Messages.ItemContainerStyle = style;
    }
    public void SetConfiguration(AiApiSettings settings)
    {
        _configuration = settings;
        var effort = settings.ReasoningEffort switch { "low" => "轻度", "medium" => "中等", "high" => "高", "xhigh" => "极高", "max" => "最高", _ => "默认" };
        ModelLabel.Text = string.IsNullOrWhiteSpace(settings.Model) ? "选择模型" : $"{settings.Model} · {effort}";
    }
    private void NewConversationClicked(object sender, RoutedEventArgs e) => NewConversationRequested?.Invoke(this, EventArgs.Empty);
    private void OpenSettingsClicked(object sender, RoutedEventArgs e)
    {
        var menu = new MenuFlyout();
        foreach (var model in (_configuration.Models ?? new()).Append(_configuration.Model).Where(model => !string.IsNullOrWhiteSpace(model)).Distinct())
        {
            var item = new ToggleMenuFlyoutItem { Text = model, IsChecked = model == _configuration.Model };
            item.Click += (_, _) => ConfigurationRequested?.Invoke(this, (model, _configuration.ReasoningEffort));
            menu.Items.Add(item);
        }
        var effortMenu = new MenuFlyoutSubItem { Text = "思考强度" };
        foreach (var effort in new[] { "default", "low", "medium", "high", "xhigh", "max" })
        {
            var item = new ToggleMenuFlyoutItem { Text = effort, IsChecked = effort == _configuration.ReasoningEffort };
            item.Click += (_, _) => ConfigurationRequested?.Invoke(this, (_configuration.Model, effort));
            effortMenu.Items.Add(item);
        }
        menu.Items.Add(effortMenu);
        menu.Items.Add(new MenuFlyoutSeparator());
        var settings = new MenuFlyoutItem { Text = "更多设置" };
        settings.Click += (_, _) => SettingsRequested?.Invoke(this, EventArgs.Empty);
        menu.Items.Add(settings); menu.ShowAt((FrameworkElement)sender);
    }
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
