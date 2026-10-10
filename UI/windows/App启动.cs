using Microsoft.UI.Xaml;

namespace Tomography.UI.windows;

public partial class App : Application
{
    private Window? _window;

    public App() => InitializeComponent();

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        _window = new AppWindow();
        _window.Activate();
    }
}
