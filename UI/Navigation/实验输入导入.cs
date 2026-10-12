using System.IO;
using Tomography.Business.Models;
using Tomography.Business.Services;

namespace Tomography.UI.windows;

public sealed partial class AppWindow
{
    private async Task ImportExperimentalInputAsync(string role, string? existingPath = null)
    {
        if (_fileOperation || _reconstructing || _sampling) return;
        _fileOperation = true;
        try
        {
            var path = existingPath ?? await PickImportPathAsync(role);
            if (path is null) return;
            _reconstructionSettings ??= new Tomography.UI.SideView.AlgorithmParametersView();
            var python = _reconstructionSettings.PythonDll;
            // 检查后导入不可变副本，再校验副本，确保只提交最终有效输入。
            async Task<string> ValidateAsync(string input)
            {
                return await Task.Run(() =>
                {
                    if (role == "灵敏度矩阵")
                    {
                        var size = ExperimentInputValidator.ValidateSensitivity(input, python);
                        if (_reconstruction.CoordinatesPath is not null && Path.GetExtension(input).ToLowerInvariant() != ".mat")
                            ExperimentInputValidator.ValidateCoordinates(_reconstruction.CoordinatesPath, input);
                        return $"{size.Rows} × {size.Columns}，校验通过";
                    }
                    if (role == "节点坐标") return $"{ExperimentInputValidator.ValidateCoordinates(input, _reconstruction.SensitivityPath)} × 2，校验通过";
                    ExperimentInputValidator.ReadChannels(input); return "64 通道，校验通过";
                });
            }
            await ValidateAsync(path);
            var asset = existingPath is null ? await Task.Run(() => _assets.Import(path)) : new ImportedAsset(Path.GetFileName(path), path);
            var description = await ValidateAsync(asset.Path);
            _reconstruction.UseAsset(asset, role);
            _projectSettings.ExperimentInputs[role] = asset.Path;
            ProjectPanel.AddAsset(asset); DirectoryPanel.SetAsset(role, asset);
            if (role == "灵敏度矩阵") DeviceView.SetMatrix($"{asset.Name} · {description}");
            else if (role == "节点坐标") DeviceView.SetCoordinates($"{asset.Name} · {description}");
            else if (role == "物场/Demo数据") { DeviceView.SetSimulation($"{asset.Name} · {description}"); Workspace.ShowMeasurements(_reconstruction.Measurements!); }
            else DeviceView.SetParameterStatus("空场参考已校验；参数尚未由下位机采样应答确认。");
            UpdateDeviceInputs();
            await SaveDeviceSettingsAsync();
        }
        catch (Exception ex)
        {
            if (!_connectionLifetime.IsCancellationRequested) ShowDeviceError($"导入失败，原有效输入保留：{ex.Message}");
        }
        finally { _fileOperation = false; }
    }
}
