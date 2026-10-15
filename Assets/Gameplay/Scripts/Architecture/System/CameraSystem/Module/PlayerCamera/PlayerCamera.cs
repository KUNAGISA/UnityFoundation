using System;
using System.Threading;
using Aoiro;
using Cysharp.Threading.Tasks;
using Gameplay.Asset;
using Gameplay.Scheduler;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Gameplay
{
    public sealed class PlayerCamera : CameraModule, ICameraProviderModule, ICameraSessionTokenOwner
    {
        public CameraProviderId ProviderId => CameraProviderId.Player;
        public bool IsActive => _activeSessionId > 0;

        private readonly string _settingsLocation;

        private IAssetService _assetService;
        private IAssetAsyncOperation<PlayerCameraSettings> _settingsOperation;
        private IAssetAsyncOperation<GameObject> _rigOperation;
        private CancellationTokenSource _moduleCancellationSource;
        private PlayerCameraSettings _settings;
        private PlayerCameraRig _cameraRig;
        private PlayerEntity _player;
        private CameraOutputRegistration _outputRegistration;
        private FrameTickToken _lateTickToken;
        private CancellationTokenRegistration _lifetimeRegistration;
        private long _lastSessionId;
        private long _activeSessionId;

        public PlayerCamera(string settingsLocation)
        {
            _settingsLocation = settingsLocation;
        }

        protected override void OnInitialize()
        {
            if (string.IsNullOrWhiteSpace(_settingsLocation))
            {
                throw new InvalidOperationException("Player camera settings location is empty.");
            }
            _assetService = Context.GetService<IAssetService>();
            _moduleCancellationSource = new CancellationTokenSource();
        }

        protected override void OnShutdown()
        {
            _moduleCancellationSource?.Cancel();
            EndSession(_activeSessionId);
            if (_cameraRig != null)
            {
                Object.Destroy(_cameraRig.gameObject);
                _cameraRig = null;
            }
            _rigOperation?.Release();
            _rigOperation = null;
            _settingsOperation?.Release();
            _settingsOperation = null;
            _assetService = null;
            _settings = null;
            _moduleCancellationSource?.Dispose();
            _moduleCancellationSource = null;
        }

        protected override UniTask OnReadyAsync(CancellationToken cancellationToken) => EnsureSettingsAsync(cancellationToken);

        public async UniTask<CameraSessionToken> ActivateAsync(PlayerCameraActivateOptions options, CancellationToken cancellationToken = default)
        {
            if (IsActive)
            {
                throw new InvalidOperationException($"Player camera session [{_activeSessionId}] is already active.");
            }
            if (options.Player == null || options.Player.Root == null)
            {
                throw new ArgumentException("Player camera activation requires a valid player.", nameof(options));
            }

            using var linkedCancellationSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _moduleCancellationSource.Token);
            var activationCancellationToken = linkedCancellationSource.Token;
            activationCancellationToken.ThrowIfCancellationRequested();
            await EnsureRigAsync(activationCancellationToken);
            activationCancellationToken.ThrowIfCancellationRequested();

            var sessionId = ++_lastSessionId;
            try
            {
                _activeSessionId = sessionId;
                _player = options.Player;
                ApplyFollowImmediate();
                _cameraRig.Snap();

                if (!_outputRegistration.IsValid)
                {
                    var range = Context.GetPriorityRange(ProviderId);
                    var priority = range.ResolveOffset(_settings.PriorityOffset);
                    _outputRegistration = Context.RegisterOutput(ProviderId, sessionId, priority, _cameraRig.NormalCamera);
                }
                else
                {
                    _outputRegistration.UpdateSession(sessionId);
                }

                _cameraRig.transform.SetParent(Context.ActiveRoot, false);
                _cameraRig.gameObject.SetActive(true);
                _outputRegistration.SetActive(true);
                _lateTickToken = Context.AddFrameTick(OnLateTick, CameraUpdatePhase.LateTick);
                _lifetimeRegistration = cancellationToken.Register(static state => ((PlayerCamera)state).CancelActiveSession(), this);
                return new CameraSessionToken(this, sessionId);
            }
            catch
            {
                EndSession(sessionId);
                throw;
            }
        }

        public void Snap()
        {
            if (!IsActive)
            {
                throw new InvalidOperationException("Player camera has no active session.");
            }
            ApplyFollowImmediate();
            _cameraRig.Snap();
        }

        bool ICameraSessionTokenOwner.IsSessionActive(long sessionId) => _activeSessionId == sessionId;
        void ICameraSessionTokenOwner.CancelSession(long sessionId) => EndSession(sessionId);

        private async UniTask EnsureRigAsync(CancellationToken cancellationToken)
        {
            await EnsureSettingsAsync(cancellationToken);
            if (_cameraRig != null)
            {
                return;
            }
            _rigOperation ??= _assetService.LoadAssetAsync<GameObject>(_settings.RigLocation);
            var prefab = await _rigOperation.ToUniTask(cancellationToken: cancellationToken, cancelImmediately: true);
            if (prefab == null)
            {
                throw new InvalidOperationException($"Player camera rig [{_settings.RigLocation}] could not be loaded.");
            }
            var instance = Object.Instantiate(prefab, Context.StagingRoot, false);
            _cameraRig = instance.GetComponent<PlayerCameraRig>();
            if (_cameraRig == null)
            {
                Object.Destroy(instance);
                throw new InvalidOperationException($"Player camera rig [{_settings.RigLocation}] has no PlayerCameraRig component.");
            }
            _cameraRig.ValidateConfiguration();
            _cameraRig.gameObject.SetActive(false);
        }

        private async UniTask EnsureSettingsAsync(CancellationToken cancellationToken)
        {
            if (_settings != null)
            {
                return;
            }
            _settingsOperation ??= _assetService.LoadAssetAsync<PlayerCameraSettings>(_settingsLocation);
            _settings = await _settingsOperation.ToUniTask(cancellationToken: cancellationToken, cancelImmediately: true);
            if (_settings == null)
            {
                throw new InvalidOperationException($"Player camera settings [{_settingsLocation}] could not be loaded.");
            }
            _settings.Validate(Context.GetPriorityRange(ProviderId));
        }

        private void OnLateTick(in FrameTime _)
        {
            var spatialDiscontinuity = IsSpatialDiscontinuity();
            ApplyFollowImmediate();
            if (spatialDiscontinuity)
            {
                _cameraRig.Snap();
            }
        }

        private void ApplyFollowImmediate()
        {
            if (_player?.Root == null)
            {
                return;
            }
            var root = _player.Root;
            _cameraRig.Follow.SetPositionAndRotation(root.position, root.rotation);
        }

        private bool IsSpatialDiscontinuity()
        {
            if (_player?.Root == null)
            {
                return false;
            }
            var difference = _player.Root.position - _cameraRig.Follow.position;
            var threshold = _settings.SnapDistanceThreshold;
            return difference.sqrMagnitude >= threshold * threshold;
        }

        private void EndSession(long sessionId)
        {
            if (sessionId <= 0 || sessionId != _activeSessionId)
            {
                return;
            }
            _lifetimeRegistration.Dispose();
            _lifetimeRegistration = default;
            _lateTickToken.Cancel();
            _lateTickToken = default;
            _outputRegistration.SetActive(false);
            _cameraRig.gameObject.SetActive(false);
            _player = null;
            _activeSessionId = 0;
        }

        private void CancelActiveSession() => EndSession(_activeSessionId);

    }
}
