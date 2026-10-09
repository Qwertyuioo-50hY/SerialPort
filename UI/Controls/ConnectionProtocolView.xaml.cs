using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Tomography.UI.Controls;

public sealed partial class ConnectionProtocolView : UserControl
{
    private bool _ready;
    public ConnectionProtocolView()
    {
        InitializeComponent();
        _ready = true;
        UpdateConfiguration();
    }

    private void ConfigurationChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!_ready) return;
        if (ReferenceEquals(sender, DemoFormat)) DataProtocol.SelectedIndex = DemoFormat.SelectedIndex;
        else if (ReferenceEquals(sender, DataProtocol)) DemoFormat.SelectedIndex = DataProtocol.SelectedIndex;
        UpdateConfiguration();
    }

    private void UpdateConfiguration()
    {
        var source = DataSource.SelectedIndex;
        var connection = ConnectionType.SelectedIndex;
        ConnectionType.IsEnabled = source == 0;
        SerialParameters.Visibility = Show(source == 0 && connection == 0);
        TcpParameters.Visibility = Show(source == 0 && connection == 1);
        UdpParameters.Visibility = Show(source == 0 && connection == 2);
        DemoParameters.Visibility = Show(source == 1);
        ReplayParameters.Visibility = Show(source == 2);
        SimulationParameters.Visibility = Show(source == 3);
        ParametersHeading.Text = source switch
        {
            1 => "Demo数据参数配置",
            2 => "文件回放参数配置",
            3 => "模拟数据参数配置",
            _ => new[] { "串口参数配置", "TCP客户端参数配置", "UDP参数配置" }[connection]
        };

    }

    public bool UsesSerialDevice => DataSource.SelectedIndex == 0 && ConnectionType.SelectedIndex == 0;

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

    private static Visibility Show(bool visible) => visible ? Visibility.Visible : Visibility.Collapsed;
}
