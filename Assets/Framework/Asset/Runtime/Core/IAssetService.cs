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

        /// <summary>
        /// 同步加载资源，返回已完成的 operation，由调用方负责释放。
        /// </summary>
        /// <remarks>服务必须已初始化。调用会阻塞当前线程，支持范围受后端和平台限制；完成不代表成功，应检查 operation 状态。</remarks>
        IAssetAsyncOperation<T> LoadAssetSync<T>(string location) where T : UnityEngine.Object;

        IAssetAsyncOperation<T> LoadAssetAsync<T>(string location) where T : UnityEngine.Object;

        /// <summary>
        /// 按 Label 同步加载资源，返回已完成的 operation，由调用方负责释放。
        /// </summary>
        /// <remarks>服务必须已初始化。调用会阻塞当前线程，支持范围受后端和平台限制；完成不代表成功，应检查 operation 状态。</remarks>
        IAssetAsyncOperation<System.Collections.Generic.IList<T>> LoadAssetsSync<T>(string label) where T : UnityEngine.Object;

        IAssetAsyncOperation<System.Collections.Generic.IList<T>> LoadAssetsAsync<T>(string label) where T : UnityEngine.Object;
        ISceneAsyncOperation LoadSceneAsync(string location, LoadSceneMode mode = LoadSceneMode.Single, LocalPhysicsMode physicsMode = LocalPhysicsMode.None, bool activateOnLoad = true);
        
        UniTask UnloadUnusedAssets(CancellationToken cancellationToken = default);
    }
}
