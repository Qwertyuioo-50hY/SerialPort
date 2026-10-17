using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace Tomography.UI.SideView;

public sealed partial class IconRailView : UserControl
{
    public event EventHandler<bool>? AccountActionRequested;
    public void SetAccount(string username)
    {
        AccountPicture.DisplayName = username;
        ToolTipService.SetToolTip(AccountButton, username);
    }
    private void AccountMenuClicked(object sender, RoutedEventArgs e) =>
        AccountActionRequested?.Invoke(this, ((MenuFlyoutItem)sender).Tag as string == "Switch");
    public event EventHandler<string>? NavigationRequested;
    public event EventHandler? DirectoryToggleRequested;
    private void CollapseDirectory(object sender, RoutedEventArgs e) => DirectoryToggleRequested?.Invoke(this, EventArgs.Empty);
    private readonly Brush _selectedBackground;
    private readonly Brush _selectedForeground;
    private readonly Brush _defaultForeground;
    public IconRailView()
    {
        InitializeComponent();
        _selectedBackground = SelectedRailItem.Background;
        _selectedForeground = SelectedRailItem.Foreground;
        _defaultForeground = ((Button)RailItems.Children[0]).Foreground;
    }
    private void SelectRailItem(object sender, RoutedEventArgs e)
    {
        if (((Button)sender).Tag is not string section) return;
        NavigationRequested?.Invoke(this, section);
        SetSection(section);
    }
    public void SetSection(string section)
    {
        foreach (Button button in RailItems.Children.OfType<Button>().Append(AiRailItem).Append(SettingsRailItem))
        {
            var selected = button.Tag as string == section;
            button.Background = selected ? _selectedBackground : new SolidColorBrush(Microsoft.UI.Colors.Transparent);
            var color = selected ? _selectedForeground : _defaultForeground;
            button.Foreground = color;
            if (button.Content is FontIcon font) font.Foreground = color;
            else if (button.Content is Microsoft.UI.Xaml.Shapes.Path path) path.Fill = color;
        }
    }
}
