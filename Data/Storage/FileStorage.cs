using Tomography.Data.Interfaces;

namespace Tomography.Data
{
    /// <summary>只存取原始字节，不解释配置、日志或编码。</summary>
    public class FileStorage : IFileStorage
    {
        private readonly SemaphoreSlim _writeLock = new(1, 1);

        public bool Exists(string path) => File.Exists(path);

        public byte[] Read(string path)
        {
            _writeLock.Wait();
            try
            {
                return File.ReadAllBytes(path);
            }
            finally
            {
                _writeLock.Release();
            }
        }

        public void Write(string path, byte[] data)
        {
            _writeLock.Wait();
            try
            {
                EnsureDirectory(path);
                File.WriteAllBytes(path, data);
            }
            finally
            {
                _writeLock.Release();
            }
        }

        public async Task AppendAsync(string path, byte[] data, CancellationToken cancellationToken = default)
        {
            await _writeLock.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                EnsureDirectory(path);
                using var stream = new FileStream(path, FileMode.Append, FileAccess.Write, FileShare.Read, 4096, useAsync: true);
                await stream.WriteAsync(data, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                _writeLock.Release();
            }
        }

        private static void EnsureDirectory(string path)
        {
            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrWhiteSpace(directory)) Directory.CreateDirectory(directory);
        }
    }
}
