using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Aoiro;
using Gameplay.Asset;
using Gameplay.Common;
using Gameplay.Scheduler;
using Gameplay.UI;
using UnityEngine;

namespace Gameplay
{
    public sealed class ProcedureSystem : SystemBase, ISystemAccessor, IFrameTickable, IPreShutdownModule
    {
        private readonly GameTimeSource _timeSource = new GameTimeSource();
        private readonly long[] _pauseTokenIds = new long[sizeof(byte) * 8];

        private IProcedure _current;
        private ProcedureContext _context;
        private TransitionProcedureRequest? _transitionProcedureRequest;
        private ProcedurePauseReason _activePauseReason = ProcedurePauseReason.None;
        private ProcedurePauseReason _pendingPauseReason;
        private long _nextPauseTokenId;
        private CancellationTokenSource _lifetimeCancellationSource;
        private InputModeToken _pauseInputModeCancelToken;
        private UISystem _uiSystem;
        private IAssetService _assetService;

        public IProcedureContext Context => _context;
        public ProcedureState State { get; private set; } = ProcedureState.Idle;
        public ITimeSource Time => _timeSource;

        protected override void OnInitialize()
        {
            _lifetimeCancellationSource = new CancellationTokenSource();
            _uiSystem = this.GetSystem<UISystem>();
            _assetService = this.GetService<IAssetService>();
            State = ProcedureState.Idle;
        }

        protected override void OnShutdown()
        {
            StopTransitions();
            _lifetimeCancellationSource?.Dispose();
            _lifetimeCancellationSource = null;
            DestroyCurrentProcedure();
            _timeSource.Reset();
            _uiSystem = null;
            _assetService = null;
        }

        public void Transition(in TransitionProcedureRequest request)
        {
            if (State == ProcedureState.Failed || State == ProcedureState.ShuttingDown)
            {
                return;
            }

            if (_transitionProcedureRequest == null || _transitionProcedureRequest.Value.Priority >= request.Priority)
            {
                _transitionProcedureRequest = request;
            }

            if (request.Reason == TransitionProcedureReason.Initialize && State == ProcedureState.Idle)
            {
                TryStartPendingTransition();
            }
        }

        public ProcedurePauseToken Pause(ProcedurePauseReason reason)
        {
            var reasonIndex = GetPauseReasonIndex(reason);
            if (_pauseTokenIds[reasonIndex] != 0)
            {
                throw new InvalidOperationException($"Procedure pause reason [{reason}] is already held.");
            }

            var tokenId = ++_nextPauseTokenId;
            _pauseTokenIds[reasonIndex] = tokenId;
            _pendingPauseReason |= reason;
            return new ProcedurePauseToken(this, reason, tokenId);
        }

        internal bool IsPauseTokenActive(ProcedurePauseReason reason, long tokenId)
        {
            if (tokenId == 0 || reason == ProcedurePauseReason.None)
            {
                return false;
            }

            return _pauseTokenIds[GetPauseReasonIndex(reason)] == tokenId;
        }

        internal void CancelPause(ProcedurePauseReason reason, long tokenId)
        {
            if (!IsPauseTokenActive(reason, tokenId))
            {
                return;
            }

            _pauseTokenIds[GetPauseReasonIndex(reason)] = 0;
            _pendingPauseReason &= ~reason;
        }

        public bool HasPauseReason(ProcedurePauseReason reason) => (_pendingPauseReason & reason) == reason;

        public bool IsPauseActive(ProcedurePauseReason reason) => (_activePauseReason & reason) == reason;
        
        void IPreShutdownModule.PreShutdown()
        {
            StopTransitions();
            DestroyCurrentProcedure();
        }

        void IFrameTickable.EarlyTick(in FrameTime time)
        {
            TryStartPendingTransition();

            if (State == ProcedureState.Running)
            {
                EnsurePauseReason();
            }

            var isTickEnabled = State == ProcedureState.Running && _activePauseReason == ProcedurePauseReason.None;
            _timeSource.Evaluate(isTickEnabled ? time.DeltaTime : 0f, isTickEnabled ? time.UnscaledDeltaTime : 0f);

            if (isTickEnabled)
            {
                _context.EarlyTick();
            }
        }

        void IFrameTickable.FrameTick(in FrameTime time)
        {
            if (State == ProcedureState.Running && _activePauseReason == ProcedurePauseReason.None)
            {
                _context.FrameTick();
                _current.FrameTick();
            }
        }

        void IFrameTickable.LateTick(in FrameTime time)
        {
            if (State == ProcedureState.Running && _activePauseReason == ProcedurePauseReason.None)
            {
                _context.LateTick();
                _current.LateTick();
            }
        }
        
        private void DestroyCurrentProcedure()
        {
            var current = _current;
            var context = _context;
            _current = null;
            _context = null;

            var exceptions = new CleanupExceptionCollector();
            if (current != null)
            {
                try
                {
                    current.Destroy();
                }
                catch (Exception exception)
                {
                    exceptions.Add(exception);
                }
            }

            if (context != null)
            {
                try
                {
                    context.Destroy();
                }
                catch (Exception exception)
                {
                    exceptions.Add(exception);
                }
                finally
                {
                    UnbindContext(context);
                }
            }

            exceptions.ThrowIfAny();
        }

        private void EnterFailedState(Exception exception)
        {
            if (State == ProcedureState.Failed || State == ProcedureState.ShuttingDown)
            {
                return;
            }

            State = ProcedureState.Failed;
            _transitionProcedureRequest = null;
            Debug.LogException(exception);
        }
        
        private void TryStartPendingTransition()
        {
            if (State == ProcedureState.Transitioning || State == ProcedureState.Failed || State == ProcedureState.ShuttingDown || _lifetimeCancellationSource == null || _transitionProcedureRequest == null)
            {
                return;
            }

            var request = _transitionProcedureRequest.Value;
            _transitionProcedureRequest = null;
            State = ProcedureState.Transitioning;
            ProcessTransition(request).Forget(EnterFailedState);
        }

        private async UniTask ProcessTransition(TransitionProcedureRequest request)
        {
            var cancellationToken = _lifetimeCancellationSource.Token;
            try
            {
                var viewToken = UIViewToken.None;
                if (request.Reason != TransitionProcedureReason.Startup && _uiSystem.IsInitialized)
                {
                    viewToken = _uiSystem.OpenView(new BlackMaskView.Data());
                    await viewToken.WaitForShownAsync(cancellationToken);
                }

                _uiSystem.CloseAll(UILayer.HUD, true);
                _uiSystem.CloseAll(UILayer.View, true);
                _uiSystem.CloseAll(UILayer.Popup, true);

                await CleanupCurrentProcedureAsync();
                cancellationToken.ThrowIfCancellationRequested();
                ResetPauseState();
                _timeSource.Reset();

                _context = BindContext(new ProcedureContext(_timeSource, cancellationToken, EnterFailedState));
                _current = CreateProcedure(request.TargetProcedure);
                _current.Initialize(_context);
                await _current.EnterAsync(request.Options);
                await UniTask.NextFrame(PlayerLoopTiming.EarlyUpdate, cancellationToken);
                await _current.ReadyAsync();
                cancellationToken.ThrowIfCancellationRequested();

                _uiSystem.TrimUnusedViewAssets();
                await _assetService.UnloadUnusedAssets(cancellationToken);
                viewToken.Close();
                await UniTask.NextFrame(PlayerLoopTiming.LastTimeUpdate, cancellationToken);
                State = ProcedureState.Running;
                this.PublishEvent(new EnterProcedureSuccessEvent(request.TargetProcedure, _context));
            }
            catch (Exception) when (State == ProcedureState.ShuttingDown)
            {
            }
        }

        private async UniTask CleanupCurrentProcedureAsync()
        {
            if (_current == null)
            {
                return;
            }

            var exceptions = new CleanupExceptionCollector();
            try
            {
                await _current.TeardownAsync();
            }
            catch (Exception exception)
            {
                exceptions.Add(exception);
            }
            try
            {
                await _current.ExitAsync();
            }
            catch (Exception exception)
            {
                exceptions.Add(exception);
            }
            try
            {
                DestroyCurrentProcedure();
            }
            catch (Exception exception)
            {
                exceptions.Add(exception);
            }
            exceptions.ThrowIfAny();
        }

        private void StopTransitions()
        {
            State = ProcedureState.ShuttingDown;
            _transitionProcedureRequest = null;
            _lifetimeCancellationSource?.Cancel();
            ResetPauseState();
        }

        private void ResetPauseState()
        {
            _activePauseReason = ProcedurePauseReason.None;
            _pendingPauseReason = ProcedurePauseReason.None;
            Array.Clear(_pauseTokenIds, 0, _pauseTokenIds.Length);
            _pauseInputModeCancelToken.Cancel();
            _pauseInputModeCancelToken = default;
        }

        private static int GetPauseReasonIndex(ProcedurePauseReason reason)
        {
            var value = (byte)reason;
            if (value == 0 || (value & (value - 1)) != 0)
            {
                throw new ArgumentOutOfRangeException(nameof(reason), reason, "A single pause reason is required.");
            }

            var index = 0;
            while (value > 1)
            {
                value >>= 1;
                ++index;
            }
            return index;
        }

        private void EnsurePauseReason()
        {
            if (_activePauseReason == _pendingPauseReason)
            {
                return;
            }
            
            var previousPauseReason = _activePauseReason;
            _activePauseReason = _pendingPauseReason;

            if (previousPauseReason == ProcedurePauseReason.None && _activePauseReason != ProcedurePauseReason.None)
            {
                _pauseInputModeCancelToken = this.GetService<IInputService>().PushMode(InputModeCatalog.Pause);
                _current.Paused();
            }
            else if (previousPauseReason != ProcedurePauseReason.None && _activePauseReason == ProcedurePauseReason.None)
            {
                _pauseInputModeCancelToken.Cancel();
                _pauseInputModeCancelToken = default;
                _current.Resumed();
            }
        }
        
        private static IProcedure CreateProcedure(ProcedureType type) => type switch
        {
            ProcedureType.Launch => new LaunchProcedure(),
            ProcedureType.MainMenu => new MainMenuProcedure(),
            ProcedureType.Game => new GameProcedure(),
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };
    }
}
