using Tomography.Business.Models;
using Tomography.Data;

namespace Tomography.Business.Services;

/// <summary>导入导出文件资产；导入 Python 文件不会自动执行。</summary>
public sealed class AssetService
{
    public static readonly string[] Extensions = { ".csv", ".txt", ".mat", ".png", ".jpg", ".jpeg", ".bmp", ".tif", ".tiff", ".db", ".sqlite", ".sqlite3", ".py" };
    private readonly WorkspaceFileStorage _storage;
    public AssetService(string? directory = null) => _storage = new WorkspaceFileStorage(directory ??
        System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Tomography", "Imports"));
    public ImportedAsset Import(string path)
    {
        if (!Extensions.Contains(System.IO.Path.GetExtension(path), StringComparer.OrdinalIgnoreCase))
            throw new ArgumentException("不支持此文件类型。");
        return new ImportedAsset(System.IO.Path.GetFileName(path), _storage.Import(path));
    }
    public void Export(ImportedAsset asset, string target) => _storage.Export(asset.Path, target);
}
