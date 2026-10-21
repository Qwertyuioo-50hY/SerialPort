using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Tomography.UI.Controls;
using Windows.System;
using Windows.UI.Core;

namespace Tomography.UI.SideView;

public sealed class ShortcutSettingsView : UserControl
{
    public event EventHandler<Dictionary<string, string>>? SaveRequested;
    private readonly Dictionary<string, TextBox> _fields = new();
    private readonly StackPanel _rows = new() { Spacing = 8 };
    private readonly TextBlock _status = new() { TextWrapping = TextWrapping.Wrap };
    private readonly TextBox _search = new() { PlaceholderText = "搜索快捷键" };
    private readonly StackPanel _panel = new() { Spacing = 20, Margin = new Thickness(12, 24, 12, 24) };
    private bool _english;
    public ShortcutSettingsView()
    {
        var header = new Grid(); header.ColumnDefinitions.Add(new() { Width = new GridLength(1, GridUnitType.Star) }); header.ColumnDefinitions.Add(new() { Width = GridLength.Auto });
        header.Children.Add(new TextBlock { Text = "键盘快捷键", FontSize = 26, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold });
        var reset = new Button { Content = "全部重置为默认值", CornerRadius = new(12) };
        AutomationProperties.SetAutomationId(reset, "ResetShortcuts"); Grid.SetColumn(reset, 1); header.Children.Add(reset);
        reset.Click += (_, _) => { foreach (var action in ApplicationShortcuts.Actions) _fields[action.Id].Text = action.Default; _status.Text = ""; };
        _panel.Children.Add(header); _panel.Children.Add(_search); _panel.Children.Add(_rows);
        AutomationProperties.SetAutomationId(_search, "ShortcutSearch");
        foreach (var action in ApplicationShortcuts.Actions)
        {
            var row = new Grid { ColumnSpacing = 12, Padding = new Thickness(16, 12, 16, 12), CornerRadius = new(16), Tag = action.Id };
            row.SetBinding(Grid.BackgroundProperty, new Microsoft.UI.Xaml.Data.Binding { Source = Application.Current.Resources["ControlFillColorSecondaryBrush"] });
            row.ColumnDefinitions.Add(new() { Width = new GridLength(1, GridUnitType.Star) });
            row.ColumnDefinitions.Add(new() { Width = new GridLength(170) }); row.ColumnDefinitions.Add(new() { Width = GridLength.Auto }); row.ColumnDefinitions.Add(new() { Width = GridLength.Auto });
            row.Children.Add(new TextBlock { Text = action.Chinese, FontSize = 15, VerticalAlignment = VerticalAlignment.Center });
            var field = new TextBox { IsReadOnly = true, PlaceholderText = "未设置", HorizontalAlignment = HorizontalAlignment.Stretch };
            AutomationProperties.SetAutomationId(field, "Shortcut_" + action.Id);
            field.KeyDown += CaptureKey; _fields[action.Id] = field; Grid.SetColumn(field, 1); row.Children.Add(field);
            var edit = new Button { Content = new FontIcon { Glyph = "\uE70F", FontSize = 16 }, Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Transparent), BorderThickness = new(0), CornerRadius = new(10) };
            AutomationProperties.SetName(edit, "编辑 " + action.Chinese); AutomationProperties.SetAutomationId(edit, "EditShortcut_" + action.Id);
            edit.Click += (_, _) => { field.PlaceholderText = _english ? "Press shortcut" : "按下快捷键"; field.Focus(FocusState.Programmatic); };
            Grid.SetColumn(edit, 2); row.Children.Add(edit);
            var remove = new Button { Content = new FontIcon { Glyph = "\uE74D", FontSize = 16 }, Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Transparent), BorderThickness = new(0), CornerRadius = new(10) };
            AutomationProperties.SetName(remove, "删除 " + action.Chinese); AutomationProperties.SetAutomationId(remove, "DeleteShortcut_" + action.Id);
            remove.Click += (_, _) => field.Text = ""; Grid.SetColumn(remove, 3); row.Children.Add(remove); _rows.Children.Add(row);
        }
        var save = new Button { Content = "保存", CornerRadius = new(12), HorizontalAlignment = HorizontalAlignment.Left };
        AutomationProperties.SetAutomationId(save, "SaveShortcuts"); AutomationProperties.SetAutomationId(_status, "ShortcutStatus");
        save.Click += (_, _) => Save(); _panel.Children.Add(save); _panel.Children.Add(_status);
        _search.TextChanged += (_, _) => Filter();
        Content = new ScrollViewer { Content = _panel, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled, VerticalScrollBarVisibility = ScrollBarVisibility.Hidden };
    }
    public void LoadBindings(IReadOnlyDictionary<string, string> values)
    { foreach (var pair in _fields) pair.Value.Text = values.GetValueOrDefault(pair.Key, ""); }
    public void SetLanguage(bool english)
    {
        _english = english;
        InterfaceLanguage.Apply(_panel, english);
        foreach (Grid row in _rows.Children)
        {
            var action = ApplicationShortcuts.Actions.First(a => a.Id == (string)row.Tag);
            ((TextBlock)row.Children[0]).Text = english ? action.English : action.Chinese;
            _fields[action.Id].PlaceholderText = english ? "Not set" : "未设置";
        }
        Filter();
    }
    private void Filter()
    {
        foreach (Grid row in _rows.Children) row.Visibility = (((TextBlock)row.Children[0]).Text + _fields[(string)row.Tag].Text).Contains(_search.Text, StringComparison.OrdinalIgnoreCase) ? Visibility.Visible : Visibility.Collapsed;
    }
    private void CaptureKey(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key is VirtualKey.Control or VirtualKey.Shift or VirtualKey.Menu or VirtualKey.LeftWindows or VirtualKey.RightWindows or VirtualKey.Tab) return;
        var modifiers = VirtualKeyModifiers.None;
        if (InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Control).HasFlag(CoreVirtualKeyStates.Down)) modifiers |= VirtualKeyModifiers.Control;
        if (InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Shift).HasFlag(CoreVirtualKeyStates.Down)) modifiers |= VirtualKeyModifiers.Shift;
        if (InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Menu).HasFlag(CoreVirtualKeyStates.Down)) modifiers |= VirtualKeyModifiers.Menu;
        var text = ApplicationShortcuts.Format(e.Key, modifiers);
        if (ApplicationShortcuts.TryParse(text, out _, out _)) ((TextBox)sender).Text = text;
        e.Handled = true;
    }
    private void Save()
    {
        var values = _fields.Where(p => p.Value.Text.Length > 0).ToDictionary(p => p.Key, p => p.Value.Text);
        var used = new HashSet<string>();
        foreach (var value in values.Values)
        {
            if (!ApplicationShortcuts.TryParse(value, out var key, out var modifiers) || !used.Add(ApplicationShortcuts.Format(key, modifiers)))
            { _status.Text = _english ? "Choose valid, unique shortcuts." : "快捷键无效或重复，请修改后保存。"; return; }
        }
        try { SaveRequested?.Invoke(this, values); _status.Text = _english ? "Saved" : "已保存"; }
        catch { _status.Text = _english ? "Unable to save shortcuts." : "快捷键保存失败，请重试。"; }
    }
}
