namespace Tomography.Business.Models;

public sealed record ImportedAsset(string Name, string Path)
{
    public override string ToString() => Name;
}
