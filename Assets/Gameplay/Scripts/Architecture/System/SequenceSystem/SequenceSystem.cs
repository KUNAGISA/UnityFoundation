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
        private readonly SequenceContext _context = new SequenceContext();
        private SequenceScriptCache _scriptCache;

        private IntPtr _mainThread;
        private IntPtr _currentThread;
        private SequencePlayer<ISequenceContext> _currentPlayer;
        private GCHandle _currentPlayerHandle;
        private SequenceToken _currentToken = SequenceToken.None;
        private SequencePlayback _currentPlayback;
        private int? _currentOnCompletedActionId;
        private InputModeToken _inputModeToken;
        private int _playbackIdGeneration;

        private IInputService _inputService;
        private TablesModel _tablesModel;
        private LocalizationSystem _localizationSystem;
        private CancellationTokenSource _playbackLoadCancellation;
        private SubscriptionToken _tableReloadedToken;
        private FrameTickToken _frameTickToken;

        protected override void OnInitialize()
        {
            BindContext(_context);
            _inputService = this.GetService<IInputService>();
            _tablesModel = this.GetModel<TablesModel>();
            _localizationSystem = this.GetSystem<LocalizationSystem>();
            _tableReloadedToken = this.SubscribeEvent<TableReloadedEvent<SequenceTable>>(OnSequenceTableReloaded);

            _mainThread = LuaAPI.luaL_newstate();
            LuaAPI.luaopen_table(_mainThread);
            LuaAPI.luaopen_string(_mainThread);
            LuaAPI.luaopen_math(_mainThread);
            LuaAPI.luaopen_utf8(_mainThread);
            SequenceLuaAPI.Register(_mainThread);
            _scriptCache = new SequenceScriptCache(this.GetService<IAssetService>(), _mainThread);
        }

        protected override void OnShutdown()
        {
            _tableReloadedToken.Cancel();
            _tableReloadedToken = default;
            StopCurrentPlayer(SequencePlaybackStatus.Cancelled);
            _scriptCache?.Dispose();
            _scriptCache = null;

            if (_mainThread != IntPtr.Zero)
            {
                LuaAPI.lua_close(_mainThread);
                _mainThread = IntPtr.Zero;
            }
            _frameTickToken.Cancel();
            _frameTickToken = default;
            _inputService = null;
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
            _currentOnCompletedActionId = sequenceEntry.OnCompletedActionId;
            _inputModeToken = _inputService.PushMode(InputModeCatalog.Sequence);
            var playback = _currentPlayback;
            var token = _currentToken;
            var location = sequenceEntry.ScriptLocation;
            var textLocation = sequenceEntry.TextLocation;
            _playbackLoadCancellation = new CancellationTokenSource();

            LoadAndStartPlaybackAsync(token, location, textLocation, _playbackLoadCancellation.Token).Forget();
            return playback;
        }

        public async UniTask PreloadAsync(int sequenceId, CancellationToken cancellationToken = default)
        {
            if (!_tablesModel.SequenceTable.TryGet(sequenceId, out var sequenceEntry))
            {
                throw new KeyNotFoundException($"Sequence [{sequenceId}] was not found.");
            }
            await _scriptCache.PreloadAsync(sequenceEntry.ScriptLocation, cancellationToken);
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
            _scriptCache.Clear();
        }

        private async UniTask LoadAndStartPlaybackAsync(SequenceToken token, string location, string textLocation, CancellationToken cancellationToken)
        {
            try
            {
                var localizationTask = _localizationSystem.SetSequenceTextSourceAsync(textLocation, cancellationToken).Preserve();
                var thread = await _scriptCache.CreateThreadAsync(location, cancellationToken);
                if (!IsPlaying(token))
                {
                    LuaAPI.lua_closethread(thread, _mainThread);
                    return;
                }
                // 等待文本期间也由播放收尾负责关闭协程。
                _currentThread = thread;
                await localizationTask;
                if (!IsPlaying(token))
                {
                    return;
                }
                InitializePlayer(thread, location);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
            }
            catch (Exception exception)
            {
                HandlePlaybackException(token, exception);
            }
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
                var onCompletedActionId = _currentOnCompletedActionId;
                StopCurrentPlayer(SequencePlaybackStatus.Succeeded);
                if (onCompletedActionId.HasValue)
                {
                    // 先释放旧播放，再提交可能启动下一段剧情的行为。
                    this.GetSystem<ActionSystem>().Submit(onCompletedActionId.Value);
                }
            }
        }

        private void InitializePlayer(IntPtr thread, string location)
        {
            LuaAPI.lua_newtable(thread);
            LuaAPI.lua_newtable(thread);
            LuaAPI.lua_pushglobaltable(thread);
            LuaAPI.lua_setfield(thread, -2, "__index");
            LuaAPI.lua_setmetatable(thread, -2);
            LuaAPI.lua_setupvalue(thread, -2, 1);

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
            _currentOnCompletedActionId = null;
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
