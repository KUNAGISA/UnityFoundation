using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Gameplay.Asset;
using LiteLua;
using UnityEngine;

namespace Gameplay.Sequence
{
    public sealed class SequenceScriptCache : IDisposable
    {
        private sealed class CacheEntry
        {
            public int ChunkReference = LuaConst.NoRef;
            public readonly UniTaskCompletionSource<int> CompletionSource = new UniTaskCompletionSource<int>();
        }

        private readonly IAssetService _assetService;
        private readonly IntPtr _mainThread;
        private readonly Dictionary<string, CacheEntry> _entries = new Dictionary<string, CacheEntry>();
        private CancellationTokenSource _cacheLifetime = new CancellationTokenSource();
        private long _generation;

        public SequenceScriptCache(IAssetService assetService, IntPtr mainThread)
        {
            _assetService = assetService;
            _mainThread = mainThread;
        }

        public async UniTask PreloadAsync(string location, CancellationToken cancellationToken = default)
        {
            if (!_entries.TryGetValue(location, out var entry))
            {
                entry = new CacheEntry();
                _entries.Add(location, entry);
                PreloadScriptAsync(entry, location, _generation, _cacheLifetime.Token).Forget();
            }
            // 调用方只取消自身等待，共享加载由缓存作用域管理。
            await entry.CompletionSource.Task.AttachExternalCancellation(cancellationToken);
        }

        public async UniTask<IntPtr> CreateThreadAsync(string location, CancellationToken cancellationToken = default)
        {
            var cacheLifetimeToken = _cacheLifetime.Token;
            if (_entries.TryGetValue(location, out var entry))
            {
                var chunkReference = await entry.CompletionSource.Task.AttachExternalCancellation(cancellationToken);
                cacheLifetimeToken.ThrowIfCancellationRequested();
                return CreateThread(default, location, chunkReference);
            }

            using var linkedCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, cacheLifetimeToken);
            var operationToken = linkedCancellation.Token;
            IAssetAsyncOperation<TextAsset> operation = null;
            try
            {
                operation = _assetService.LoadAssetAsync<TextAsset>("Sequences/" + location);
                var textAsset = await operation.ToUniTask(cancellationToken: operationToken);
                if (textAsset == null)
                {
                    throw new InvalidOperationException($"Sequence script [{location}] returned null.");
                }

                // 协程持有编译后的函数，返回前即可释放源资源；普通播放不写入缓存。
                return CreateThread(textAsset.bytes, location);
            }
            finally
            {
                operation?.Release();
            }
        }

        public void Clear()
        {
            ClearCache(true);
        }

        public void Dispose()
        {
            if (_cacheLifetime != null)
            {
                ClearCache(false);
            }
        }

        private async UniTask PreloadScriptAsync(CacheEntry entry, string location, long generation, CancellationToken cancellationToken)
        {
            IAssetAsyncOperation<TextAsset> operation = null;
            try
            {
                operation = _assetService.LoadAssetAsync<TextAsset>("Sequences/" + location);
                var textAsset = await operation.ToUniTask(cancellationToken: cancellationToken);
                if (generation != _generation)
                {
                    throw new OperationCanceledException(cancellationToken);
                }
                if (textAsset == null)
                {
                    throw new InvalidOperationException($"Sequence script [{location}] returned null.");
                }

                entry.ChunkReference = CompileScript(textAsset.bytes, location);
                entry.CompletionSource.TrySetResult(entry.ChunkReference);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                RemoveEntry(location, entry);
                entry.CompletionSource.TrySetCanceled(cancellationToken);
            }
            catch (Exception exception)
            {
                RemoveEntry(location, entry);
                entry.CompletionSource.TrySetException(exception);
            }
            finally
            {
                operation?.Release();
            }
        }

        private int CompileScript(ReadOnlySpan<byte> bytes, string location)
        {
            if (LuaAPI.luaL_loadbuffer(_mainThread, bytes, location) == LuaStatus.Ok)
            {
                return LuaAPI.luaL_ref(_mainThread);
            }

            var error = LuaAPI.lua_tostring(_mainThread, -1);
            LuaAPI.lua_pop(_mainThread, 1);
            throw new Exception(error);
        }

        private IntPtr CreateThread(ReadOnlySpan<byte> bytes, string location, int chunkReference = LuaConst.NoRef)
        {
            var thread = LuaAPI.lua_newthread(_mainThread);
            try
            {
                if (chunkReference != LuaConst.NoRef)
                {
                    LuaAPI.luaL_getref(_mainThread, chunkReference);
                    LuaAPI.lua_xmove(_mainThread, thread, 1);
                }
                else if (LuaAPI.luaL_loadbuffer(thread, bytes, location) != LuaStatus.Ok)
                {
                    throw new Exception(LuaAPI.lua_tostring(thread, -1));
                }
                return thread;
            }
            catch
            {
                LuaAPI.lua_closethread(thread, _mainThread);
                throw;
            }
        }

        private void RemoveEntry(string location, CacheEntry entry)
        {
            if (_entries.TryGetValue(location, out var current) && ReferenceEquals(current, entry))
            {
                _entries.Remove(location);
            }
        }

        private void ClearCache(bool recreateLifetime)
        {
            ++_generation;
            _cacheLifetime.Cancel();
            _cacheLifetime.Dispose();
            _cacheLifetime = recreateLifetime ? new CancellationTokenSource() : null;
            foreach (var entry in _entries.Values)
            {
                if (entry.ChunkReference != LuaConst.NoRef)
                {
                    LuaAPI.luaL_unref(_mainThread, entry.ChunkReference);
                }
            }
            _entries.Clear();
        }
    }
}
