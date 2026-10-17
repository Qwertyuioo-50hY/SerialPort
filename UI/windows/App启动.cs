using Microsoft.UI.Xaml;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;

namespace Tomography.UI.windows;

public partial class App : Application
{
    private Window? _window;

    public App() => InitializeComponent();

    protected override async void OnLaunched(LaunchActivatedEventArgs args)
    {
        LiveCharts.Configure(settings => settings.AddSkiaSharp().AddDefaultMappers().AddDefaultTheme());
        var window = new AppWindow();
        _window = window;
        _window.Activate();
        await window.StartQtWindowAsync();
    }
}
