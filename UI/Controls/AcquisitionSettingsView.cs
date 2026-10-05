using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Tomography.Business.Models;

namespace Tomography.UI.Controls;

public sealed class AcquisitionSettingsView : UserControl
{
    private readonly NumberBox _frequency = new() { Header = "激励频率（kHz，整数 1–999）", Minimum = 1, Maximum = 999, Value = 100 };
    private readonly NumberBox _coils = new() { Header = "线圈数量（整数 1–99）", Minimum = 1, Maximum = 99, Value = 8 };
    private readonly NumberBox _frames = new() { Header = "多帧数量", Minimum = 1, Maximum = 10000, Value = 10 };
    private readonly List<Button> _startButtons = new();
    private readonly Button _stop = new() { Content = "停止采集", IsEnabled = false, HorizontalAlignment = HorizontalAlignment.Stretch };
    public event EventHandler<AcquisitionMode>? AcquisitionRequested;
    public event EventHandler? StopRequested;
    public double FrequencyKhz => _frequency.Value;
    public double CoilCount => _coils.Value;
    public double FrameCount => _frames.Value;
    public AcquisitionSettingsView()
    {
        var panel = new StackPanel { Spacing = 8, Margin = new Thickness(0, 16, 0, 0) };
        panel.Children.Add(new TextBlock { Text = "设备采集" });
        foreach (var control in new Control[] { _frequency, _coils, _frames }) panel.Children.Add(control);
        foreach (var item in new[] { ("空场采集（10 帧平均）", AcquisitionMode.Reference), ("单帧采集", AcquisitionMode.Single),
            ("多帧采集", AcquisitionMode.Batch), ("实时采集", AcquisitionMode.Live) })
        {
            var button = new Button { Content = item.Item1, HorizontalAlignment = HorizontalAlignment.Stretch };
            AutomationProperties.SetAutomationId(button, "Acquire" + item.Item2);
            button.Click += (_, _) => AcquisitionRequested?.Invoke(this, item.Item2);
            panel.Children.Add(button); _startButtons.Add(button);
        }
        _stop.Click += (_, _) => StopRequested?.Invoke(this, EventArgs.Empty);
        AutomationProperties.SetAutomationId(_stop, "StopAcquisition");
        AutomationProperties.SetAutomationId(_frequency, "ExcitationFrequency");
        AutomationProperties.SetAutomationId(_coils, "CoilCount");
        AutomationProperties.SetAutomationId(_frames, "AcquisitionFrameCount");
        panel.Children.Add(_stop); Content = panel;
    }
    public void SetBusy(bool busy)
    {
        _frequency.IsEnabled = _coils.IsEnabled = _frames.IsEnabled = !busy;
        foreach (var button in _startButtons) button.IsEnabled = !busy;
        _stop.IsEnabled = busy;
    }
}
