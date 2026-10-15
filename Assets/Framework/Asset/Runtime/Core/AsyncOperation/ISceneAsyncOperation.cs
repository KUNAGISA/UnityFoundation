using System;
using UnityEngine.SceneManagement;

namespace Gameplay.Asset
{
    public interface ISceneAsyncOperation : IAsyncOperation<Scene>
    {
        new event Action<ISceneAsyncOperation> Completed;
        
        /// <summary>
        /// 激活场景
        /// </summary>
        void Activate();
        
        /// <summary>
        /// 异步卸载场景
        /// <remarks>卸载场景会同时销毁当前操作句柄 </remarks>
        /// </summary>
        /// <param name="autoReleaseOperation">是否自动释放卸载场景操作句柄</param>
        /// <returns>卸载场景操作句柄</returns>
        IAsyncOperation UnloadAsync(bool autoReleaseOperation = true);
        
        event Action<IAsyncOperation<Scene>> IAsyncOperation<Scene>.Completed
        {
            add => Completed += value;
            remove => Completed -= value;
        }
    }
}