using Aoiro.Sequence;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using Aoiro;
using Cysharp.Threading.Tasks;
using Gameplay.Asset;
using Gameplay.Scheduler;
using Gameplay.Sequence;
using Gameplay.Tables;
using LiteLua;
using UnityEngine;

namespace Gameplay
{
    public class SequenceSystem : ProcedureCapacitySystem, ISystemAccessor, ISceneBeforeLeave
    {
        private sealed class ScriptCacheEntry
        {
            public int ChunkReference = LuaConst.NoRef;
            public readonly UniTaskCompletionSource<int> CompletionSource = new UniTaskCompletionSource<int>();
        }

        private readonly SequenceContext _context = new SequenceContext();
        private readonly Dictionary<string, ScriptCacheEntry> _scriptCache = new Dictionary<string, ScriptCacheEntry>();

        private IntPtr _mainThread;
        private IntPtr _currentThread;
        private SequencePlayer<ISequenceContext> _currentPlayer;
        private GCHandle _currentPlayerHandle;
        private SequenceToken _currentToken = SequenceToken.None;
        private SequencePlayback _currentPlayback;
        private InputModeToken _inputModeToken;
        private int _playbackIdGeneration;
        private long _cacheGeneration;

        private IInputService _inputService;
        private IAssetService _assetService;
        private TablesModel _tablesModel;
        private LocalizationSystem _localizationSystem;
        private CancellationTokenSource _cacheLifetime;
        private CancellationTokenSource _playbackLoadCancellation;
        private SubscriptionToken _tableReloadedToken;
        private FrameTickToken _frameTickToken;

        protected override void OnInitialize()
        {
            BindContext(_context);
            _inputService = this.GetService<IInputService>();
            _assetService = this.GetService<IAssetService>();
            _tablesModel = this.GetModel<TablesModel>();
            _localizationSystem = this.GetSystem<LocalizationSystem>();
            _cacheLifetime = new CancellationTokenSource();
            _tableReloadedToken = this.SubscribeEvent<TableReloadedEvent<SequenceTable>>(OnSequenceTableReloaded);

            _mainThread = LuaAPI.luaL_newstate();
            LuaAPI.luaopen_table(_mainThread);
            LuaAPI.luaopen_string(_mainThread);
            LuaAPI.luaopen_math(_mainThread);
            LuaAPI.luaopen_utf8(_mainThread);
            SequenceLuaAPI.Register(_mainThread);
        }

        protected override void OnShutdown()
        {
            _tableReloadedToken.Cancel();
            _tableReloadedToken = default;
            StopCurrentPlayer(SequencePlaybackStatus.Cancelled);
            ClearCache(false);

            if (_mainThread != IntPtr.Zero)
            {
                LuaAPI.lua_close(_mainThread);
                _mainThread = IntPtr.Zero;
            }
            _frameTickToken.Cancel();
            _frameTickToken = default;
            _inputService = null;
            _assetService = null;
            _tablesModel = null;
            _localizationSystem = null;
        }

        public SequencePlayback Play(int sequenceId)
        {
            StopCurrentPlayer(SequencePlaybackStatus.Cancelled);
            if (!_tablesModel.SequenceTable.TryGet(sequenceId, out var sequenceEntry))
            {
                Debug.LogError($"[Sequence] 找不到演出配置 [{sequenceId}]");
                return SequencePlayback.Failed;
            }

            _currentToken = CreateToken();
            _currentPlayback = new SequencePlayback(this, _currentToken);
            _inputModeToken = _inputService.PushMode(InputModeCatalog.Sequence);
            var playback = _currentPlayback;
            var token = _currentToken;
            var location = sequenceEntry.ScriptLocation;
            var textLocation = sequenceEntry.TextLocation;
            _playbackLoadCancellation = new CancellationTokenSource();

            if (!_scriptCache.TryGetValue(location, out var cacheEntry) || cacheEntry.ChunkReference == LuaConst.NoRef)
            {
                if (cacheEntry == null)
                {
                    LoadAndStartUncachedPlaybackAsync(token, location, textLocation, _playbackLoadCancellation.Token).Forget();
                }
                else
                {
                    WaitForPreloadAndStartPlaybackAsync(token, location, textLocation, cacheEntry, _playbackLoadCancellation.Token).Forget();
                }
                return playback;
            }

            LoadTextAndStartCachedPlaybackAsync(token, location, textLocation, cacheEntry.ChunkReference, _playbackLoadCancellation.Token).Forget();
            return playback;
        }

        public async UniTask PreloadAsync(int sequenceId, CancellationToken cancellationToken = default)
        {
            if (!_tablesModel.SequenceTable.TryGet(sequenceId, out var sequenceEntry))
            {
                throw new KeyNotFoundException($"Sequence [{sequenceId}] was not found.");
            }
            var location = sequenceEntry.ScriptLocation;
            if (!_scriptCache.TryGetValue(location, out var cacheEntry))
            {
                cacheEntry = new ScriptCacheEntry();
                var generation = _cacheGeneration;
                _scriptCache.Add(location, cacheEntry);
                PreloadScriptAsync(cacheEntry, location, generation, _cacheLifetime.Token).Forget();
            }
            await cacheEntry.CompletionSource.Task.AttachExternalCancellation(cancellationToken);
        }

        public void Cancel(SequenceToken token)
        {
            if (IsPlaying(token))
            {
                CancelCurrent();
            }
        }

        public bool IsPlaying(SequenceToken token)
        {
            return token.IsValid && _currentPlayback != null && token.Equals(_currentToken);
        }

        public void CancelCurrent()
        {
            if (_currentPlayback == null)
            {
                return;
            }
            StopCurrentPlayer(SequencePlaybackStatus.Cancelled);
            Debug.Log("[Sequence] 中断剧情脚本");
        }

        UniTask ISceneBeforeLeave.BeforeLeaveSceneAsync(CancellationToken cancellationToken)
        {
            ClearSceneCache();
            return UniTask.CompletedTask;
        }

        protected override void OnProcedureAttach(IProcedureContext context)
        {
            _frameTickToken = context.AddFrameTickTask(OnProcedureFrameTick, ProcedureTiming.FrameTick);
        }

        protected override UniTask OnProcedureTeardownAsync(IProcedureContext context)
        {
            _frameTickToken.Cancel();
            _frameTickToken = default;
            ClearSceneCache();
            return UniTask.CompletedTask;
        }

        private void ClearSceneCache()
        {
            StopCurrentPlayer(SequencePlaybackStatus.Cancelled);
            ClearCache(true);
        }

        private async UniTask WaitForPreloadAndStartPlaybackAsync(SequenceToken token, string location, string textLocation, ScriptCacheEntry cacheEntry, CancellationToken cancellationToken)
        {
            try
            {
                var localizationTask = _localizationSystem.SetSequenceTextSourceAsync(textLocation, cancellationToken).Preserve();
                var chunkReference = await cacheEntry.CompletionSource.Task.AttachExternalCancellation(cancellationToken);
                await localizationTask;
                if (!IsPlaying(token))
                {
                    return;
                }
                CreatePlayerFromReference(chunkReference, location);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
            }
            catch (Exception exception)
            {
                HandlePlaybackException(token, exception);
            }
        }

        private async UniTask LoadAndStartUncachedPlaybackAsync(SequenceToken token, string location, string textLocation, CancellationToken cancellationToken)
        {
            IAssetAsyncOperation<TextAsset> operation = null;
            try
            {
                var localizationTask = _localizationSystem.SetSequenceTextSourceAsync(textLocation, cancellationToken).Preserve();
                operation = _assetService.LoadAssetAsync<TextAsset>("Sequences/" + location);
                var textAsset = await operation.ToUniTask(cancellationToken: cancellationToken);
                if (textAsset == null)
                {
                    throw new InvalidOperationException($"Sequence script [{location}] returned null.");
                }
                await localizationTask;
                cancellationToken.ThrowIfCancellationRequested();
                if (!IsPlaying(token))
                {
                    return;
                }
                await UniTask.SwitchToMainThread(cancellationToken);
                if (!IsPlaying(token))
                {
                    return;
                }
                CreatePlayerFromBytes(textAsset.bytes, location);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
            }
            catch (Exception exception)
            {
                HandlePlaybackException(token, exception);
            }
            finally
            {
                operation?.Release();
            }
        }

        private async UniTask LoadTextAndStartCachedPlaybackAsync(SequenceToken token, string location, string textLocation, int chunkReference, CancellationToken cancellationToken)
        {
            try
            {
                await _localizationSystem.SetSequenceTextSourceAsync(textLocation, cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();
                if (!IsPlaying(token))
                {
                    return;
                }
                CreatePlayerFromReference(chunkReference, location);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
            }
            catch (Exception exception)
            {
                HandlePlaybackException(token, exception);
            }
        }

        private async UniTask PreloadScriptAsync(ScriptCacheEntry cacheEntry, string location, long generation, CancellationToken cancellationToken)
        {
            IAssetAsyncOperation<TextAsset> operation = null;
            try
            {
                operation = _assetService.LoadAssetAsync<TextAsset>("Sequences/" + location);
                var textAsset = await operation.ToUniTask(cancellationToken: cancellationToken);
                if (textAsset == null)
                {
                    throw new InvalidOperationException($"Sequence script [{location}] returned null.");
                }
                cancellationToken.ThrowIfCancellationRequested();
                if (generation != _cacheGeneration)
                {
                    throw new OperationCanceledException(cancellationToken);
                }
                await UniTask.SwitchToMainThread(cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();
                if (generation != _cacheGeneration)
                {
                    throw new OperationCanceledException(cancellationToken);
                }
                var chunkReference = CompileScript(textAsset.bytes, location);
                cacheEntry.ChunkReference = chunkReference;
                cacheEntry.CompletionSource.TrySetResult(chunkReference);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                RemoveCacheEntry(location, cacheEntry);
                cacheEntry.CompletionSource.TrySetCanceled(cancellationToken);
            }
            catch (Exception exception)
            {
                RemoveCacheEntry(location, cacheEntry);
                cacheEntry.CompletionSource.TrySetException(exception);
            }
            finally
            {
                operation?.Release();
            }
        }

        private void RemoveCacheEntry(string location, ScriptCacheEntry cacheEntry)
        {
            if (_scriptCache.TryGetValue(location, out var current) && ReferenceEquals(current, cacheEntry))
            {
                _scriptCache.Remove(location);
            }
        }

        private void ClearCache(bool recreateLifetime)
        {
            ++_cacheGeneration;
            _cacheLifetime?.Cancel();
            _cacheLifetime?.Dispose();
            _cacheLifetime = recreateLifetime ? new CancellationTokenSource() : null;
            foreach (var entry in _scriptCache.Values)
            {
                if (entry.ChunkReference != LuaConst.NoRef)
                {
                    LuaAPI.luaL_unref(_mainThread, entry.ChunkReference);
                }
            }
            _scriptCache.Clear();
        }

        private void OnSequenceTableReloaded(TableReloadedEvent<SequenceTable> _)
        {
            ClearSceneCache();
        }

        private void OnProcedureFrameTick(in FrameTime time)
        {
            if (_currentPlayer == null)
            {
                return;
            }
            try
            {
                _currentPlayer.Tick(time.DeltaTime);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                StopCurrentPlayer(SequencePlaybackStatus.Failed);
                return;
            }
            if (_currentPlayer.IsCompleted)
            {
                Debug.Log("[Sequence] 剧情脚本播放结束");
                StopCurrentPlayer(SequencePlaybackStatus.Succeeded);
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

        private void CreatePlayerFromReference(int chunkReference, string location)
        {
            var thread = LuaAPI.lua_newthread(_mainThread);
            LuaAPI.luaL_getref(_mainThread, chunkReference);
            LuaAPI.lua_xmove(_mainThread, thread, 1);
            InitializePlayer(thread, location);
        }

        private void CreatePlayerFromBytes(ReadOnlySpan<byte> bytes, string location)
        {
            var thread = LuaAPI.lua_newthread(_mainThread);
            if (LuaAPI.luaL_loadbuffer(thread, bytes, location) != LuaStatus.Ok)
            {
                var error = LuaAPI.lua_tostring(thread, -1);
                LuaAPI.lua_closethread(thread, _mainThread);
                throw new Exception(error);
            }
            InitializePlayer(thread, location);
        }

        private void InitializePlayer(IntPtr thread, string location)
        {
            LuaAPI.lua_newtable(thread);
            LuaAPI.lua_newtable(thread);
            LuaAPI.lua_pushglobaltable(thread);
            LuaAPI.lua_setfield(thread, -2, "__index");
            LuaAPI.lua_setmetatable(thread, -2);
            LuaAPI.lua_setupvalue(thread, -2, 1);

            _currentThread = thread;
            var routine = new LuaSequenceRoutine(_mainThread, _currentThread);
            _currentPlayer = new SequencePlayer<Sequence.ISequenceContext>(_context, routine, builder =>
            {
                builder.Register(new AdvSession());
            });
            _currentPlayerHandle = GCHandle.Alloc(_currentPlayer, GCHandleType.Normal);
            SequenceLuaAPI.SetCurrentPlayer(_currentThread, GCHandle.ToIntPtr(_currentPlayerHandle));
            _currentPlayer.Start();
            Debug.Log($"[Sequence] 创建脚本 [{location}] 成功");
        }

        private void HandlePlaybackException(SequenceToken token, Exception exception)
        {
            Debug.LogException(exception);
            if (IsPlaying(token))
            {
                StopCurrentPlayer(SequencePlaybackStatus.Failed);
            }
        }

        private SequenceToken CreateToken()
        {
            do
            {
                _playbackIdGeneration = unchecked(_playbackIdGeneration + 1);
            }
            while (_playbackIdGeneration == 0);
            return new SequenceToken(_playbackIdGeneration);
        }

        private void StopCurrentPlayer(SequencePlaybackStatus status)
        {
            var inputModeToken = _inputModeToken;
            _inputModeToken = default;
            inputModeToken.Cancel();

            _playbackLoadCancellation?.Cancel();
            _playbackLoadCancellation?.Dispose();
            _playbackLoadCancellation = null;
            var playback = _currentPlayback;
            _currentPlayback = null;

            if (_currentPlayer != null)
            {
                _currentPlayer.Stop();
                _currentPlayer = null;
            }
            if (_currentThread != IntPtr.Zero)
            {
                LuaAPI.lua_closethread(_currentThread, _mainThread);
                _currentThread = IntPtr.Zero;
            }
            if (_currentPlayerHandle.IsAllocated)
            {
                _currentPlayerHandle.Free();
            }

            _currentToken = SequenceToken.None;
            playback?.Complete(status);
            _localizationSystem?.ClearSequenceTextSource();
        }
    }
}
