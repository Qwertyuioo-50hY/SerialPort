using System.IO;
using Tomography.Business.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Tomography.UI.Home;

public sealed partial class HomeView : UserControl
{
    public event EventHandler? NewProjectRequested;
    public event EventHandler? BrowseProjectRequested;
    public event EventHandler<string>? OpenProjectRequested;
    public event EventHandler<RecentChatEntry>? OpenChatRequested;
    private string _username = "";
    public HomeView()
    {
        InitializeComponent();
        var timer = new DispatcherTimer { Interval = TimeSpan.FromMinutes(1) };
        timer.Tick += (_, _) => UpdateGreeting();
        Loaded += (_, _) => { UpdateGreeting(); timer.Start(); };
        Unloaded += (_, _) => timer.Stop();
    }
    public void SetGreeting(string username) { _username = username; UpdateGreeting(); }
    private void UpdateGreeting()
    {
        var hour = DateTime.Now.Hour;
        Welcome.Text = $"Good {(hour < 12 ? "morning" : hour < 18 ? "afternoon" : "evening")}, {_username}";
    }
    public void SetRecentChats(IReadOnlyList<RecentChatEntry> chats)
    {
        RecentChats.ItemsSource = chats;
        ChatHeading.Visibility = chats.Count == 0 ? Visibility.Collapsed : Visibility.Visible;
    }
    private void RecentChatClicked(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is RecentChatEntry chat) OpenChatRequested?.Invoke(this, chat);
    }
    public void SetRecentProjects(IEnumerable<string>? paths)
    {
        var projects = (paths ?? Array.Empty<string>()).Select(path => new RecentProjectItem(path)).ToArray();
        RecentProjects.ItemsSource = projects;
        RecentHeading.Visibility = projects.Length > 0 ? Visibility.Visible : Visibility.Collapsed;
    }
    public void SetStatus(string message)
    {
        HomeStatus.Text = message;
        HomeStatus.Visibility = string.IsNullOrWhiteSpace(message) ? Visibility.Collapsed : Visibility.Visible;
    }
    private void OpenProjectClicked(object sender, RoutedEventArgs e) => BrowseProjectRequested?.Invoke(this, EventArgs.Empty);
    private void CreateProjectSplitClicked(SplitButton sender, SplitButtonClickEventArgs args) => NewProjectRequested?.Invoke(this, EventArgs.Empty);
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
