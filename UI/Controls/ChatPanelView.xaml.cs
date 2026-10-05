using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
namespace Tomography.UI.Controls;
public sealed partial class ChatPanelView : UserControl
{
    public ChatPanelView() => InitializeComponent();
    private void CloseChat(object sender, RoutedEventArgs e) => Visibility = Visibility.Collapsed;
}
