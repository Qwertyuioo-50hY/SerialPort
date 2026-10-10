using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace Tomography.UI.SideView;

public sealed partial class IconRailView : UserControl
{
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
        foreach (Button button in RailItems.Children)
        {
            var selected = ReferenceEquals(button, sender);
            button.Background = selected ? _selectedBackground : new SolidColorBrush(Microsoft.UI.Colors.Transparent);
            var color = selected ? _selectedForeground : _defaultForeground;
            button.Foreground = ((FontIcon)button.Content).Foreground = color;
        }
    }
}
