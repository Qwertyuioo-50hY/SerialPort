using Microsoft.UI.Xaml.Controls;
using Tomography.UI.TittleBar;

namespace Tomography.UI.windows;

public sealed partial class AppWindow
{
    private async void TitleBarActionRequested(object? sender, TitleBarAction action)
    {
        switch (action)
        {
            case TitleBarAction.ToggleParameters: ViewModel.ParametersVisible = !ViewModel.ParametersVisible; break;
            case TitleBarAction.ConnectDevice: ToggleDeviceConnection(); break;
            case TitleBarAction.ImportAlgorithm: await ImportFileAsync("自定义算法"); break;
            case TitleBarAction.ImportData: await ImportFileAsync("数据"); break;
            case TitleBarAction.ImportSensitivity: await ImportFileAsync("灵敏度矩阵"); break;
            case TitleBarAction.ToggleRealtimeReconstruction: SetRealtimeReconstruction(!_realtimeEnabled); break;
            case TitleBarAction.CheckPythonSyntax: CheckPythonSyntaxAsync(); break;
            case TitleBarAction.RunReconstruction: ReconstructAsync(); break;
            case TitleBarAction.ExportFile: await ExportFileAsync(); break;
            case TitleBarAction.ExportAcquisitionCsv: await ExportAcquisitionAsync(false); break;
            case TitleBarAction.ExportAcquisitionMat: await ExportAcquisitionAsync(true); break;
            case TitleBarAction.ExportMatrix: await ExportReconstructionAsync(false); break;
            case TitleBarAction.ExportImage: await ExportReconstructionAsync(true); break;
            case TitleBarAction.OpenChat: ShowBottomPanel(true); break;
            case TitleBarAction.OpenConsole: ShowBottomPanel(false); break;
            case TitleBarAction.AddCommand: await AddCustomCommandAsync(); break;
            case TitleBarAction.ConfigureApi: await ConfigureAsync("自定义API"); break;
            case TitleBarAction.ConfigureAlgorithm:
                ViewModel.Section = "算法";
                ViewModel.ParametersVisible = true;
                break;
            case TitleBarAction.ConfigureWaveform: await ConfigureAsync("波形图像"); break;
            default: HandleViewAction(action); break;
            case TitleBarAction.Help:
                var dialog = new ContentDialog
                {
                    XamlRoot = Root.XamlRoot,
                    RequestedTheme = Root.ActualTheme,
                    Title = "快捷键",
                    Content = "Ctrl+M：显示 / 隐藏顶部菜单\nAlt+F / V / I / C / H：文件 / 视图 / 指令 / 自定义 / 帮助\nCtrl+B：显示 / 隐藏参数面板\n拖动面板右侧边缘或按左右方向键：调整宽度\nAlt：显示顶部菜单访问键",
                    CloseButtonText = "关闭"
                };
                await dialog.ShowAsync();
                break;
        }
    }

}
