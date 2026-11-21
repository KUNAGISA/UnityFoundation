using System;
using System.Collections.Generic;
using System.Threading;
using Aoiro;
using Cysharp.Threading.Tasks;
using Gameplay.Asset;
using Gameplay.Common;
using Gameplay.UI;
using UnityEngine;
using UnityEngine.InputSystem.UI;

namespace Gameplay
{
    internal sealed class UISystem : SystemBase, ISystemAccessor
    {
        public bool IsInitialized => _viewHost != null;

        private readonly HashSet<string> _usedViewPrefabLocations = new(StringComparer.Ordinal);
        private IAssetService _assetService;
        private IInputService _inputService;
        private IAssetAsyncOperation<GameObject> _rootAssetOperation;
        private UIRoot _root;
        private UIViewHost _viewHost;
        private UIViewPrefabCache _viewPrefabCache;
        private InputSystemUIInputModule _inputModule;
        private UIInputModuleToken _inputModuleToken;
        private OverlayCameraToken _backgroundCameraToken;
        private OverlayCameraToken _foregroundCameraToken;

        protected override void OnInitialize()
        {
            _assetService = this.GetService<IAssetService>();
            _inputService = this.GetService<IInputService>();
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
                _rootAssetOperation = _assetService.LoadAssetAsync<GameObject>("Global/UIRoot");
                var rootPrefab = await _rootAssetOperation.ToUniTask(progress, cancellationToken: cancellationToken, cancelImmediately: true);
                InitializeRoot(rootPrefab);
                progress?.Report(1f);
            }
            catch
            {
                ReleaseFailedInitialization();
                throw;
            }
        }

        private void InitializeRoot(GameObject rootPrefab)
        {
            if (rootPrefab == null)
            {
                _rootAssetOperation.Release();
                _rootAssetOperation = null;
                throw new InvalidOperationException("UI root asset [Global/UIRoot] returned null.");
            }

            var rootGameObject = UnityEngine.Object.Instantiate(rootPrefab);
            try
            {
                rootGameObject.name = "[UIRoot]";
                rootGameObject.hideFlags = HideFlags.NotEditable;

                if (!rootGameObject.TryGetComponent<UIRoot>(out _root))
                {
                    throw new InvalidOperationException("UI root asset [Global/UIRoot] does not contain a UIRoot component.");
                }

                var layerRoots = BuildLayerRoots(_root);
                ValidateRootRendering(_root);
                BindInputModule(_root);
                var persistentSceneService = this.GetService<IPersistentSceneService>();
                var cameraSystem = this.GetSystem<CameraSystem>();
                var extensionContainer = new UIViewExtensionContainer(
                    new UIViewInputBlockExtension(_inputService),
                    new UIViewNavigationExtension(_inputService, _root),
                    new UIViewClosePopupExtension(),
                    new UIViewHideHUDExtension(),
                    new UIViewHideMainCameraExtension(cameraSystem),
                    new UIViewBlurExtension(_root));
                _viewPrefabCache = new UIViewPrefabCache(_assetService);
                _viewHost = new UIViewHost(_viewPrefabCache, layerRoots, UILayer.View.ToLayerId(), extensionContainer);
                _backgroundCameraToken = cameraSystem.AddOverlayCamera(_root.BackgroundCamera);
                _foregroundCameraToken = cameraSystem.AddOverlayCamera(_root.ForegroundCamera);
                persistentSceneService.MoveToPersistent(rootGameObject);
            }
            catch
            {
                _viewHost?.Shutdown();
                _viewHost = null;
                _viewPrefabCache?.Shutdown();
                _viewPrefabCache = null;
                ReleaseInputModule();
                ReleaseOverlayCameraTokens();
                _root = null;
                UnityEngine.Object.DestroyImmediate(rootGameObject);
                _rootAssetOperation.Release();
                _rootAssetOperation = null;
                throw;
            }
        }

        protected override void OnShutdown()
        {
            _viewHost?.Shutdown();
            _viewHost = null;
            _viewPrefabCache?.Shutdown();
            _viewPrefabCache = null;
            _usedViewPrefabLocations.Clear();
            ReleaseInputModule();
            ReleaseOverlayCameraTokens();

            _rootAssetOperation?.Release();
            _rootAssetOperation = null;

            if (_root != null)
            {
                UnityEngine.Object.DestroyImmediate(_root.gameObject);
                _root = null;
            }

            _assetService = null;
            _inputService = null;

            base.OnShutdown();
        }

        public UIViewToken GetViewToken<TView>() where TView : UIView
        {
            ThrowIfNotInitialized();
            return _viewHost.GetViewToken<TView>();
        }

        public UIViewToken OpenView(UIViewData data)
        {
            ThrowIfNotInitialized();
            return _viewHost.OpenView(data);
        }

        public void CloseAll(UILayer layer, bool immediately = false) => _viewHost?.CloseAll(layer.ToLayerId(), immediately);

        public void CloseAll(bool immediately = false) => _viewHost?.CloseAll(immediately);

        public void TrimUnusedViewAssets()
        {
            ThrowIfNotInitialized();
            _usedViewPrefabLocations.Clear();
            _viewHost.CollectUsedPrefabLocations(_usedViewPrefabLocations);
            _viewPrefabCache.TrimUnused(_usedViewPrefabLocations);
            _usedViewPrefabLocations.Clear();
        }

        private static Dictionary<UILayerId, UILayerRoots> BuildLayerRoots(UIRoot root)
        {
            var layers = (UILayer[])Enum.GetValues(typeof(UILayer));
            var configs = root.Layers;
            if (configs.Length != layers.Length)
            {
                throw new InvalidOperationException($"UI root requires exactly [{layers.Length}] layer configurations, but received [{configs.Length}].");
            }

            var layerRoots = new Dictionary<UILayerId, UILayerRoots>(layers.Length);
            var sortingOrders = new HashSet<int>();
            for (var index = 0; index < configs.Length; index++)
            {
                var config = configs[index];
                if (config == null)
                {
                    throw new InvalidOperationException($"UI root layer configuration at index [{index}] is null.");
                }

                if (!Enum.IsDefined(typeof(UILayer), config.Layer))
                {
                    throw new InvalidOperationException($"UI root layer configuration at index [{index}] has invalid layer [{config.Layer}].");
                }

                if (config.BackgroundRoot == null || config.ForegroundRoot == null || config.BackgroundRoot == config.ForegroundRoot)
                {
                    throw new InvalidOperationException($"UI root layer [{config.Layer}] requires distinct background and foreground roots.");
                }

                if (!layerRoots.TryAdd(config.LayerId, new UILayerRoots(config.BackgroundRoot, config.ForegroundRoot)))
                {
                    throw new InvalidOperationException($"UI root layer [{config.Layer}] is configured more than once.");
                }

                if (!sortingOrders.Add(config.SortingOrder))
                {
                    throw new InvalidOperationException($"UI root sorting order [{config.SortingOrder}] is configured more than once.");
                }
            }

            foreach (var layer in layers)
            {
                if (!layerRoots.ContainsKey(layer.ToLayerId()))
                {
                    throw new InvalidOperationException($"UI root layer [{layer}] is not configured.");
                }
            }

            return layerRoots;
        }

        private static void ValidateRootRendering(UIRoot root)
        {
            if (root.BackgroundCamera == null || root.ForegroundCamera == null || root.BackgroundCamera == root.ForegroundCamera)
            {
                throw new InvalidOperationException("UI root requires distinct background and foreground cameras.");
            }

            if (root.BlurFeature == null)
            {
                throw new InvalidOperationException("UI root does not reference a Kawase blur renderer feature.");
            }

            root.BackgroundCamera.enabled = true;
            root.ForegroundCamera.enabled = false;
        }

        private void BindInputModule(UIRoot root)
        {
            var inputModule = root.InputModule;
            if (inputModule == null)
            {
                throw new InvalidOperationException("UI root does not reference an InputSystemUIInputModule.");
            }

            inputModule.enabled = false;
            _inputModuleToken = _inputService.BindUIInputModule(inputModule);
            _inputModule = inputModule;
            inputModule.enabled = true;
        }

        private void ReleaseInputModule()
        {
            if (_inputModule != null)
            {
                _inputModule.enabled = false;
            }

            _inputModuleToken.Cancel();
            _inputModuleToken = default;
            _inputModule = null;
        }

        private void ReleaseOverlayCameraTokens()
        {
            _foregroundCameraToken.Cancel();
            _foregroundCameraToken = default;
            _backgroundCameraToken.Cancel();
            _backgroundCameraToken = default;
        }

        private void ThrowIfNotInitialized()
        {
            if (!IsInitialized)
            {
                throw new InvalidOperationException("UISystem must be initialized before opening or reading views.");
            }
        }

        private void ReleaseFailedInitialization()
        {
            _rootAssetOperation?.Release();
            _rootAssetOperation = null;
        }
    }
}
