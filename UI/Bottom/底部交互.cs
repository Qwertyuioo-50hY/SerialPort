using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Tomography.UI.windows;

public sealed partial class AppWindow
{
    private void BottomTabChanged(SelectorBar sender, SelectorBarSelectionChangedEventArgs args)
    {
        if (BottomPanel is not null) ShowBottomPanel(sender.SelectedItem == AITab);
    }

    private void ShowBottomPanel(bool ai)
    {
        BottomTabs.SelectedItem = ai ? AITab : CommandTab;
        CommandPanel.Visibility = ai ? Visibility.Collapsed : Visibility.Visible;
        ChatPanel.Visibility = ai ? Visibility.Visible : Visibility.Collapsed;
        BottomPanel.Visibility = Visibility.Visible;
        CommandRow.Height = new GridLength(220);
    }

    private void ToggleBottomPanel(object sender, RoutedEventArgs e)
    {
        if (BottomPanel.Visibility == Visibility.Collapsed) ShowBottomPanel(BottomTabs.SelectedItem == AITab);
        else
        {
            BottomPanel.Visibility = Visibility.Collapsed;
            CommandRow.Height = new GridLength(0);
        }
    }

}
