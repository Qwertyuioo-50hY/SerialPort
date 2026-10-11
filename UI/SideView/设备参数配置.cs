using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;

namespace Tomography.UI.SideView;

public sealed class DeviceParametersView : UserControl
{
    private readonly NumberBox _frequency = new() { Header = "激励频率 / kHz", Value = 100, Minimum = 1, Maximum = 999 };
    private readonly NumberBox _coils = new() { Header = "线圈数", Value = 8, Minimum = 1, Maximum = 99 };
    public double FrequencyKhz => _frequency.Value;
    public double CoilCount => _coils.Value;

    public DeviceParametersView()
    {
        AutomationProperties.SetAutomationId(_frequency, "ExcitationFrequency");
        AutomationProperties.SetAutomationId(_coils, "CoilCount");
        var panel = new StackPanel { Spacing = 8 };
        panel.Children.Add(_frequency);
        panel.Children.Add(_coils);
        Content = panel;
    }
}
