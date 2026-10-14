using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Windows.Storage.Pickers;
using Tomography.Business.Models;
using Tomography.Business.Services;
using Tomography.UI.SideView;
using System.Security.Cryptography;
using System.Text;

namespace Tomography.UI.windows;

public sealed partial class AppWindow
{
    private readonly ConfigService _projectConfig = new();
    private AppSettings _projectSettings = new();
    private readonly DispatcherTimer _saveDeviceTimer = new() { Interval = TimeSpan.FromMilliseconds(400) };
    private bool _restoringDevice;
    private string _referenceConfiguration = "";
    private string ProjectSettingsPath => System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Tomography", "Projects", Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(DirectoryPanel.DirectoryPath.ToUpperInvariant()))), "settings.json");
    private void InitializeDevicePage()
    {
        _protocolSettings = new ConnectionProtocolView();
        DeviceView.Attach(_protocolSettings, _deviceSettings); ConnectionStatus = DeviceView.StatusText;
        _protocolSettings.RefreshRequested += (_, _) => RefreshDevicePorts();
        _protocolSettings.Changed += DeviceConfigurationChanged; _deviceSettings.Changed += DeviceConfigurationChanged;
        DeviceView.ActionRequested += DeviceActionRequested;
        SideRail.NavigationRequested += (_, section) => { ViewModel.Section = section; ViewModel.ParametersVisible = false; ToolsPanel.Visibility = Visibility.Visible; };
        SideRail.DirectoryToggleRequested += ToggleProjectNavigation;
        DirectoryPanel.ChooseDirectoryRequested += async (_, _) =>
        {
            if (_aiBusy || _connecting || ActiveChannel.IsOpen || _sampling || _fileOperation || _reconstructing) { ShowDeviceError("请先结束采集、断开设备并完成当前操作，再切换实验项目。"); return; }
            var folder = await new FolderPicker(AppWindow.Id).PickSingleFolderAsync();
            if (folder is null) return;
            _saveDeviceTimer.Stop(); await SaveDeviceSettingsAsync();
            foreach (var role in _projectSettings.ExperimentInputs.Keys) DirectoryPanel.SetAsset(role, null);
            _reconstruction.ClearInputs(); _projectSettings = new();
            await DirectoryPanel.LoadAsync(folder.Path); await RestoreDeviceSettingsAsync();
        };
        _saveDeviceTimer.Tick += async (_, _) => { _saveDeviceTimer.Stop(); await SaveDeviceSettingsAsync(); };
        DeviceView.Loaded += async (_, _) => { if (!_devicePageLoaded) { _devicePageLoaded = true; await RestoreDeviceSettingsAsync(); } };
        _tcpConnection.Disconnected += (_, reason) => DispatcherQueue.TryEnqueue(() => { if (!_connectionLifetime.IsCancellationRequested) { CloseDevice(); ShowDeviceError(reason); } });
    }
    private bool _devicePageLoaded;
    private async Task RestoreDeviceSettingsAsync()
    {
        _restoringDevice = true;
        try
        {
            RefreshDevicePorts();
            var path = ProjectSettingsPath;
            var result = await Task.Run(() => _projectConfig.Load(path));
            _projectSettings = result.IsSuccess && result.Data is AppSettings settings ? settings : new AppSettings();
            _protocolSettings!.Restore(_projectSettings);
            _deviceSettings.Restore(_projectSettings.Device.FrequencyKhz, _projectSettings.Device.CoilCount);
            _referenceConfiguration = ReferenceConfiguration();
            foreach (var input in _projectSettings.ExperimentInputs.ToArray())
                await ImportExperimentalInputAsync(input.Key, input.Value);
            UpdateDeviceInputs();
            if (System.IO.File.Exists(path) && !result.IsSuccess) ShowDeviceError(result.Message);
        }
        finally { _restoringDevice = false; }
    }
    private string ReferenceConfiguration() => $"{_protocolSettings!.IsSimulation}:{_protocolSettings.Protocol}:{_deviceSettings.FrequencyKhz}:{_deviceSettings.CoilCount}";
    private void DeviceConfigurationChanged(object? sender, EventArgs e)
    {
        if (_restoringDevice) return;
        var signature = ReferenceConfiguration();
        if (signature != _referenceConfiguration) { _reconstruction.InvalidateReference(); SetRealtimeReconstruction(false); _referenceConfiguration = signature; }
        DeviceView.SetParameterStatus(_reconstruction.ReferenceNeedsReview ? "参数已更改；请复核或重新采集空场参考。下位机尚未确认应用。" : "参数仅保存在本项目，下位机尚未确认应用。");
        UpdateDeviceInputs(); _saveDeviceTimer.Stop(); _saveDeviceTimer.Start();
    }
    private void UpdateDeviceInputs()
    {
        var simulation = _protocolSettings!.IsSimulation;
        DeviceView.SetInputState(simulation, _connecting || ActiveChannel.IsOpen || _sampling);
        if (!ActiveChannel.IsOpen && !_connecting)
            DeviceView.SetState(simulation && _projectSettings.ExperimentInputs.ContainsKey("物场/Demo数据") ? "仿真就绪" : simulation ? "仿真数据未就绪：请导入 64 通道数据" : "未连接", false, false);
    }
    private async Task SaveDeviceSettingsAsync()
    {
        if (_restoringDevice) return;
        try
        {
            if (!IsInteger(_deviceSettings.FrequencyKhz, 1, 999) || !IsInteger(_deviceSettings.CoilCount, 1, 99))
                throw new ArgumentException("频率需为 1–999 kHz、线圈需为 1–99 的整数。");
            _projectSettings.Device = _protocolSettings!.ReadSettings((int)_deviceSettings.FrequencyKhz, (int)_deviceSettings.CoilCount);
            _projectSettings.DefaultSerialPortConfig = _protocolSettings.GetSerialConfig();
            var path = ProjectSettingsPath; var result = await Task.Run(() => _projectConfig.Save(_projectSettings, path));
            if (!result.IsSuccess) ShowDeviceError(result.Message);
        }
        catch (Exception ex) { ShowDeviceError(ex.Message); }
    }
    private void RefreshDevicePorts()
    {
        if (_connecting || ActiveChannel.IsOpen) return;
        try { _protocolSettings!.SetPorts(_serialConnection.GetPortNames()); }
        catch (Exception ex) { ShowDeviceError(ex.Message); }
    }
    private void DeviceWindowCommand(object sender, RoutedEventArgs e) => WindowCommandRequested(sender, (string)((Button)sender).Tag);
    private void ShowDeviceError(string message) => DeviceView.SetState("操作失败", false, ActiveChannel.IsOpen, message);
    private async void DeviceActionRequested(object? sender, string action)
    {
        if (action == "连接") { ToggleDeviceConnection(); return; }
        if (action == "握手") { await RepeatDeviceHandshakeAsync(); return; }
        if (action == "断开")
        {
            if (_connecting) _deviceStop?.Cancel(); else CloseDevice();
            return;
        }
        if (action == "刷新") { RefreshDevicePorts(); return; }
        if (action is "灵敏度矩阵" or "节点坐标" or "物场/Demo数据" or "空场参考") { await ImportExperimentalInputAsync(action); return; }
        if (action == "移除矩阵")
        {
            if (_sampling || _reconstructing || _fileOperation) { ShowDeviceError("请先结束采集、重建或导入。"); return; }
            _reconstruction.RemoveSensitivity(); _projectSettings.ExperimentInputs.Remove("灵敏度矩阵");
            DirectoryPanel.SetAsset("灵敏度矩阵", null); DeviceView.SetMatrix("未导入灵敏度矩阵");
            await SaveDeviceSettingsAsync(); return;
        }
        if (action == "自定义帧") { await EditFrameDefinitionAsync(); return; }
        var text = action == "诊断" ? string.Join("\n", _deviceDiagnostics) : "自定义帧暂未启用：请提供真实帧样例、帧头、总长度、字段类型、字节序、校验和 64 通道映射。不会执行脚本或发送未知设备指令。";
        await new ContentDialog { XamlRoot = Root.XamlRoot, RequestedTheme = Root.ActualTheme, Title = action,
            Content = new ScrollViewer { Content = new TextBlock { Text = string.IsNullOrEmpty(text) ? "暂无原始收发记录" : text, TextWrapping = TextWrapping.Wrap }, MaxHeight = 420 }, CloseButtonText = "关闭" }.ShowAsync();
    }
}
