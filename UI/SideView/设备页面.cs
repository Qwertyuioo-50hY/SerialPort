using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace Tomography.UI.SideView;

public sealed partial class DevicePage : UserControl
{
    public event EventHandler<string>? ActionRequested;
    private bool _emt;
    private bool _busy, _connected;
    public void SetProtocol(int protocol) { _emt = protocol == 1; UpdateButtons(); }
    private void UpdateButtons()
    {
        ConnectButton.IsEnabled = !_busy && !_connected;
        HandshakeButton.IsEnabled = _emt && _connected && !_busy;
        DisconnectButton.IsEnabled = _connected || _busy;
    }
    private readonly DispatcherTimer _importTimer = new() { Interval = TimeSpan.FromSeconds(4) };
    public TextBlock StatusText => Status;
    public FrameSettingsView FrameEditor => FrameSettings;
    public DevicePage()
    {
        InitializeComponent();
        _importTimer.Tick += (_, _) => { _importTimer.Stop(); ImportStatus.Visibility = Visibility.Collapsed; };
        Unloaded += (_, _) => _importTimer.Stop();
    }
    public void Attach(ConnectionProtocolView connection, DeviceParametersView parameters)
    { ConnectionHost.Content = connection; TransportParametersHost.Content = connection.DetachTransportParameters(); ParametersHost.Content = parameters; }
    public void SetState(string text, bool busy, bool connected, string? error = null)
    {
        Status.Text = text; Status.Visibility = text == "未连接" ? Visibility.Collapsed : Visibility.Visible; Progress.IsActive = busy;
        _busy = busy; _connected = connected; UpdateButtons();
        Error.Title = error is null ? "" : "设备操作失败";
        ErrorDetails.Text = error is null ? "" : string.IsNullOrWhiteSpace(error) ? "设备操作失败，请检查连接和握手状态。" : error;
        Error.IsOpen = error is not null;
    }
    public void SetInputState(bool busy) => ParametersHost.IsEnabled = !busy;
    public void SetParameterStatus(string text) => Microsoft.UI.Xaml.Automation.AutomationProperties.SetHelpText(ParametersHost, text);
    public void SetMatrix(string text) => MatrixFile.Text = text;
    public void ShowImportResult(bool success)
    {
        _importTimer.Stop(); ImportStatus.Text = success ? "导入成功" : "导入失败";
        ImportStatus.Foreground = new SolidColorBrush(success ? Microsoft.UI.ColorHelper.FromArgb(255, 40, 170, 105) : Microsoft.UI.ColorHelper.FromArgb(255, 230, 85, 85));
        ImportStatus.Visibility = Visibility.Visible; _importTimer.Start();
    }
    public void AppendReceived(byte[] bytes, string encoding, bool binary = false) => ReceivedData.Append(bytes, encoding, binary);
    public void AppendEmtFrame(uint[] values) => ReceivedData.AppendEmtFrame(values);
    private void HandshakeClicked(object sender, RoutedEventArgs e) => ActionRequested?.Invoke(this, "握手");
    private void DisconnectClicked(object sender, RoutedEventArgs e) => ActionRequested?.Invoke(this, "断开");
    private void ConnectClicked(object sender, RoutedEventArgs e) => ActionRequested?.Invoke(this, "连接");
    private void ImportClicked(object sender, RoutedEventArgs e) => ActionRequested?.Invoke(this, (string)((Button)sender).Tag);
}
