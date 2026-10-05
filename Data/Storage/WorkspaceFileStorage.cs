namespace Tomography.Data;

/// <summary>导入文件保留独立副本，导出不改变原文件。</summary>
public sealed class WorkspaceFileStorage
{
    private readonly string _directory;
    public WorkspaceFileStorage(string directory) => _directory = directory;
    public string Import(string source)
    {
        var directory = Path.Combine(_directory, Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        var destination = Path.Combine(directory, Path.GetFileName(source));
        File.Copy(source, destination);
        return destination;
    }
    public void Export(string source, string destination)
    {
        if (Path.GetFullPath(source).Equals(Path.GetFullPath(destination), StringComparison.OrdinalIgnoreCase)) return;
        File.Copy(source, destination, overwrite: true);
    }
}
