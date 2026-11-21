using System;
using System.Collections.Generic;
using System.Threading;
using Aoiro;
using Cysharp.Threading.Tasks;
using Gameplay.Asset;
using Gameplay.Scheduler;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;

namespace Gameplay
{
    public sealed class CameraSystem : SystemBase, IFrameTickable
    {
        private const string SettingsLocation = "Global/CameraSystemSettings";

        public Camera MainCamera => _context.MainCamera;
        public CameraSelection Selected => _director.Selected;
        public bool IsBlending => _director.IsBlending;
        public bool IsInitialized { get; private set; }

        private readonly CameraContext _context = new CameraContext();
        private readonly CameraDirector _director = new CameraDirector();
        private readonly HashSet<long> _mainCameraHideOwnerIds = new HashSet<long>();
        private readonly Dictionary<long, Camera> _overlayCamerasByOwnerId = new Dictionary<long, Camera>();

        private IAssetService _assetService;
        private IPersistentSceneService _persistentSceneService;
        private IAssetAsyncOperation<CameraSystemSettings> _settingsOperation;
        private IAssetAsyncOperation<GameObject> _mainCameraOperation;
        private CameraSystemSettings _settings;
        private UniversalAdditionalCameraData _additionalCameraData;
        private long _lastMainCameraHideOwnerId;
        private long _lastOverlayCameraOwnerId;
        private bool _mainCameraEnabledBeforeFirstHide;
        private int _mainCameraCullingMaskBeforeFirstHide;

        public void RegisterCameraModule<T>(T module) where T : class, ICameraModule => _director.RegisterCameraModule(module);

        protected override void OnInitialize()
        {
            _assetService = this.GetService<IAssetService>();
            _persistentSceneService = this.GetService<IPersistentSceneService>();
        }

        protected override void OnShutdown()
        {
            _mainCameraHideOwnerIds.Clear();
            _overlayCamerasByOwnerId.Clear();
            _director.Shutdown();
            CleanupInfrastructure(false);
            _persistentSceneService = null;
            _assetService = null;
        }

        public async UniTask InitializeAsync(CancellationToken cancellationToken = default, IProgress<float> progress = null)
        {
            if (IsInitialized)
            {
                progress?.Report(1f);
                return;
            }

            try
            {
                progress?.Report(0f);
                _settingsOperation = _assetService.LoadAssetAsync<CameraSystemSettings>(SettingsLocation);
                _settings = await _settingsOperation.ToUniTask(cancellationToken: cancellationToken, cancelImmediately: true);
                ValidateSettings();
                progress?.Report(0.25f);

                _mainCameraOperation = _assetService.LoadAssetAsync<GameObject>(_settings.MainCameraLocation);
                var mainCameraPrefab = await _mainCameraOperation.ToUniTask(cancellationToken: cancellationToken, cancelImmediately: true);
                CompleteInfrastructureInitialization(mainCameraPrefab);
                progress?.Report(0.75f);

                await _director.ReadyAsync(cancellationToken);
                IsInitialized = true;
                progress?.Report(1f);
            }
            catch
            {
                _director.Shutdown();
                CleanupInfrastructure(true);
                throw;
            }
        }

        public T GetModule<T>() where T : class, ICameraModule => _director.GetModule<T>();

        public bool TryGetModule<T>(out T module) where T : class, ICameraModule => _director.TryGetModule(out module);

        public CameraDebugSnapshot GetDebugSnapshot() => _director.GetDebugSnapshot();

        public MainCameraHideToken HideMainCamera()
        {
            if (MainCamera == null)
            {
                throw new InvalidOperationException("Cannot hide the main camera before CameraSystem initialization.");
            }
            var ownerId = ++_lastMainCameraHideOwnerId;
            _mainCameraHideOwnerIds.Add(ownerId);
            if (_mainCameraHideOwnerIds.Count == 1)
            {
                _mainCameraEnabledBeforeFirstHide = MainCamera.enabled;
                _mainCameraCullingMaskBeforeFirstHide = MainCamera.cullingMask;
                var uiLayer = LayerMask.NameToLayer("UI");
                MainCamera.cullingMask = uiLayer >= 0 ? 1 << uiLayer : 0;
                MainCamera.enabled = _mainCameraEnabledBeforeFirstHide;
            }
            return new MainCameraHideToken(this, ownerId);
        }

        public void CancelMainCameraHide(MainCameraHideToken token)
        {
            if (!_mainCameraHideOwnerIds.Remove(token.OwnerId) || _mainCameraHideOwnerIds.Count != 0)
            {
                return;
            }
            if (MainCamera != null)
            {
                MainCamera.cullingMask = _mainCameraCullingMaskBeforeFirstHide;
                MainCamera.enabled = _mainCameraEnabledBeforeFirstHide;
            }
        }

        public OverlayCameraToken AddOverlayCamera(Camera camera)
        {
            if (camera == null)
            {
                throw new ArgumentNullException(nameof(camera));
            }
            if (!camera.TryGetComponent<UniversalAdditionalCameraData>(out var cameraData) || cameraData.renderType != CameraRenderType.Overlay)
            {
                throw new InvalidOperationException($"Camera [{camera.name}] must be configured as a URP Overlay camera.");
            }
            var stack = _additionalCameraData.cameraStack;
            if (!stack.Contains(camera))
            {
                stack.Add(camera);
            }

            var ownerId = ++_lastOverlayCameraOwnerId;
            _overlayCamerasByOwnerId.Add(ownerId, camera);
            return new OverlayCameraToken(this, ownerId);
        }

        public void CancelOverlayCamera(OverlayCameraToken token)
        {
            if (!_overlayCamerasByOwnerId.Remove(token.OwnerId, out var camera))
            {
                return;
            }

            foreach (var registeredCamera in _overlayCamerasByOwnerId.Values)
            {
                if (ReferenceEquals(registeredCamera, camera))
                {
                    return;
                }
            }

            if (camera != null && _additionalCameraData != null)
            {
                _additionalCameraData.cameraStack.Remove(camera);
            }
        }

        public FrameTickToken AddAfterCameraUpdate(FrameTickAction action) => _director.AddAfterCameraUpdate(action);

        void IFrameTickable.EarlyTick(in FrameTime time) { }

        void IFrameTickable.FrameTick(in FrameTime time)
        {
            if (!IsInitialized)
            {
                return;
            }
            _director.FrameTick(in time);
        }

        void IFrameTickable.LateTick(in FrameTime time)
        {
            if (!IsInitialized)
            {
                return;
            }
            _director.LateTick(in time);
        }

        private void ValidateSettings()
        {
            if (_settings == null)
            {
                throw new InvalidOperationException($"Camera system settings [{SettingsLocation}] could not be loaded.");
            }
            if (string.IsNullOrWhiteSpace(_settings.MainCameraLocation))
            {
                throw new InvalidOperationException("Camera system settings has no main camera location.");
            }
            if ((int)_settings.OutputChannel == 0)
            {
                throw new InvalidOperationException("Camera system output channel cannot be None.");
            }
        }

        private void CreateRoots()
        {
            var root = new GameObject("[Camera]") { hideFlags = HideFlags.NotEditable };
            _persistentSceneService.MoveToPersistent(root);
            _context.ActiveRoot = new GameObject("[Active]") { hideFlags = HideFlags.NotEditable }.transform;
            _context.ActiveRoot.SetParent(root.transform, false);
            _context.StagingRoot = new GameObject("[Staging]") { hideFlags = HideFlags.NotEditable }.transform;
            _context.StagingRoot.SetParent(root.transform, false);
            _context.StagingRoot.gameObject.SetActive(false);
        }

        private void CreateMainCamera(GameObject prefab)
        {
            if (prefab == null)
            {
                throw new InvalidOperationException($"Main camera prefab [{_settings.MainCameraLocation}] could not be loaded.");
            }
            var instance = Object.Instantiate(prefab, _context.ActiveRoot, false);
            _context.MainCamera = instance.GetComponent<Camera>();
            _context.Brain = instance.GetComponent<CinemachineBrain>();
            _additionalCameraData = instance.GetComponent<UniversalAdditionalCameraData>();
            if (_context.MainCamera == null || _context.Brain == null || _additionalCameraData == null)
            {
                throw new InvalidOperationException($"Main camera prefab [{_settings.MainCameraLocation}] is missing required components.");
            }
            _context.Brain.UpdateMethod = CinemachineBrain.UpdateMethods.ManualUpdate;
            _context.Brain.ChannelMask = _settings.OutputChannel;
            _context.Brain.DefaultBlend = _settings.DefaultBlend.ToDefinition();
        }

        private void CompleteInfrastructureInitialization(GameObject mainCameraPrefab)
        {
            BindContext(_context);
            CreateRoots();
            CreateMainCamera(mainCameraPrefab);
            _director.Initialize(_context, _settings);
        }

        private void CleanupInfrastructure(bool immediate)
        {
            var root = _context.ActiveRoot != null ? _context.ActiveRoot.parent : null;
            if (root != null)
            {
#if UNITY_EDITOR
                if (immediate && !Application.isPlaying)
                {
                    Object.DestroyImmediate(root.gameObject);
                }
                else
#endif
                {
                    Object.Destroy(root.gameObject);
                }
            }
            _context.Cleanup();
            _additionalCameraData = null;
            _mainCameraOperation?.Release();
            _mainCameraOperation = null;
            _settingsOperation?.Release();
            _settingsOperation = null;
            _settings = null;
            IsInitialized = false;
        }
    }
}
