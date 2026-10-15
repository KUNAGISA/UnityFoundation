using System.Threading;
using System;
using Cysharp.Threading.Tasks;
using Aoiro;
using UnityEngine.SceneManagement;

namespace Gameplay.Asset
{
    public interface IAssetService : IService
    {
        bool IsInitialized { get; }

        UniTask InitializeAsync(CancellationToken cancellationToken = default, IProgress<float> progress = null);

        IAssetAsyncOperation<T> LoadAssetAsync<T>(string location) where T : UnityEngine.Object;
        IAssetAsyncOperation<System.Collections.Generic.IList<T>> LoadAssetsAsync<T>(string label) where T : UnityEngine.Object;
        ISceneAsyncOperation LoadSceneAsync(string location, LoadSceneMode mode = LoadSceneMode.Single, LocalPhysicsMode physicsMode = LocalPhysicsMode.None, bool activateOnLoad = true);
        
        UniTask UnloadUnusedAssets(CancellationToken cancellationToken = default);
    }
}
