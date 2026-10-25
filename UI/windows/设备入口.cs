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
    private string ProjectSettingsPath => System.IO.Path.Combine(DirectoryPanel.DirectoryPath, ProjectStore.ConfigFileName);
    private string LegacyProjectSettingsPath => System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Tomography", "Projects", Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(DirectoryPanel.DirectoryPath.ToUpperInvariant()))), "settings.json");
    private void InitializeDevicePage()
    {
        _protocolSettings = new ConnectionProtocolView();
        DeviceView.Attach(_protocolSettings, _deviceSettings); ConnectionStatus = DeviceView.StatusText;
        _protocolSettings.RefreshRequested += (_, _) => RefreshDevicePorts();
        _protocolSettings.Changed += DeviceConfigurationChanged; _deviceSettings.Changed += DeviceConfigurationChanged;
        DeviceView.ActionRequested += DeviceActionRequested;
        DeviceView.FrameEditor.SaveRequested += async (_, definition) =>
        {
            if (_connecting || ActiveChannel.IsOpen || _sampling) { DeviceView.FrameEditor.SetStatus("请先断开设备再修改帧配置。"); return; }
            var previous = _projectSettings.CustomFrame;
            try
            {
                _projectSettings.CustomFrame = definition;
                var path = ProjectSettingsPath;
                var saved = await Task.Run(() => _projectConfig.Save(_projectSettings, path));
                if (!saved.IsSuccess) throw new InvalidOperationException(saved.Message);
                _reconstruction.InvalidateReference(); DeviceView.FrameEditor.SetStatus("已保存");
            }
            catch (Exception ex) { _projectSettings.CustomFrame = previous; DeviceView.FrameEditor.SetStatus(ex.Message); }
        };
        _networkConnection.Disconnected += (_, reason) => DispatcherQueue.TryEnqueue(() => { if (!_connectionLifetime.IsCancellationRequested) { CloseDevice(); ShowDeviceError(reason); } });
        SideRail.NavigationRequested += (_, section) => { ViewModel.Section = section; ViewModel.ParametersVisible = false; ToolsPanel.Visibility = Visibility.Visible; };
        SideRail.DirectoryToggleRequested += ToggleProjectNavigation;
        DirectoryPanel.ChooseDirectoryRequested += async (_, _) => await BrowseProjectAsync();
        DirectoryPanel.CreateProjectRequested += async (_, _) => await CreateProjectAsync();
        _saveDeviceTimer.Tick += async (_, _) => { _saveDeviceTimer.Stop(); await SaveDeviceSettingsAsync(); };
        DeviceView.Loaded += async (_, _) => { if (!_devicePageLoaded) { _devicePageLoaded = true; await RestoreDeviceSettingsAsync(); } };
        _tcpConnection.Disconnected += (_, reason) => DispatcherQueue.TryEnqueue(() => { if (!_connectionLifetime.IsCancellationRequested) { CloseDevice(); ShowDeviceError(reason); } });
    }
    private async Task BrowseProjectAsync()
    {
        if (!CanChangeProject()) return;
        try
        {
            var folder = await new FolderPicker(PickerWindowId).PickSingleFolderAsync();
            if (folder is not null) await SwitchProjectAsync(folder.Path);
        }
        catch (Exception) { HomePanel.SetStatus("项目打开失败，请检查文件夹权限和配置。"); }
    }
    private bool _devicePageLoaded;
    private async Task RestoreDeviceSettingsAsync()
    {
        _restoringDevice = true;
        try
        {
            RefreshDevicePorts();
            var path = ProjectSettingsPath;
            var source = !System.IO.File.Exists(path) && System.IO.File.Exists(LegacyProjectSettingsPath) ? LegacyProjectSettingsPath : path;
            var result = await Task.Run(() => _projectConfig.Load(source));
            if (source != path && result.IsSuccess && result.Data is AppSettings migrated)
            {
                var saved = await Task.Run(() => _projectConfig.Save(migrated, path));
                if (!saved.IsSuccess) ShowDeviceError(saved.Message);
            }
            _projectSettings = result.IsSuccess && result.Data is AppSettings settings ? settings : new AppSettings();
            _protocolSettings!.Restore(_projectSettings);
            DeviceView.FrameEditor.LoadDefinition(_projectSettings.CustomFrame);
            _deviceSettings.Restore(_projectSettings.Device.FrequencyKhz, _projectSettings.Device.CoilCount, _projectSettings.Device.ArrayType, _projectSettings.Device.ExcitationMode);
            _referenceConfiguration = ReferenceConfiguration();
            foreach (var input in _projectSettings.ExperimentInputs.ToArray())
                if (input.Key is "灵敏度矩阵" or "空场参考") await ImportExperimentalInputAsync(input.Key, input.Value);
            UpdateDeviceInputs();
            if (System.IO.File.Exists(path) && !result.IsSuccess) ShowDeviceError(result.Message);
        }
        finally { _restoringDevice = false; }
    }
    private string ReferenceConfiguration() => $"{_protocolSettings!.Transport}:{_protocolSettings.Protocol}:{_deviceSettings.FrequencyKhz}:{_deviceSettings.CoilCount}:{_deviceSettings.ArrayType}:{_deviceSettings.ExcitationMode}";
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
        DeviceView.SetProtocol(_protocolSettings!.Protocol);
        DeviceView.SetInputState(_connecting || ActiveChannel.IsOpen || _sampling);
        if (!ActiveChannel.IsOpen && !_connecting) DeviceView.SetState("未连接", false, false);
    }
    private async Task SaveDeviceSettingsAsync()
    {
        if (_restoringDevice) return;
        try
        {
            if (!IsInteger(_deviceSettings.FrequencyKhz, 1, 500) || !IsInteger(_deviceSettings.CoilCount, 1, 32))
                throw new ArgumentException("频率需为 1–500 kHz、线圈需为 1–32 的整数。");
            _projectSettings.Device = _protocolSettings!.ReadSettings((int)_deviceSettings.FrequencyKhz, (int)_deviceSettings.CoilCount, _deviceSettings.ArrayType, _deviceSettings.ExcitationMode);
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
        if (action == "灵敏度矩阵") await ImportExperimentalInputAsync(action);
    }
}
