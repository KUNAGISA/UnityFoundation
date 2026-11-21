using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.U2D;

namespace Gameplay.Asset
{
    /// <summary>
    /// 资源加载器
    /// 作为资源加载域使用，一般用在界面等有域概念的地方
    /// <remarks>不使用前要调用<see cref="Clear"/>清空Loader持有的资源</remarks>
    /// </summary>
    public sealed class AssetLoader
    {
        private static readonly Regex SpriteAtlasRegex = new(@"^(.*?)\[(.*?)\]$");
        
        private readonly IAssetService _assetService;

        private CancellationTokenSource _cancellationTokenSource = null;
        private Dictionary<string, IAsyncOperation> _operations = null;

        internal AssetLoader(IAssetService assetService)
        {
            _assetService = assetService;
        }
        
        /// <summary>
        /// 清空当前加载批次
        /// <remarks>取消未完成的异步等待并释放已加载资源，清空后Loader可以继续使用</remarks>
        /// </summary>
        public void Clear()
        {
            if (_cancellationTokenSource != null)
            {
                _cancellationTokenSource.Cancel();
                _cancellationTokenSource.Dispose();
                _cancellationTokenSource = null;
            }

            if (_operations == null)
            {
                return;
            }

            foreach (var (_, operation) in _operations)
            {
                operation.Release();
            }
            _operations.Clear();
        }

        /// <summary>
        /// 异步加载资源
        /// </summary>
        /// <param name="location">资源路径</param>
        /// <typeparam name="T">资源类型</typeparam>
        /// <returns>目标资源</returns>
        public async UniTask<T> LoadAssetAsync<T>(string location) where T : Object
        {
            IAsyncOperation<T> operation = null;
            if (_operations != null && _operations.TryGetValue(location, out var cache))
            {
                if ((operation = cache as IAsyncOperation<T>) == null)
                {
                    Debug.LogError($"Load Asset Failed. [{location}] is not {typeof(T)}.");
                    return null;
                }
            }
            else
            {
                if ((operation = _assetService.LoadAssetAsync<T>(location)) == null)
                {
                    return null;
                }
                (_operations ??= new Dictionary<string, IAsyncOperation>())[location] = operation;
            }
            
            if (operation.IsDone)
            {
                return operation.Result;
            }

            _cancellationTokenSource ??= new CancellationTokenSource();
            return await operation.ToUniTask(cancellationToken: _cancellationTokenSource.Token);
        }

        /// <summary>
        /// 同步加载资源
        /// </summary>
        /// <param name="location">资源路径</param>
        /// <typeparam name="T">资源类型</typeparam>
        /// <returns>目标资源</returns>
        public T LoadAssetSync<T>(string location) where T : Object
        {
            IAsyncOperation<T> operation = null;
            if (_operations != null && _operations.TryGetValue(location, out var cache))
            {
                if ((operation = cache as IAsyncOperation<T>) == null)
                {
                    Debug.LogError($"Load Asset Failed. [{location}] is not {typeof(T)}.");
                    return null;
                }
            }
            else
            {
                if ((operation = _assetService.LoadAssetSync<T>(location)) == null)
                {
                    return null;
                }
                (_operations ??= new Dictionary<string, IAsyncOperation>())[location] = operation;
            }
            
            return operation.IsDone ? operation.Result : operation.WaitForComplete();
        }

        /// <summary>
        /// 异步从图集里加载图片
        /// </summary>
        /// <param name="location">图集路径</param>
        /// <param name="name">图片名字</param>
        /// <returns>目标图片</returns>
        public async UniTask<Sprite> LoadSpriteInAtlasAsync(string location, string name)
        {
            var atlas = await LoadAssetAsync<SpriteAtlas>(location);
            return atlas != null ? atlas.GetSprite(name) : null;
        }

        /// <summary>
        /// 同步从图集里加载图片
        /// </summary>
        /// <param name="location">图集路径</param>
        /// <param name="name">图片名字</param>
        /// <returns>目标图片</returns>
        public Sprite LoadSpriteInAtlasSync(string location, string name)
        {
            var atlas = LoadAssetSync<SpriteAtlas>(location);
            return atlas != null ? atlas.GetSprite(name) : null;
        }

        /// <summary>
        /// 异步从图集里加载图片，或者单独加载图片
        /// <remarks>路径格式为"路径[图片名]"表示从图集里获取</remarks>
        /// </summary>
        /// <param name="location">资源路径</param>
        /// <returns>图片资源</returns>
        public UniTask<Sprite> LoadSpriteAsync(string location)
        {
            var match = SpriteAtlasRegex.Match(location);
            return !match.Success ? LoadAssetAsync<Sprite>(location) : LoadSpriteInAtlasAsync(match.Groups[1].Value, match.Groups[2].Value);
        }

        /// <summary>
        /// 同步从图集里加载图片，或者单独加载图片
        /// <remarks>路径格式为"路径[图片名]"表示从图集里获取</remarks>
        /// </summary>
        /// <param name="location">资源路径</param>
        /// <returns>图片资源</returns>
        public Sprite LoadSpriteSync(string location)
        {
            var match = SpriteAtlasRegex.Match(location);
            return !match.Success ? LoadAssetSync<Sprite>(location) : LoadSpriteInAtlasSync(match.Groups[1].Value, match.Groups[2].Value);
        }
    }
}
