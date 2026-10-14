using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Tomography.Business.Services;

namespace Tomography.UI.SideView;

/// <summary>算法参数和用户操作，计算由 Business 完成。</summary>
public sealed class AlgorithmParametersView : UserControl
{
    private readonly ComboBox _algorithm = new() { Header = "重建算法", ItemsSource = new[] { "LBP", "Tikhonov", "Landweber", "TV", "Custom" }, SelectedIndex = 1, HorizontalAlignment = HorizontalAlignment.Stretch };
    private readonly ComboBox _mode = new() { Header = "差分模式", ItemsSource = new[] { "相对差分", "绝对差分" }, SelectedIndex = 0, HorizontalAlignment = HorizontalAlignment.Stretch };
    private readonly NumberBox _lambda = new() { Header = "Tikhonov λ", Value = 0.1, Minimum = 0.000001 };
    private readonly NumberBox _alpha = new() { Header = "Landweber 步长上限", Value = 0.1, Minimum = 0.000001 };
    private readonly NumberBox _iterations = new() { Header = "迭代次数", Value = 50, Minimum = 1, Maximum = 10000 };
    private readonly NumberBox _tv = new() { Header = "TV 权重", Value = 0.05, Minimum = 0.000001 };
    private readonly TextBox _python = new() { Header = "Python 运行时 DLL", Text = PythonRuntimeHost.FindPythonDll() };
    private readonly StackPanel _panel = new() { Spacing = 8 };
    public event EventHandler? CustomAlgorithmRequested;
    public string Algorithm => (string)_algorithm.SelectedItem;
    public string DifferenceMode => _mode.SelectedIndex == 0 ? "rel" : "abs";
    public string PythonDll => _python.Text.Trim();
    public double Lambda => _lambda.Value;
    public double Alpha => _alpha.Value;
    public double Iterations => _iterations.Value;
    public double TvMu => _tv.Value;
    public AlgorithmParametersView()
    {
        _panel.Children.Add(new TextBlock { Text = "EMT 二维重建", FontSize = 14 });
        foreach (var control in new FrameworkElement[] { _algorithm, _mode, _lambda, _alpha, _iterations, _tv, _python }) _panel.Children.Add(control);
        AutomationProperties.SetAutomationId(_algorithm, "ReconstructionAlgorithm");
        AutomationProperties.SetAutomationId(_python, "PythonRuntimeDll");
        _algorithm.SelectionChanged += (_, _) => UpdateParameters();
        var custom = new Button { Content = "选择自定义 Python 算法", HorizontalAlignment = HorizontalAlignment.Stretch };
        AutomationProperties.SetAutomationId(custom, "ImportCustomAlgorithm");
        custom.Click += (_, _) => CustomAlgorithmRequested?.Invoke(this, EventArgs.Empty);
        _panel.Children.Add(custom);
        UpdateParameters();
        Content = _panel;
    }
    private void UpdateParameters()
    {
        _lambda.Visibility = Algorithm == "Tikhonov" ? Visibility.Visible : Visibility.Collapsed;
        _alpha.Visibility = Algorithm == "Landweber" ? Visibility.Visible : Visibility.Collapsed;
        _iterations.Visibility = Algorithm is "Landweber" or "TV" ? Visibility.Visible : Visibility.Collapsed;
        _tv.Visibility = Algorithm == "TV" ? Visibility.Visible : Visibility.Collapsed;
    }
    public void SetBusy(bool busy)
    {
        foreach (var control in _panel.Children.OfType<Control>()) control.IsEnabled = !busy;
    }
}
