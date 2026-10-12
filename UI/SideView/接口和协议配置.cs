using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Tomography.UI.SideView;

public sealed partial class ConnectionProtocolView : UserControl
{
    private bool _ready;
    public event EventHandler? Changed;
    public event EventHandler? RefreshRequested;
    public bool IsSimulation => DataSource.SelectedIndex == 1;
    public bool UsesSerialDevice => !IsSimulation && ConnectionType.SelectedIndex == 0;
    public bool UsesTcpDevice => !IsSimulation && ConnectionType.SelectedIndex == 1;
    public int Protocol => DataProtocol.SelectedIndex;
    public string Host => TcpHost.Text.Trim();
    public double Port => TcpPort.Value;
    public double TimeoutSeconds => TcpTimeout.Value;
    public ConnectionProtocolView() { InitializeComponent(); _ready = true; UpdateConfiguration(); }
    private void ConfigurationChanged(object sender, SelectionChangedEventArgs e) => NotifyChanged();
    private void FlagChanged(object sender, RoutedEventArgs e) => NotifyChanged();
    private void TextChanged(object sender, TextChangedEventArgs e) => NotifyChanged();
    private void NumberChanged(NumberBox sender, NumberBoxValueChangedEventArgs e) => NotifyChanged();
    private void NotifyChanged()
    {
        if (!_ready) return;
        UpdateConfiguration(); Changed?.Invoke(this, EventArgs.Empty);
    }
    private void RefreshClicked(object sender, RoutedEventArgs e) => RefreshRequested?.Invoke(this, EventArgs.Empty);
    private void UpdateConfiguration()
    {
        ConnectionType.Visibility = Show(!IsSimulation);
        SerialParameters.Visibility = Show(UsesSerialDevice);
        TcpParameters.Visibility = Show(UsesTcpDevice);
        ProtocolNote.Text = Protocol switch
        {
            1 => "FireWater：逗号分隔，换行结束，可带名称前缀。按 CSV 顺序映射 64 通道；与 EMT 二进制帧不同。",
            2 => "自定义帧需提供固件帧样例、长度、字段、字节序、校验与通道映射；当前不发送未知协议指令。",
            _ => "现有 EMT 固件：which emt 握手，256 字节二进制采样帧，固定 64 个测量通道。"
        };
    }
    private void LayoutChanged(object sender, SizeChangedEventArgs e)
    {
        var narrow = e.NewSize.Width < 680;
        Grid.SetColumn(ConnectionType, narrow ? 0 : 1); Grid.SetRow(ConnectionType, narrow ? 1 : 0);
        Grid.SetColumn(DataProtocol, narrow ? 0 : 2); Grid.SetRow(DataProtocol, narrow ? 2 : 0);
        Grid.SetColumnSpan(DataSource, narrow ? 3 : 1);
        Grid.SetColumnSpan(ConnectionType, narrow ? 3 : 1);
        Grid.SetColumnSpan(DataProtocol, narrow ? 3 : 1);
    }
    public void SetPorts(string[] ports)
    {
        var selected = DevicePortField.SelectedItem as string;
        DevicePortField.ItemsSource = ports;
        DevicePortField.SelectedItem = ports.Contains(selected) ? selected : ports.FirstOrDefault();
    }

    public Tomography.Business.Models.SerialPortConfig GetSerialConfig() => new()
    {
        PortName = DevicePortField.SelectedItem as string ?? "",
        BaudRate = int.Parse((string)BaudRateField.SelectedItem),
        DataBits = int.Parse((string)DataBitsField.SelectedItem),
        Parity = Enum.Parse<System.IO.Ports.Parity>((string)ParityField.SelectedItem),
        StopBits = StopBitsField.SelectedIndex switch { 1 => System.IO.Ports.StopBits.Two, 2 => System.IO.Ports.StopBits.OnePointFive, _ => System.IO.Ports.StopBits.One },
        Handshake = Enum.Parse<System.IO.Ports.Handshake>((string)FlowControlField.SelectedItem),
        EncodingName = (string)EncodingField.SelectedItem,
        DtrEnable = DTRField.IsChecked == true,
        RtsEnable = RTSField.IsChecked == true
    };

    public Tomography.Business.Models.DeviceConfiguration ReadSettings(int frequency, int coils)
    {
        if (!double.IsFinite(Port) || Port != Math.Truncate(Port) || Port is < 1 or > 65535 ||
            !double.IsFinite(TimeoutSeconds) || TimeoutSeconds != Math.Truncate(TimeoutSeconds) || TimeoutSeconds is < 1 or > 60)
            throw new ArgumentException("TCP 端口需为 1–65535、超时需为 1–60 秒的整数。");
        return new() { Source = DataSource.SelectedIndex, Connection = ConnectionType.SelectedIndex, Protocol = Protocol,
            Host = Host, Port = (int)Port, TimeoutSeconds = (int)TimeoutSeconds, FrequencyKhz = frequency, CoilCount = coils };
    }
    public void Restore(Tomography.Business.Models.AppSettings settings)
    {
        _ready = false;
        try
        {
            var d = settings.Device; var serial = settings.DefaultSerialPortConfig;
            DataSource.SelectedIndex = d.Source is 0 or 1 ? d.Source : 0;
            ConnectionType.SelectedIndex = d.Connection is 0 or 1 ? d.Connection : 0;
            DataProtocol.SelectedIndex = d.Protocol is >= 0 and <= 2 ? d.Protocol : 0;
            TcpHost.Text = d.Host; TcpPort.Value = d.Port; TcpTimeout.Value = d.TimeoutSeconds;
            DevicePortField.SelectedItem = serial.PortName;
            BaudRateField.SelectedItem = serial.BaudRate.ToString(); DataBitsField.SelectedItem = serial.DataBits.ToString();
            ParityField.SelectedItem = serial.Parity.ToString(); FlowControlField.SelectedItem = serial.Handshake.ToString();
            StopBitsField.SelectedIndex = serial.StopBits switch { System.IO.Ports.StopBits.Two => 1, System.IO.Ports.StopBits.OnePointFive => 2, _ => 0 };
            EncodingField.SelectedItem = serial.EncodingName; DTRField.IsChecked = serial.DtrEnable; RTSField.IsChecked = serial.RtsEnable;
        }
        finally { _ready = true; UpdateConfiguration(); }
    }
    public void SetBusy(bool busy) => IsEnabled = !busy;
    private static Visibility Show(bool visible) => visible ? Visibility.Visible : Visibility.Collapsed;
}
