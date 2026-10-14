using Microsoft.UI.Xaml;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;

namespace Tomography.UI.windows;

public partial class App : Application
{
    private Window? _window;

    public App() => InitializeComponent();

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        LiveCharts.Configure(settings => settings.AddSkiaSharp().AddDefaultMappers().AddDefaultTheme());
        _window = new AppWindow();
        _window.Activate();
    }
}
