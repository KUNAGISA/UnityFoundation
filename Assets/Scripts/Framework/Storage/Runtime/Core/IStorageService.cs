using System.Threading;
using Aoiro;
using Cysharp.Threading.Tasks;

namespace Gameplay.Storage
{
    public interface IStorageService : IService
    {
        string RootPath { get; }

        bool Exists(string relativePath);
        bool DirectoryExists(string relativePath);
        void EnsureDirectory(string relativePath);
        void Delete(string relativePath);

        void WriteBytes(string relativePath, byte[] bytes, StorageWriteMode mode = StorageWriteMode.Atomic);
        byte[] ReadBytes(string relativePath);
        byte[] ReadBytesRange(string relativePath, long offset, int count);

        void WriteText(string relativePath, string text, StorageWriteMode mode = StorageWriteMode.Atomic);
        string ReadText(string relativePath);

        UniTask WriteBytesAsync(string relativePath, byte[] bytes, StorageWriteMode mode = StorageWriteMode.Atomic, CancellationToken cancellationToken = default);
        UniTask<byte[]> ReadBytesAsync(string relativePath, CancellationToken cancellationToken = default);
        UniTask<byte[]> ReadBytesRangeAsync(string relativePath, long offset, int count, CancellationToken cancellationToken = default);

        UniTask WriteTextAsync(string relativePath, string text, StorageWriteMode mode = StorageWriteMode.Atomic, CancellationToken cancellationToken = default);
        UniTask<string> ReadTextAsync(string relativePath, CancellationToken cancellationToken = default);
    }
}
