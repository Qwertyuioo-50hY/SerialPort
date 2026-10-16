using Microsoft.UI.Xaml;

namespace Tomography.UI.Navigation;

public sealed partial class ProjectDirectoryView
{
    public event EventHandler? NewChatRequested;
    public event EventHandler? CreateProjectRequested;
    private void NewChatClicked(object sender, RoutedEventArgs e) => NewChatRequested?.Invoke(this, EventArgs.Empty);
    private void CreateProjectClicked(object sender, RoutedEventArgs e) => CreateProjectRequested?.Invoke(this, EventArgs.Empty);
}
