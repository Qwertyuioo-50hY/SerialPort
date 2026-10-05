namespace Tomography.UI.Views;

public sealed partial class MainWindow
{
    private async void CheckPythonSyntaxAsync()
    {
        if (_reconstructing || _reconstructionSettings is null) return;
        if (_reconstruction.CustomAlgorithmPath is null) { ConnectionStatus.Text = "请先导入自定义 Python 算法"; return; }
        _reconstructing = true;
        _reconstructionSettings.SetBusy(true);
        try
        {
            await _pythonReconstruction.CheckSyntaxAsync(_reconstruction.CustomAlgorithmPath, _reconstructionSettings.PythonDll);
            if (!_connectionLifetime.IsCancellationRequested) ConnectionStatus.Text = "Python 语法检查通过";
        }
        catch (Exception ex) { if (!_connectionLifetime.IsCancellationRequested) ConnectionStatus.Text = ex.Message; }
        finally { _reconstructing = false; if (!_connectionLifetime.IsCancellationRequested) _reconstructionSettings.SetBusy(false); }
    }
}
