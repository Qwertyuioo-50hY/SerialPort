using System.Text.Json;
using Python.Runtime;
using Tomography.Business.Models;

namespace Tomography.Business.Services;

public sealed partial class PythonReconstructionService
{
    public async Task ExportAcquisitionMatAsync(AcquisitionSnapshot snapshot, string path, string pythonDll)
    {
        await Gate.WaitAsync().ConfigureAwait(false);
        try
        {
            await Task.Run(() =>
            {
                PythonRuntimeHost.Initialize(pythonDll);
                using var gil = Py.GIL();
                using var scope = Py.CreateScope();
                scope.Set("scripts", _scripts); scope.Set("path", path);
                scope.Set("snapshot_json", JsonSerializer.Serialize(snapshot));
                scope.Exec("import sys\nif scripts not in sys.path: sys.path.insert(0, scripts)\nfrom acquisition_export import save\nsave(path, snapshot_json)");
            }).ConfigureAwait(false);
        }
        finally { Gate.Release(); }
    }
}
