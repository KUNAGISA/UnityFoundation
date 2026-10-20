using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using Aoiro;
using Aoiro.Sequence;
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
            StopCurrentPlayback(SequencePlaybackStatus.Cancelled);
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
            StopCurrentPlayback(SequencePlaybackStatus.Cancelled);
            if (!_tablesModel.SequenceTable.TryGet(sequenceId, out var sequenceEntry))
            {
                Debug.LogError($"[Sequence] 找不到演出配置 [{sequenceId}]");
                return SequencePlayback.Failed;
            }

            var token = CreateToken();
            _currentToken = token;
            _currentPlayback = new SequencePlayback(this, _currentToken);
            _currentOnCompletedActionId = sequenceEntry.OnCompletedActionId;
            _inputModeToken = _inputService.PushMode(InputModeCatalog.Sequence);
            var playback = _currentPlayback;
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
            StopCurrentPlayback(SequencePlaybackStatus.Cancelled);
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
            StopCurrentPlayback(SequencePlaybackStatus.Cancelled);
            _scriptCache.Clear();
        }

        private async UniTask LoadAndStartPlaybackAsync(SequenceToken token, string location, string textLocation, CancellationToken cancellationToken)
        {
            try
            {
                var localizationTask = _localizationSystem.SetSequenceTextSourceAsync(textLocation, cancellationToken);
                var scriptTask = PreparePlaybackThreadAsync(token, location, cancellationToken);
                // 两个任务均由 WhenAll 观察；任一失败后收尾会取消另一个任务。
                await UniTask.WhenAll(localizationTask, scriptTask);
                if (!IsPlaying(token))
                {
                    return;
                }
                StartPlayer(_currentThread, location);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
            }
            catch (Exception exception)
            {
                HandlePlaybackException(token, exception);
            }
        }

        private async UniTask PreparePlaybackThreadAsync(SequenceToken token, string location, CancellationToken cancellationToken)
        {
            var mainThread = _mainThread;
            var thread = await _scriptCache.CreateThreadAsync(location, cancellationToken);
            if (!IsPlaying(token))
            {
                SequenceLuaAPI.CloseThread(thread, mainThread);
                return;
            }
            // 等待文本期间也由播放收尾负责关闭协程。
            _currentThread = thread;
        }

        private void OnSequenceTableReloaded(TableReloadedEvent<SequenceTable> _)
        {
            ClearSceneCache();
        }

        private void OnProcedureFrameTick(in FrameTime time)
        {
            var player = _currentPlayer;
            var token = _currentToken;
            if (player == null)
            {
                return;
            }
            try
            {
                player.Tick(time.DeltaTime);
            }
            catch (Exception exception)
            {
                HandlePlaybackException(token, exception);
                return;
            }
            if (IsPlaying(token) && player.IsCompleted)
            {
                Debug.Log("[Sequence] 剧情脚本播放结束");
                var onCompletedActionId = _currentOnCompletedActionId;
                StopCurrentPlayback(SequencePlaybackStatus.Succeeded);
                if (onCompletedActionId.HasValue)
                {
                    // 先释放旧播放，再提交可能启动下一段剧情的行为。
                    this.GetSystem<ActionSystem>().Submit(onCompletedActionId.Value);
                }
            }
        }

        private void StartPlayer(IntPtr thread, string location)
        {
            LuaAPI.lua_newtable(thread);
            LuaAPI.lua_newtable(thread);
            LuaAPI.lua_pushglobaltable(thread);
            LuaAPI.lua_setfield(thread, -2, "__index");
            LuaAPI.lua_setmetatable(thread, -2);
            LuaAPI.lua_setupvalue(thread, -2, 1);

            var routine = new LuaSequenceRoutine(_mainThread, thread);
            _currentPlayer = new SequencePlayer<ISequenceContext>(_context, routine, builder =>
            {
                builder.Register(new AdvSession());
            });
            _currentPlayerHandle = GCHandle.Alloc(_currentPlayer, GCHandleType.Normal);
            SequenceLuaAPI.BindPlayer(thread, GCHandle.ToIntPtr(_currentPlayerHandle));
            _currentPlayer.Start();
            Debug.Log($"[Sequence] 创建脚本 [{location}] 成功");
        }

        private void HandlePlaybackException(SequenceToken token, Exception exception)
        {
            Debug.LogException(exception);
            if (IsPlaying(token))
            {
                StopCurrentPlayback(SequencePlaybackStatus.Failed);
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

        private void StopCurrentPlayback(SequencePlaybackStatus status)
        {
            var playback = _currentPlayback;
            var player = _currentPlayer;
            var thread = _currentThread;
            var mainThread = _mainThread;
            var playerHandle = _currentPlayerHandle;
            var inputModeToken = _inputModeToken;
            var loadCancellation = _playbackLoadCancellation;
            var localizationSystem = _localizationSystem;

            // 先移交到局部变量，使取消回调和旧异步任务无法再接管当前播放。
            _currentPlayback = null;
            _currentPlayer = null;
            _currentThread = IntPtr.Zero;
            _currentPlayerHandle = default;
            _inputModeToken = default;
            _playbackLoadCancellation = null;
            _currentToken = SequenceToken.None;
            _currentOnCompletedActionId = null;

            inputModeToken.Cancel();
            loadCancellation?.Cancel();
            loadCancellation?.Dispose();
            player?.Stop();
            if (thread != IntPtr.Zero)
            {
                SequenceLuaAPI.CloseThread(thread, mainThread);
            }
            if (playerHandle.IsAllocated)
            {
                playerHandle.Free();
            }
            if (_currentPlayback == null)
            {
                localizationSystem?.ClearSequenceTextSource();
            }
            playback?.Complete(status);
        }
    }
}
