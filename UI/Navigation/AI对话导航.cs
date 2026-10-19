using Microsoft.UI.Xaml;

namespace Tomography.UI.Navigation;

public sealed partial class ProjectDirectoryView
{
    public event EventHandler? CreateProjectRequested;
    private void CreateProjectClicked(object sender, RoutedEventArgs e) => CreateProjectRequested?.Invoke(this, EventArgs.Empty);
}
