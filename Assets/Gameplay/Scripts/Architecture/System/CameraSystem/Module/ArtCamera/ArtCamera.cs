using System;
using System.Collections.Generic;
using System.Threading;
using Aoiro;
using Cysharp.Threading.Tasks;
using Gameplay.Asset;
using Gameplay.Scheduler;
using Unity.Cinemachine;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Gameplay
{
    public sealed class ArtCamera : CameraModule, ICameraProviderModule, ICameraSessionTokenOwner, ICameraSelectionListener, ICameraProviderBlendResolver
    {
        public CameraProviderId ProviderId => CameraProviderId.Art;
        public bool IsActive
        {
            get
            {
                for (var index = 0; index < _sessions.Count; ++index)
                {
                    if (_sessions[index].State != ArtCameraSessionState.Deactivating)
                    {
                        return true;
                    }
                }
                return false;
            }
        }

        private readonly string _settingsLocation;
        private readonly List<ArtCameraSession> _sessions = new List<ArtCameraSession>();

        private IAssetService _assetService;
        private ITimeSource _procedureTimeSource;
        private IAssetAsyncOperation<ArtCameraSettings> _settingsOperation;
        private CancellationTokenSource _moduleCancellationSource;
        private ArtCameraSettings _settings;
        private CameraPriorityRange _priorityRange;
        private FrameTickToken _lateTickToken;
        private CameraSelection _selection;
        private long _lastSessionId;

        public ArtCamera(string settingsLocation)
        {
            _settingsLocation = settingsLocation;
        }

        protected override void OnInitialize()
        {
            if (string.IsNullOrWhiteSpace(_settingsLocation))
            {
                throw new InvalidOperationException("Art camera settings location is empty.");
            }
            _assetService = Context.GetService<IAssetService>();
            _procedureTimeSource = Context.GetSystem<ProcedureSystem>().Time;
            _moduleCancellationSource = new CancellationTokenSource();
            _priorityRange = Context.GetPriorityRange(ProviderId);
            _lateTickToken = Context.AddFrameTick(OnLateTick, CameraUpdatePhase.LateTick);
        }

        protected override void OnShutdown()
        {
            _moduleCancellationSource?.Cancel();
            _lateTickToken.Cancel();
            _lateTickToken = default;
            for (var index = _sessions.Count - 1; index >= 0; --index)
            {
                _sessions[index].ReadySource.TrySetResult();
                ReportCompletion(_sessions[index], ArtCameraCompletionReason.Cancelled);
                CleanupSession(_sessions[index], false);
            }
            _sessions.Clear();
            _settingsOperation?.Release();
            _settingsOperation = null;
            _procedureTimeSource = null;
            _assetService = null;
            _settings = null;
            _moduleCancellationSource?.Dispose();
            _moduleCancellationSource = null;
        }

        protected override UniTask OnReadyAsync(CancellationToken cancellationToken) => EnsureSettingsAsync(cancellationToken);

        /// <summary>
        /// 立即创建并返回 Loading Session；资源加载、追帧和输出启用由 ArtCamera 内部完成。
        /// </summary>
        public ArtCameraPlayback Activate(ArtCameraActivateOptions options, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(options.Location))
            {
                throw new ArgumentException("Art camera prefab location is empty.", nameof(options));
            }
            if (options.CompletionPolicy > ArtCameraCompletionPolicy.LoopUntilCancelled)
            {
                throw new ArgumentOutOfRangeException(nameof(options), options.CompletionPolicy, "Art camera completion policy is invalid.");
            }

            cancellationToken.ThrowIfCancellationRequested();
            EnsureLayerAvailable(options.Layer);

            var sessionId = ++_lastSessionId;
            var session = new ArtCameraSession
            {
                SessionId = sessionId,
                Layer = options.Layer,
                CoveredPolicy = options.CoveredPolicy,
                CompletionPolicy = options.CompletionPolicy,
                Bindings = options.Bindings,
                ContentTimeSource = options.TimeMode == ArtCameraTimeMode.Procedure ? _procedureTimeSource : null,
                LoadingCancellationSource = CancellationTokenSource.CreateLinkedTokenSource(_moduleCancellationSource.Token),
                ReadySource = new UniTaskCompletionSource(),
                CompletionSource = new UniTaskCompletionSource<ArtCameraCompletionResult>(),
                State = ArtCameraSessionState.Loading
            };
            session.LifetimeRegistration = cancellationToken.Register(static state =>
            {
                ((ArtCameraSession)state).CancelRequested = true;
            }, session);
            _sessions.Add(session);
            LoadSessionAsync(session, options.Location).Forget();

            var token = new CameraSessionToken(this, sessionId);
            return new ArtCameraPlayback(sessionId, token, session.ReadySource.Task, session.CompletionSource.Task);
        }

        bool ICameraSessionTokenOwner.IsSessionActive(long sessionId)
        {
            var session = FindSession(sessionId);
            return session != null && session.State != ArtCameraSessionState.Deactivating;
        }

        void ICameraSessionTokenOwner.CancelSession(long sessionId)
        {
            var session = FindSession(sessionId);
            if (session != null && session.State != ArtCameraSessionState.Deactivating)
            {
                if (session.State == ArtCameraSessionState.Loading)
                {
                    CompleteLoadingSession(session, ArtCameraCompletionReason.Cancelled);
                }
                else
                {
                    BeginDeactivate(session, ArtCameraCompletionReason.Cancelled);
                }
            }
        }

        void ICameraSelectionListener.OnCameraSelectionChanged(in CameraSelection selection) => _selection = selection;

        bool ICameraProviderBlendResolver.TryResolveBlend(long fromSessionId, long toSessionId, out CinemachineBlendDefinition blend)
        {
            var from = FindSession(fromSessionId);
            var to = FindSession(toSessionId);
            if (from != null && to != null)
            {
                blend = _settings.ResolveBlend(from.Layer, to.Layer);
                return true;
            }
            blend = default;
            return false;
        }

        private async UniTask LoadSessionAsync(ArtCameraSession session, string location)
        {
            try
            {
                var cancellationToken = session.LoadingCancellationSource.Token;
                await EnsureSettingsAsync(cancellationToken);
                await UniTask.SwitchToMainThread();
                if (session.CleanedUp)
                {
                    return;
                }
                if (session.CancelRequested)
                {
                    CompleteLoadingSession(session, ArtCameraCompletionReason.Cancelled);
                    return;
                }

                session.Priority = _settings.ResolvePriority(session.Layer, in _priorityRange);
                session.PriorityResolved = true;
                if (TryCancelCoveredLoadingSession(session))
                {
                    return;
                }

                session.AssetOperation = _assetService.LoadAssetAsync<GameObject>(location);
                var prefab = await session.AssetOperation.ToUniTask(cancellationToken: cancellationToken, cancelImmediately: true);
                await UniTask.SwitchToMainThread();
                if (session.CleanedUp)
                {
                    return;
                }
                if (session.CancelRequested)
                {
                    CompleteLoadingSession(session, ArtCameraCompletionReason.Cancelled);
                    return;
                }
                cancellationToken.ThrowIfCancellationRequested();
                if (prefab == null)
                {
                    throw new InvalidOperationException($"Art camera prefab [{location}] could not be loaded.");
                }

                var instance = Object.Instantiate(prefab, Context.StagingRoot, false);
                session.Rig = instance.GetComponent<ArtCameraRig>();
                if (session.Rig == null)
                {
                    Object.Destroy(instance);
                    throw new InvalidOperationException($"Art camera prefab [{location}] has no ArtCameraRig component.");
                }
                session.Rig.ValidateAndPrepare();
                session.Rig.ApplyBindings(session.Bindings);
                session.Rig.CreatePlayback();
                session.Bindings = null;

                if (session.CancelRequested)
                {
                    CompleteLoadingSession(session, ArtCameraCompletionReason.Cancelled);
                    return;
                }
                if (TryCancelCoveredLoadingSession(session))
                {
                    return;
                }

                var completed = session.Rig.Evaluate(session.LoadingElapsed, session.CompletionPolicy);
                if (completed && session.CompletionPolicy == ArtCameraCompletionPolicy.ReleaseOnComplete)
                {
                    CompleteLoadingSession(session, ArtCameraCompletionReason.Completed);
                    return;
                }

                session.OutputRegistration = Context.RegisterOutput(ProviderId, session.SessionId, session.Priority, session.Rig.OutputCamera);
                session.Rig.transform.SetParent(Context.ActiveRoot, false);
                session.Rig.gameObject.SetActive(true);
                session.Rig.SetOutputEnabled(true);
                session.OutputRegistration.SetActive(true);
                session.State = completed ? ArtCameraSessionState.Holding : ArtCameraSessionState.Active;
                session.ReadySource.TrySetResult();
                if (completed)
                {
                    ReportCompletion(session, ArtCameraCompletionReason.Completed);
                }
            }
            catch (OperationCanceledException)
            {
                if (session.CleanedUp)
                {
                    return;
                }
                await UniTask.SwitchToMainThread();
                CompleteLoadingSession(session, ArtCameraCompletionReason.Cancelled);
            }
            catch (Exception exception)
            {
                if (session.CleanedUp)
                {
                    return;
                }
                await UniTask.SwitchToMainThread();
                FailLoadingSession(session, exception);
            }
        }

        private async UniTask EnsureSettingsAsync(CancellationToken cancellationToken)
        {
            if (_settings != null)
            {
                return;
            }
            _settingsOperation ??= _assetService.LoadAssetAsync<ArtCameraSettings>(_settingsLocation);
            _settings = await _settingsOperation.ToUniTask(cancellationToken: cancellationToken, cancelImmediately: true);
            if (_settings == null)
            {
                throw new InvalidOperationException($"Art camera settings [{_settingsLocation}] could not be loaded.");
            }
            _settings.Validate(in _priorityRange);
        }

        private void OnLateTick(in FrameTime time)
        {
            for (var index = _sessions.Count - 1; index >= 0; --index)
            {
                var session = _sessions[index];
                if (session.State == ArtCameraSessionState.Loading)
                {
                    if (session.CancelRequested)
                    {
                        CompleteLoadingSession(session, ArtCameraCompletionReason.Cancelled);
                        continue;
                    }
                    if (session.PriorityResolved)
                    {
                        if (TryCancelCoveredLoadingSession(session))
                        {
                            continue;
                        }
                        if (session.CoveredPolicy == ArtCameraCoveredPolicy.PauseWhenCovered && Context.IsPriorityCovered(session.Priority))
                        {
                            continue;
                        }
                    }
                    session.LoadingElapsed += session.ContentTimeSource?.DeltaTime ?? time.DeltaTime;
                    continue;
                }
                if (session.State == ArtCameraSessionState.Deactivating)
                {
                    session.DeactivationElapsed += time.DeltaTime;
                    if (Time.frameCount > session.DeactivationFrame && !session.OutputRegistration.IsLive())
                    {
                        _sessions.RemoveAt(index);
                        CleanupSession(session, true);
                    }
                    else if (session.DeactivationElapsed >= _settings.LiveWarningInterval)
                    {
                        Debug.LogWarning($"Art camera session [{session.SessionId}] is still live after [{session.DeactivationElapsed:F1}] seconds.");
                        session.DeactivationElapsed = 0f;
                    }
                    continue;
                }

                if (session.CancelRequested)
                {
                    BeginDeactivate(session, ArtCameraCompletionReason.Cancelled);
                    continue;
                }
                if (session.Rig.SyncBindings() == ArtCameraLostTargetPolicy.CancelSession)
                {
                    BeginDeactivate(session, ArtCameraCompletionReason.TargetLost);
                    continue;
                }

                var selected = _selection.ProviderId == ProviderId && _selection.SessionId == session.SessionId;
                if (!selected && session.CoveredPolicy == ArtCameraCoveredPolicy.CancelWhenCovered)
                {
                    BeginDeactivate(session, ArtCameraCompletionReason.CoveredAndCancelled);
                    continue;
                }
                var shouldAdvance = selected || session.CoveredPolicy == ArtCameraCoveredPolicy.ContinueWhenCovered;
                if (session.State == ArtCameraSessionState.Holding || !shouldAdvance)
                {
                    continue;
                }

                var contentDeltaTime = session.ContentTimeSource?.DeltaTime ?? time.DeltaTime;
                if (!session.Rig.Evaluate(contentDeltaTime, session.CompletionPolicy))
                {
                    continue;
                }
                if (session.CompletionPolicy == ArtCameraCompletionPolicy.HoldLastFrame)
                {
                    session.State = ArtCameraSessionState.Holding;
                    ReportCompletion(session, ArtCameraCompletionReason.Completed);
                }
                else
                {
                    BeginDeactivate(session, ArtCameraCompletionReason.Completed);
                }
            }
        }

        private void BeginDeactivate(ArtCameraSession session, ArtCameraCompletionReason reason)
        {
            if (session.State == ArtCameraSessionState.Deactivating)
            {
                return;
            }
            ReportCompletion(session, reason);
            session.State = ArtCameraSessionState.Deactivating;
            session.DeactivationFrame = Time.frameCount;
            session.DeactivationElapsed = 0f;
            session.OutputRegistration.SetActive(false);
            session.Rig.SetOutputEnabled(false);
        }

        private bool TryCancelCoveredLoadingSession(ArtCameraSession session)
        {
            if (session.CoveredPolicy != ArtCameraCoveredPolicy.CancelWhenCovered || !Context.IsPriorityCovered(session.Priority))
            {
                return false;
            }
            CompleteLoadingSession(session, ArtCameraCompletionReason.CoveredAndCancelled);
            return true;
        }

        private void CompleteLoadingSession(ArtCameraSession session, ArtCameraCompletionReason reason)
        {
            if (session.CleanedUp)
            {
                return;
            }
            session.ReadySource.TrySetResult();
            ReportCompletion(session, reason);
            _sessions.Remove(session);
            CleanupSession(session, true);
        }

        private void FailLoadingSession(ArtCameraSession session, Exception exception)
        {
            if (session.CleanedUp)
            {
                return;
            }
            Debug.LogException(exception);
            session.CompletionReported = true;
            session.ReadySource.TrySetException(exception);
            session.CompletionSource.TrySetException(exception);
            _sessions.Remove(session);
            CleanupSession(session, true);
        }

        private void ReportCompletion(ArtCameraSession session, ArtCameraCompletionReason reason)
        {
            if (session.CompletionReported)
            {
                return;
            }
            session.CompletionReported = true;
            session.CompletionSource.TrySetResult(new ArtCameraCompletionResult(session.SessionId, reason, session.Rig == null ? 0f : session.Rig.NormalizedTime));
        }

        private void CleanupSession(ArtCameraSession session, bool unregisterOutput)
        {
            if (session.CleanedUp)
            {
                return;
            }
            session.CleanedUp = true;
            session.LifetimeRegistration.Dispose();
            session.LoadingCancellationSource.Cancel();
            session.LoadingCancellationSource.Dispose();
            session.Rig?.DisposePlayback();
            if (unregisterOutput && !session.OutputRegistration.IsLive())
            {
                session.OutputRegistration.Cancel();
            }
            if (session.Rig != null)
            {
                Object.Destroy(session.Rig.gameObject);
            }
            session.AssetOperation?.Release();
            session.AssetOperation = null;
            session.Rig = null;
        }

        private void EnsureLayerAvailable(ArtCameraLayer layer)
        {
            for (var index = 0; index < _sessions.Count; ++index)
            {
                if (_sessions[index].Layer == layer && _sessions[index].State != ArtCameraSessionState.Deactivating)
                {
                    throw new InvalidOperationException($"Art camera layer [{layer}] already has an active session [{_sessions[index].SessionId}].");
                }
            }
        }

        private ArtCameraSession FindSession(long sessionId)
        {
            for (var index = 0; index < _sessions.Count; ++index)
            {
                if (_sessions[index].SessionId == sessionId)
                {
                    return _sessions[index];
                }
            }
            return null;
        }
    }
}
