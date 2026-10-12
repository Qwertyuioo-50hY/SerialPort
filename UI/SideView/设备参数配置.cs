using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;

namespace Tomography.UI.SideView;

public sealed class DeviceParametersView : UserControl
{
    private readonly NumberBox _frequency = new() { Header = "激励频率 / kHz", Value = 100, Minimum = 1, Maximum = 999 };
    private readonly NumberBox _coils = new() { Header = "线圈数", Value = 8, Minimum = 1, Maximum = 99 };
    public event EventHandler? Changed;
    public void Restore(double frequency, double coils) { _frequency.Value = frequency; _coils.Value = coils; }
    public double FrequencyKhz => _frequency.Value;
    public double CoilCount => _coils.Value;

    public DeviceParametersView()
    {
        AutomationProperties.SetAutomationId(_frequency, "ExcitationFrequency");
        AutomationProperties.SetAutomationId(_coils, "CoilCount");
        _frequency.ValueChanged += (_, _) => Changed?.Invoke(this, EventArgs.Empty);
        _coils.ValueChanged += (_, _) => Changed?.Invoke(this, EventArgs.Empty);
        var panel = new Grid { ColumnSpacing = 24, RowSpacing = 12 };
        panel.ColumnDefinitions.Add(new ColumnDefinition());
        panel.ColumnDefinitions.Add(new ColumnDefinition());
        panel.RowDefinitions.Add(new RowDefinition { Height = Microsoft.UI.Xaml.GridLength.Auto });
        panel.RowDefinitions.Add(new RowDefinition { Height = Microsoft.UI.Xaml.GridLength.Auto });
        panel.SizeChanged += (_, e) =>
        {
            var narrow = e.NewSize.Width < 560;
            Grid.SetColumn(_coils, narrow ? 0 : 1); Grid.SetRow(_coils, narrow ? 1 : 0);
            Grid.SetColumnSpan(_frequency, narrow ? 2 : 1); Grid.SetColumnSpan(_coils, narrow ? 2 : 1);
        };
        panel.Children.Add(_frequency);
        panel.Children.Add(_coils);
        Content = panel;
    }
}
