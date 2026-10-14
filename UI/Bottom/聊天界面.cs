using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Tomography.Business.Models;
using Windows.System;
using Windows.UI.Core;

namespace Tomography.UI.Bottom;

public sealed partial class AiChatPanelView : UserControl
{
    public event EventHandler<string>? SendRequested;
    public event EventHandler? NewConversationRequested;
    public event EventHandler<(string Model, string Effort)>? ConfigurationRequested;
    private AiApiSettings _configuration = new();
    private static readonly string[] Efforts = { "low", "medium", "high", "xhigh", "max" };
    private static readonly string[] EffortNames = { "轻度", "中等", "高", "极高", "最高" };
    private bool _updatingMode;
    private bool _busy;
    private bool _composing;
    private bool _commitPending;
    private TextBlock? _pendingSummary;
    private TextBlock? _pendingAnswer;
    private Expander? _pendingExpander;
    private Ellipse? _pendingDot;
    private readonly DispatcherTimer _pulse = new() { Interval = TimeSpan.FromMilliseconds(550) };
    public AiChatPanelView()
    {
        InitializeComponent();
        var style = new Style(typeof(ListViewItem));
        style.Setters.Add(new Setter(Control.HorizontalContentAlignmentProperty, HorizontalAlignment.Stretch));
        Messages.ItemContainerStyle = style;
        MessageInput.TextCompositionStarted += (_, _) => _composing = true;
        MessageInput.TextCompositionEnded += (_, _) =>
        {
            _composing = false; _commitPending = true;
            DispatcherQueue.TryEnqueue(() => _commitPending = false);
        };
        _pulse.Tick += (_, _) => { if (_pendingDot is not null) _pendingDot.Opacity = _pendingDot.Opacity < 1 ? 1 : 0.25; };
        Unloaded += (_, _) => _pulse.Stop();
        Loaded += (_, _) => { if (_busy && _pendingDot is not null) _pulse.Start(); };
        UpdateSendState();
    }
    public void SetConfiguration(AiApiSettings settings)
    {
        _configuration = settings;
        var index = Array.IndexOf(Efforts, settings.ReasoningEffort);
        ModelLabel.Text = index < 0 ? "默认" : EffortNames[index];
        UpdateModeControls();
    }
    private void UpdateModeControls()
    {
        _updatingMode = true;
        try
        {
            var index = Array.IndexOf(Efforts, _configuration.ReasoningEffort);
            EffortSlider.Value = index < 0 ? 1 : index;
            EffortHeading.Text = index < 0 ? "默认" : EffortNames[index];
            UpdateEffortDots(index < 0 ? 1 : index);
        }
        finally { _updatingMode = false; }
    }
    private void ModeFlyoutOpened(object? sender, object e) => UpdateModeControls();
    private void UpdateEffortDots(int selected)
    {
        if (EffortDot4 is null) return;
        var dots = new[] { EffortDot0, EffortDot1, EffortDot2, EffortDot3, EffortDot4 };
        for (var i = 0; i < dots.Length; i++) dots[i].Opacity = i == selected ? 0 : 0.4;
    }
    private void EffortValueChanged(object sender, Microsoft.UI.Xaml.Controls.Primitives.RangeBaseValueChangedEventArgs e)
    {
        if (_updatingMode || EffortHeading is null) return;
        var index = Math.Clamp((int)Math.Round(e.NewValue), 0, 4);
        EffortHeading.Text = EffortNames[index];
        UpdateEffortDots(index);
        ConfigurationRequested?.Invoke(this, (_configuration.Model, Efforts[index]));
    }
    private void ResetEffortClicked(object sender, RoutedEventArgs e) => ConfigurationRequested?.Invoke(this, (_configuration.Model, "default"));
    private void NewConversationClicked(object sender, RoutedEventArgs e) => NewConversationRequested?.Invoke(this, EventArgs.Empty);
    public void SetBusy(bool busy)
    {
        _busy = busy; ModelButton.IsEnabled = !busy; UpdateSendState();
        if (!busy) { _pulse.Stop(); if (_pendingDot is not null) _pendingDot.Visibility = Visibility.Collapsed; }
    }
    public void SetStatus(string text) { ChatStatus.Text = text; ChatStatus.Visibility = string.IsNullOrEmpty(text) ? Visibility.Collapsed : Visibility.Visible; }
    public void ClearInput() => MessageInput.Text = "";
    public void RestoreInput(string text) { if (string.IsNullOrWhiteSpace(MessageInput.Text)) MessageInput.Text = text; }
    private void UpdateSendState() { if (SendButton is not null) SendButton.IsEnabled = !_busy && !string.IsNullOrWhiteSpace(MessageInput.Text); }
    private void MessageInputChanged(object sender, TextChangedEventArgs e) => UpdateSendState();
    private void MessageInputKeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key != VirtualKey.Enter || _composing || _commitPending) return;
        var shift = (InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Shift) & CoreVirtualKeyStates.Down) != 0;
        if (shift) return;
        e.Handled = true;
        SendMessage();
    }
    private static TextBlock Text(string text) => new() { Text = text, TextWrapping = TextWrapping.Wrap, IsTextSelectionEnabled = true };
    private static Expander Summary(string text) => new() { Header = "思考摘要", Content = Text(text), IsExpanded = true, HorizontalAlignment = HorizontalAlignment.Stretch, HorizontalContentAlignment = HorizontalAlignment.Stretch };
    public void ShowConversation(AiConversation? conversation)
    {
        _pulse.Stop(); _pendingDot = null; _pendingSummary = null; _pendingAnswer = null; _pendingExpander = null;
        ConversationTitle.Text = conversation?.Title ?? "新对话";
        Messages.Items.Clear();
        foreach (var message in conversation?.Messages ?? new List<AiMessage>())
        {
            if (message.Role == "user")
            {
                var bubble = new Border { CornerRadius = new CornerRadius(20), Padding = new Thickness(16, 12, 16, 12), Margin = new Thickness(0, 4, 0, 4), MaxWidth = 760, HorizontalAlignment = HorizontalAlignment.Right, Child = Text(message.Content) };
                bubble.Style = (Style)Resources["ChatBubbleStyle"]; Messages.Items.Add(bubble);
            }
            else
            {
                var panel = new StackPanel { Spacing = 12, Margin = new Thickness(0, 12, 0, 16), HorizontalAlignment = HorizontalAlignment.Stretch };
                if (!string.IsNullOrWhiteSpace(message.ReasoningSummary)) panel.Children.Add(Summary(message.ReasoningSummary));
                panel.Children.Add(Text(message.Content)); Messages.Items.Add(panel);
            }
        }
        EmptyMessage.Visibility = Messages.Items.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        ScrollToEnd();
    }
    public void ShowPending(string summary, string answer)
    {
        if (_pendingAnswer is null)
        {
            var panel = new StackPanel { Spacing = 12, Margin = new Thickness(0, 16, 0, 16), HorizontalAlignment = HorizontalAlignment.Stretch };
            _pendingDot = new Ellipse { Width = 10, Height = 10, HorizontalAlignment = HorizontalAlignment.Left, Fill = MessageInput.Foreground };
            _pendingExpander = Summary(""); _pendingSummary = (TextBlock)_pendingExpander.Content;
            _pendingAnswer = Text("");
            panel.Children.Add(_pendingDot); panel.Children.Add(_pendingExpander); panel.Children.Add(_pendingAnswer); Messages.Items.Add(panel);
        }
        _pendingSummary!.Text = summary; _pendingAnswer.Text = answer;
        _pendingExpander!.Visibility = string.IsNullOrEmpty(summary) ? Visibility.Collapsed : Visibility.Visible;
        _pendingAnswer.Visibility = string.IsNullOrEmpty(answer) ? Visibility.Collapsed : Visibility.Visible;
        EmptyMessage.Visibility = Visibility.Collapsed;
        if (_busy) _pulse.Start(); ScrollToEnd();
    }
    private void ScrollToEnd() { if (Messages.Items.Count > 0) Messages.ScrollIntoView(Messages.Items[Messages.Items.Count - 1]); }
    private void SendMessage() { if (!_busy && !_composing && !string.IsNullOrWhiteSpace(MessageInput.Text)) SendRequested?.Invoke(this, MessageInput.Text.Trim()); }
    private void SendClicked(object sender, RoutedEventArgs e) => SendMessage();
}
