using System.IO;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Tomography.UI.Home;

public sealed partial class HomeView : UserControl
{
    public event EventHandler? NewProjectRequested;
    public event EventHandler<string>? OpenProjectRequested;
    public HomeView() => InitializeComponent();
    public void SetRecentProjects(IEnumerable<string>? paths)
    {
        var projects = (paths ?? Array.Empty<string>()).Select(path => new RecentProjectItem(path)).ToArray();
        RecentProjects.ItemsSource = projects;
        EmptyRecentProjects.Visibility = projects.Length == 0 ? Visibility.Visible : Visibility.Collapsed;
    }
    public void SetStatus(string message) => HomeStatus.Text = message;
    private void NewProjectClicked(object sender, RoutedEventArgs e) => NewProjectRequested?.Invoke(this, EventArgs.Empty);
    private void RecentProjectClicked(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is RecentProjectItem item) OpenProjectRequested?.Invoke(this, item.Path);
    }
}

[WinRT.GeneratedBindableCustomProperty]
public sealed partial class RecentProjectItem
{
    public string Path { get; }
    public string Name => System.IO.Path.GetFileName(Path.TrimEnd(System.IO.Path.DirectorySeparatorChar));
    public RecentProjectItem(string path) => Path = path;
}
