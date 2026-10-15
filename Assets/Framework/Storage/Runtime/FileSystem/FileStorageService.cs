using System;
using System.IO;
using System.Text;
using System.Threading;
using Aoiro;
using Cysharp.Threading.Tasks;

namespace Gameplay.Storage
{
    public sealed class FileStorageService : ServiceBase, IStorageService
    {
        private static readonly UTF8Encoding Utf8NoBom = new UTF8Encoding(false);

        private readonly string _rootPathSource;
        private string _rootPath;

        public FileStorageService(string rootPath)
        {
            if (string.IsNullOrWhiteSpace(rootPath))
            {
                throw new ArgumentException("Storage root path cannot be null or empty.", nameof(rootPath));
            }

            _rootPathSource = rootPath;
        }

        public string RootPath => _rootPath;
        
        protected override void OnInitialize()
        {
            _rootPath = Path.GetFullPath(_rootPathSource);
            Directory.CreateDirectory(_rootPath);
            Directory.CreateDirectory(GetPath("Temp"));
        }

        protected override void OnShutdown()
        {
            _rootPath = null;
        }

        public bool Exists(string relativePath)
        {
            return File.Exists(GetPath(relativePath));
        }

        public bool DirectoryExists(string relativePath)
        {
            return Directory.Exists(GetPath(relativePath));
        }

        public void EnsureDirectory(string relativePath)
        {
            Directory.CreateDirectory(GetPath(relativePath));
        }

        public void Delete(string relativePath)
        {
            var path = GetPath(relativePath);
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }

        public void WriteBytes(string relativePath, byte[] bytes, StorageWriteMode mode = StorageWriteMode.Atomic)
        {
            if (bytes == null)
            {
                throw new ArgumentNullException(nameof(bytes));
            }

            var path = GetPath(relativePath);
            EnsureParentDirectory(path);

            if (mode == StorageWriteMode.Direct)
            {
                File.WriteAllBytes(path, bytes);
                return;
            }

            WriteBytesAtomic(path, bytes);
        }

        public byte[] ReadBytes(string relativePath)
        {
            return File.ReadAllBytes(GetPath(relativePath));
        }

        public byte[] ReadBytesRange(string relativePath, long offset, int count)
        {
            return ReadBytesRange(relativePath, offset, count, default);
        }

        public void WriteText(string relativePath, string text, StorageWriteMode mode = StorageWriteMode.Atomic)
        {
            WriteBytes(relativePath, Utf8NoBom.GetBytes(text ?? string.Empty), mode);
        }

        public string ReadText(string relativePath)
        {
            return Utf8NoBom.GetString(ReadBytes(relativePath));
        }

        public async UniTask WriteBytesAsync(string relativePath, byte[] bytes, StorageWriteMode mode = StorageWriteMode.Atomic, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await UniTask.RunOnThreadPool(() =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                WriteBytes(relativePath, bytes, mode);
            }, cancellationToken: cancellationToken);
        }

        public async UniTask<byte[]> ReadBytesAsync(string relativePath, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return await UniTask.RunOnThreadPool(() =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                return ReadBytes(relativePath);
            }, cancellationToken: cancellationToken);
        }

        public async UniTask<byte[]> ReadBytesRangeAsync(string relativePath, long offset, int count, CancellationToken cancellationToken = default)
        {
            ValidateRange(offset, count);
            cancellationToken.ThrowIfCancellationRequested();
            return await UniTask.RunOnThreadPool(() => ReadBytesRange(relativePath, offset, count, cancellationToken), cancellationToken: cancellationToken);
        }

        public async UniTask WriteTextAsync(string relativePath, string text, StorageWriteMode mode = StorageWriteMode.Atomic, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await UniTask.RunOnThreadPool(() =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                WriteText(relativePath, text, mode);
            }, cancellationToken: cancellationToken);
        }

        public async UniTask<string> ReadTextAsync(string relativePath, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return await UniTask.RunOnThreadPool(() =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                return ReadText(relativePath);
            }, cancellationToken: cancellationToken);
        }

        private string GetPath(string relativePath)
        {
            if (_rootPath == null)
            {
                throw new InvalidOperationException($"{nameof(FileStorageService)} has not been initialized.");
            }

            if (string.IsNullOrWhiteSpace(relativePath))
            {
                return _rootPath;
            }

            if (Path.IsPathRooted(relativePath))
            {
                throw new ArgumentException("Storage path must be relative.", nameof(relativePath));
            }

            var parts = relativePath.Split(new[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries);
            for (var i = 0; i < parts.Length; i++)
            {
                if (parts[i] == "." || parts[i] == "..")
                {
                    throw new ArgumentException("Storage path cannot contain '.' or '..'.", nameof(relativePath));
                }
            }

            var path = Path.GetFullPath(Path.Combine(_rootPath, relativePath));
            var root = _rootPath.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal)
                ? _rootPath
                : _rootPath + Path.DirectorySeparatorChar;

            if (!path.Equals(_rootPath, StringComparison.OrdinalIgnoreCase) && !path.StartsWith(root, StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException("Storage path is outside of the root path.", nameof(relativePath));
            }

            return path;
        }

        private byte[] ReadBytesRange(string relativePath, long offset, int count, CancellationToken cancellationToken)
        {
            ValidateRange(offset, count);
            cancellationToken.ThrowIfCancellationRequested();
            if (count == 0)
            {
                return Array.Empty<byte>();
            }

            using var stream = new FileStream(GetPath(relativePath), FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
            if (offset >= stream.Length)
            {
                return Array.Empty<byte>();
            }

            stream.Seek(offset, SeekOrigin.Begin);
            var length = (int)Math.Min(count, stream.Length - offset);
            var bytes = new byte[length];
            var read = 0;
            while (read < length)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var current = stream.Read(bytes, read, length - read);
                if (current == 0)
                {
                    break;
                }
                read += current;
            }

            if (read == length)
            {
                return bytes;
            }

            Array.Resize(ref bytes, read);
            return bytes;
        }

        private static void ValidateRange(long offset, int count)
        {
            if (offset < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(offset), "Storage read offset cannot be negative.");
            }
            if (count < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(count), "Storage read count cannot be negative.");
            }
        }

        private static void EnsureParentDirectory(string path)
        {
            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }
        }

        private void WriteBytesAtomic(string path, byte[] bytes)
        {
            var tempDirectory = GetPath("Temp");
            Directory.CreateDirectory(tempDirectory);

            var tempPath = Path.Combine(tempDirectory, Guid.NewGuid().ToString("N") + ".tmp");
            File.WriteAllBytes(tempPath, bytes);
            
            if (!File.Exists(path))
            {
                File.Move(tempPath, path);
                return;
            }

            try
            {
                File.Replace(tempPath, path, null);
            }
            catch (PlatformNotSupportedException)
            {
                File.Delete(path);
                File.Move(tempPath, path);
            }
            catch
            {
                if (File.Exists(tempPath))
                {
                    File.Delete(tempPath);
                }

                throw;
            }
        }
    }
}
