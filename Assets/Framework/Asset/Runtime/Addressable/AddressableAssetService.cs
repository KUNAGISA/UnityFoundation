using Aoiro;
using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.AddressableAssets.ResourceLocators;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Gameplay.Asset
{
    public class AddressableAssetService : ServiceBase, IAssetService
    {
        private const string LogCategory = "AssetService";

        public bool IsInitialized => _defaultResourceLocator != null;

        private readonly bool _initializeSynchronously;
        private IResourceLocator _defaultResourceLocator = null;

        public AddressableAssetService() : this(false)
        {
        }

        public AddressableAssetService(bool initializeSynchronously)
        {
            _initializeSynchronously = initializeSynchronously;
        }

        protected override void OnInitialize()
        {
            if (!_initializeSynchronously)
            {
                return;
            }

            var initializeHandle = Addressables.InitializeAsync(false);
            try
            {
                SetDefaultResourceLocator(initializeHandle.WaitForCompletion());
            }
            finally
            {
                if (initializeHandle.IsValid())
                {
                    Addressables.Release(initializeHandle);
                }
            }
        }

        protected override void OnShutdown()
        {
            _defaultResourceLocator = null;
        }

        public async UniTask InitializeAsync(CancellationToken cancellationToken = default, IProgress<float> progress = null)
        {
            if (IsInitialized)
            {
                progress?.Report(1f);
                return;
            }

            var initializeHandle = Addressables.InitializeAsync(false);
            try
            {
                await initializeHandle.ToUniTask(progress, PlayerLoopTiming.EarlyUpdate, cancellationToken, true);
                SetDefaultResourceLocator(initializeHandle.Result);
                progress?.Report(1f);
            }
            finally
            {
                if (initializeHandle.IsValid())
                {
                    Addressables.Release(initializeHandle);
                }
            }
        }

        public IAssetAsyncOperation<T> LoadAssetAsync<T>(string location) where T : Object
        {
            ThrowIfNotInitialized();
            var handle = default(AsyncOperationHandle<T>);
            if (_defaultResourceLocator.Locate(location, typeof(T), out var locations))
            {
                handle = Addressables.LoadAssetAsync<T>(locations[0]);
            }
            else
            {
                Debug.LogError($"[{LogCategory}] Load Asset Failed. [{location}] cant not found.");
            }
            return new AddressableAssetAsyncOperation<T>(handle);
        }

        public IAssetAsyncOperation<System.Collections.Generic.IList<T>> LoadAssetsAsync<T>(string label) where T : Object
        {
            ThrowIfNotInitialized();
            var handle = default(AsyncOperationHandle<System.Collections.Generic.IList<T>>);
            if (_defaultResourceLocator.Locate(label, typeof(T), out var locations))
            {
                handle = Addressables.LoadAssetsAsync<T>(locations, null, true);
            }
            else
            {
                Debug.LogError($"[{LogCategory}] Load Assets Failed. Label [{label}] cant not found.");
            }
            return new AddressableAssetAsyncOperation<System.Collections.Generic.IList<T>>(handle);
        }

        public ISceneAsyncOperation LoadSceneAsync(string location, LoadSceneMode mode = LoadSceneMode.Single, LocalPhysicsMode physicsMode = LocalPhysicsMode.None, bool activateOnLoad = true)
        {
            ThrowIfNotInitialized();
            var handle = default(AsyncOperationHandle<SceneInstance>);
            if (_defaultResourceLocator.Locate(location, typeof(SceneInstance), out var locations))
            {
                handle = Addressables.LoadSceneAsync(locations[0], new LoadSceneParameters(mode, physicsMode), activateOnLoad);
            }
            else
            {
                Debug.LogError($"[{LogCategory}] Load Scene Failed. [{location}] cant not found.");
            }
            return new AddressableSceneAsyncOperation(handle);
        }

        public async UniTask UnloadUnusedAssets(CancellationToken cancellationToken = default)
        {
            await Resources.UnloadUnusedAssets().ToUniTask(timing: PlayerLoopTiming.EarlyUpdate, cancellationToken: cancellationToken);
        }

        private void ThrowIfNotInitialized()
        {
            if (!IsInitialized)
            {
                throw new InvalidOperationException("Addressables must be initialized before loading assets or scenes.");
            }
        }

        private void SetDefaultResourceLocator(IResourceLocator resourceLocator)
        {
            _defaultResourceLocator = resourceLocator ?? throw new InvalidOperationException("Addressables initialization returned no resource locator.");
        }
    }
}
