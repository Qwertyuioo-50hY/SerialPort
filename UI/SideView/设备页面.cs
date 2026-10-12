using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Tomography.UI.SideView;

public sealed partial class DevicePage : UserControl
{
    public event EventHandler<string>? ActionRequested;
    public TextBlock StatusText => Status;
    public DevicePage() => InitializeComponent();
    public void Attach(ConnectionProtocolView connection, DeviceParametersView parameters)
    { ConnectionHost.Content = connection; ParametersHost.Content = parameters; }
    public void SetState(string text, bool busy, bool connected, string? error = null)
    {
        Status.Text = text; Progress.IsActive = busy;
        ConnectButton.Content = connected ? "断开" : busy ? "取消" : "连接";
        Error.Message = error ?? ""; Error.IsOpen = error is not null;
    }
    public void SetInputState(bool simulation, bool busy)
    { SimulationRow.Visibility = simulation ? Visibility.Visible : Visibility.Collapsed; ParametersHost.IsEnabled = !busy; }
    public void SetParameterStatus(string text) => ParameterStatus.Text = text;
    public void SetMatrix(string text) => MatrixFile.Text = text;
    public void SetCoordinates(string text) => CoordinatesFile.Text = text;
    public void SetSimulation(string text) => SimulationFile.Text = text;
    private void ConnectClicked(object sender, RoutedEventArgs e) => ActionRequested?.Invoke(this, "连接");
    private void RefreshClicked(object sender, RoutedEventArgs e) => ActionRequested?.Invoke(this, "刷新");
    private void ImportClicked(object sender, RoutedEventArgs e) => ActionRequested?.Invoke(this, (string)((Button)sender).Tag);
    private void RemoveClicked(object sender, RoutedEventArgs e) => ActionRequested?.Invoke(this, "移除矩阵");
    private void DiagnosticsClicked(object sender, RoutedEventArgs e) => ActionRequested?.Invoke(this, "诊断");
    private void ProtocolClicked(object sender, RoutedEventArgs e) => ActionRequested?.Invoke(this, "自定义帧");
}
