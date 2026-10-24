using System.Threading;
using Cysharp.Threading.Tasks;
using Aoiro;
using Gameplay.Asset;
using Luban;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Gameplay.Tables
{
    partial class TablesModel
    {
        private ByteBuf _cache = null;
        private IAssetService _assetService = null;
        
        protected override void OnInitialize()
        {
            _cache = new ByteBuf();
            _assetService = this.GetService<IAssetService>();
        }

        protected override void OnShutdown()
        {
            _cache.Clear();
            _cache = null;
            _assetService = null;
        }

        private async UniTask<ByteBuf> LoadBuffAsync(string path, CancellationToken  cancellationToken = default)
        {
            var operation = _assetService.LoadAssetAsync<TextAsset>(path);
            try
            {
                var text = await operation.ToUniTask(cancellationToken: cancellationToken);
                if (text == null)
                {
                    throw new System.InvalidOperationException($"Table asset [{path}] returned null.");
                }
                _cache.Replace(text.bytes);
                return _cache;
            }
            finally
            {
                operation.Release();
            }
        }

        private ByteBuf LoadBuff(string path, bool cleanup = false)
        {
            var operation = _assetService.LoadAssetSync<TextAsset>(path);
            try
            {
                _cache.Replace(operation.Result.bytes);
                return _cache;
            }
            finally
            {
                operation.Release();
            }
        }
        
        [Button("加载全部"), PropertyOrder(-100)]
        private void LoadAllTables()
        {
            LoadAll();
        }
    }
}
