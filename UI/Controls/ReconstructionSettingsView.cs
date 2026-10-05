using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Tomography.Business.Services;

namespace Tomography.UI.Controls;

/// <summary>算法参数和用户操作，计算由 Business 完成。</summary>
public sealed class ReconstructionSettingsView : UserControl
{
    private readonly ComboBox _algorithm = new() { Header = "重建算法", ItemsSource = new[] { "LBP", "Tikhonov", "Landweber", "TV", "Custom" }, SelectedIndex = 1, HorizontalAlignment = HorizontalAlignment.Stretch };
    private readonly ComboBox _mode = new() { Header = "差分模式", ItemsSource = new[] { "相对差分", "绝对差分" }, SelectedIndex = 0, HorizontalAlignment = HorizontalAlignment.Stretch };
    private readonly NumberBox _lambda = new() { Header = "Tikhonov λ", Value = 0.1, Minimum = 0.000001 };
    private readonly NumberBox _alpha = new() { Header = "Landweber 步长上限", Value = 0.1, Minimum = 0.000001 };
    private readonly NumberBox _iterations = new() { Header = "迭代次数", Value = 50, Minimum = 1, Maximum = 10000 };
    private readonly NumberBox _tv = new() { Header = "TV 权重", Value = 0.05, Minimum = 0.000001 };
    private readonly TextBox _python = new() { Header = "Python 运行时 DLL", Text = PythonRuntimeHost.FindPythonDll() };
    private readonly TextBlock _inputs = new() { TextWrapping = TextWrapping.Wrap };
    private readonly StackPanel _panel = new() { Spacing = 8 };
    private readonly ToggleSwitch _live = new() { Header = "实时二维重建", OnContent = "开启", OffContent = "关闭" };
    public event EventHandler? RealtimeChanged;
    public bool RealtimeEnabled => _live.IsOn;
    public void StopRealtime() => _live.IsOn = false;
    public event EventHandler? ReconstructRequested;
    public event EventHandler? ReferenceRequested;
    public event EventHandler? SyntaxCheckRequested;
    public string Algorithm => (string)_algorithm.SelectedItem;
    public string DifferenceMode => _mode.SelectedIndex == 0 ? "rel" : "abs";
    public string PythonDll => _python.Text.Trim();
    public double Lambda => _lambda.Value;
    public double Alpha => _alpha.Value;
    public double Iterations => _iterations.Value;
    public double TvMu => _tv.Value;
    public ReconstructionSettingsView()
    {
        _panel.Children.Add(new TextBlock { Text = "EMT 二维重建", FontSize = 14 });
        foreach (var control in new FrameworkElement[] { _algorithm, _mode, _lambda, _alpha, _iterations, _tv, _python, _inputs, _live }) _panel.Children.Add(control);
        AddButton("空场采集（10 帧平均）", "CaptureReference", () => ReferenceRequested?.Invoke(this, EventArgs.Empty));
        AddButton("检查自定义 Python 语法", "CheckPythonSyntax", () => SyntaxCheckRequested?.Invoke(this, EventArgs.Empty));
        AddButton("重建", "RunReconstruction", () => ReconstructRequested?.Invoke(this, EventArgs.Empty));
        AutomationProperties.SetAutomationId(_algorithm, "ReconstructionAlgorithm");
        AutomationProperties.SetAutomationId(_python, "PythonRuntimeDll");
        AutomationProperties.SetAutomationId(_live, "RealtimeReconstruction");
        _live.Toggled += (_, _) => RealtimeChanged?.Invoke(this, EventArgs.Empty);
        Content = _panel;
    }
    private void AddButton(string label, string id, Action action)
    {
        var button = new Button { Content = label, HorizontalAlignment = HorizontalAlignment.Stretch };
        AutomationProperties.SetAutomationId(button, id);
        button.Click += (_, _) => action();
        _panel.Children.Add(button);
    }
    public void UpdateInputs(ReconstructionSession session) => _inputs.Text =
        $"灵敏度：{System.IO.Path.GetFileName(session.SensitivityPath) ?? "未导入"}\n物场：{(session.Measurements is null ? "未设置" : "64 通道")}\n空场：{(session.Reference is null ? "未设置" : "64 通道")}\n自定义：{System.IO.Path.GetFileName(session.CustomAlgorithmPath) ?? "未导入"}";
    public void SetBusy(bool busy)
    {
        foreach (var control in _panel.Children.OfType<Control>())
            if (!ReferenceEquals(control, _live)) control.IsEnabled = !busy;
    }
}

