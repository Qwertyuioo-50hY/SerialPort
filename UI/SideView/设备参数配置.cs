using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;

namespace Tomography.UI.SideView;

public sealed class DeviceParametersView : UserControl
{
    private readonly NumberBox _frequency = new() { Value = 100, Minimum = 1, Maximum = 999 };
    private readonly NumberBox _coils = new() { Value = 8, Minimum = 1, Maximum = 99 };
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
        var panel = new Grid { ColumnSpacing = 24, RowSpacing = 12, Margin = new Microsoft.UI.Xaml.Thickness(0) };
        panel.ColumnDefinitions.Add(new ColumnDefinition());
        panel.ColumnDefinitions.Add(new ColumnDefinition());
        panel.RowDefinitions.Add(new RowDefinition { Height = Microsoft.UI.Xaml.GridLength.Auto });
        panel.RowDefinitions.Add(new RowDefinition { Height = Microsoft.UI.Xaml.GridLength.Auto });
        var frequencyRow = ParameterRow("激励频率 / kHz", _frequency);
        var coilsRow = ParameterRow("线圈数量", _coils);
        panel.SizeChanged += (_, e) =>
        {
            var narrow = e.NewSize.Width < 560;
            Grid.SetColumn(coilsRow, narrow ? 0 : 1); Grid.SetRow(coilsRow, narrow ? 1 : 0);
            Grid.SetColumnSpan(frequencyRow, narrow ? 2 : 1); Grid.SetColumnSpan(coilsRow, narrow ? 2 : 1);
        };
        panel.Children.Add(frequencyRow);
        panel.Children.Add(coilsRow);
        Content = panel;
    }

    private static Grid ParameterRow(string label, NumberBox field)
    {
        var row = new Grid { ColumnSpacing = 16 };
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = Microsoft.UI.Xaml.GridLength.Auto });
        row.ColumnDefinitions.Add(new ColumnDefinition());
        row.Children.Add(new TextBlock { Text = label, VerticalAlignment = Microsoft.UI.Xaml.VerticalAlignment.Center });
        AutomationProperties.SetName(field, label);
        Grid.SetColumn(field, 1);
        row.Children.Add(field);
        return row;
    }
}
