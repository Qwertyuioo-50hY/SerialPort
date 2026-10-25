using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Tomography.UI.Controls;
using Microsoft.UI.Xaml.Controls;

namespace Tomography.UI.SideView;

public sealed class DeviceParametersView : UserControl
{
    private readonly NumberBox _frequency = new() { Value = 100, Minimum = 1, Maximum = 500 };
    private readonly NumberBox _coils = new() { Value = 8, Minimum = 1, Maximum = 32 };
    private readonly ClickComboBox _arrayType = new() { ItemsSource = new[] { "环形阵列（R）", "方形阵列（S）" }, SelectedIndex = 0, HorizontalAlignment = HorizontalAlignment.Stretch, CornerRadius = new(12) };
    private readonly ClickComboBox _excitationMode = new() { ItemsSource = new[] { "恒压激励（V）", "恒流激励（I）" }, SelectedIndex = 0, HorizontalAlignment = HorizontalAlignment.Stretch, CornerRadius = new(12) };
    public string ArrayType => _arrayType.SelectedIndex == 1 ? "S" : "R";
    public string ExcitationMode => _excitationMode.SelectedIndex == 1 ? "I" : "V";
    public event EventHandler? Changed;
    public void Restore(double frequency, double coils, string arrayType = "R", string excitationMode = "V")
    {
        _frequency.Value = frequency; _coils.Value = coils;
        _arrayType.SelectedIndex = arrayType == "S" ? 1 : 0;
        _excitationMode.SelectedIndex = excitationMode == "I" ? 1 : 0;
    }
    public double FrequencyKhz => _frequency.Value;
    public double CoilCount => _coils.Value;

    public DeviceParametersView()
    {
        AutomationProperties.SetAutomationId(_frequency, "ExcitationFrequency");
        AutomationProperties.SetAutomationId(_coils, "CoilCount");
        AutomationProperties.SetAutomationId(_arrayType, "ArrayType");
        AutomationProperties.SetAutomationId(_excitationMode, "ExcitationMode");
        _arrayType.SelectionChanged += (_, _) => Changed?.Invoke(this, EventArgs.Empty);
        _excitationMode.SelectionChanged += (_, _) => Changed?.Invoke(this, EventArgs.Empty);
        _frequency.ValueChanged += (_, _) => Changed?.Invoke(this, EventArgs.Empty);
        _coils.ValueChanged += (_, _) => Changed?.Invoke(this, EventArgs.Empty);
        var panel = new Grid { ColumnSpacing = 24, RowSpacing = 12, Margin = new Microsoft.UI.Xaml.Thickness(0) };
        panel.ColumnDefinitions.Add(new ColumnDefinition());
        panel.ColumnDefinitions.Add(new ColumnDefinition());
        var rows = new[] { ParameterRow("激励频率 / kHz", _frequency), ParameterRow("线圈数量", _coils),
            ParameterRow("阵列类型", _arrayType), ParameterRow("激励方式", _excitationMode) };
        foreach (var row in rows)
        {
            panel.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            Grid.SetRow(row, panel.Children.Count); Grid.SetColumnSpan(row, 2);
            panel.Children.Add(row);
        }
        panel.SizeChanged += (_, e) =>
        {
            var narrow = e.NewSize.Width < 560;
            for (var i = 0; i < rows.Length; i++)
            {
                Grid.SetColumn(rows[i], narrow ? 0 : i % 2); Grid.SetRow(rows[i], narrow ? i : i / 2);
                Grid.SetColumnSpan(rows[i], narrow ? 2 : 1);
            }
        };
        Content = panel;
    }

    private static Grid ParameterRow(string label, Control field)
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
