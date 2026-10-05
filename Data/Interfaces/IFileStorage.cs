namespace Tomography.Data.Interfaces
{
    public interface IFileStorage
    {
        bool Exists(string path);
        byte[] Read(string path);
        void Write(string path, byte[] data);
        Task AppendAsync(string path, byte[] data, CancellationToken cancellationToken = default);
    }
}
