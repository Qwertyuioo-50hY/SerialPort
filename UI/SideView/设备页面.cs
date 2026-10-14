using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Tomography.UI.SideView;

public sealed partial class DevicePage : UserControl
{
    public event EventHandler<string>? ActionRequested;
    public TextBlock StatusText => Status;
    public DevicePage() => InitializeComponent();
    public void Attach(ConnectionProtocolView connection, DeviceParametersView parameters)
    { ConnectionHost.Content = connection; TransportParametersHost.Content = connection.DetachTransportParameters(); ParametersHost.Content = parameters; }
    public void SetState(string text, bool busy, bool connected, string? error = null)
    {
        Status.Text = text; Status.Visibility = text == "未连接" ? Visibility.Collapsed : Visibility.Visible; Progress.IsActive = busy;
        ConnectButton.IsEnabled = !busy && !connected;
        HandshakeButton.IsEnabled = connected && !busy;
        DisconnectButton.IsEnabled = connected || busy;
        Error.Message = error ?? ""; Error.IsOpen = error is not null;
    }
    public void SetInputState(bool simulation, bool busy)
    { SimulationRow.Visibility = simulation ? Visibility.Visible : Visibility.Collapsed; ParametersHost.IsEnabled = !busy; }
    public void SetParameterStatus(string text) => Microsoft.UI.Xaml.Automation.AutomationProperties.SetHelpText(ParametersHost, text);
    public void SetMatrix(string text) => MatrixFile.Text = text;
    public void SetCoordinates(string text)
    { CoordinatesFile.Text = text; CoordinatesFile.Visibility = Visibility.Visible; }
    public void SetSimulation(string text) => SimulationFile.Text = text;
    private void ModulesSizeChanged(object sender, SizeChangedEventArgs e)
    {
        var narrow = e.NewSize.Width < 760;
        ParametersColumn.Width = new GridLength(narrow ? 0 : 1, GridUnitType.Star);
        Grid.SetColumn(DeviceParametersModule, narrow ? 0 : 1);
        Grid.SetRow(DeviceParametersModule, narrow ? 1 : 0);
    }
    private void HandshakeClicked(object sender, RoutedEventArgs e) => ActionRequested?.Invoke(this, "握手");
    private void DisconnectClicked(object sender, RoutedEventArgs e) => ActionRequested?.Invoke(this, "断开");
    private void ConnectClicked(object sender, RoutedEventArgs e) => ActionRequested?.Invoke(this, "连接");
    private void ImportClicked(object sender, RoutedEventArgs e) => ActionRequested?.Invoke(this, (string)((Button)sender).Tag);
    private void RemoveClicked(object sender, RoutedEventArgs e) => ActionRequested?.Invoke(this, "移除矩阵");
    private void DiagnosticsClicked(object sender, RoutedEventArgs e) => ActionRequested?.Invoke(this, "诊断");
    private void ProtocolClicked(object sender, RoutedEventArgs e) => ActionRequested?.Invoke(this, "自定义帧");
}
