using System;
using JetBrains.Annotations;

namespace Gameplay.Asset
{
    public interface IAsyncOperation
    {
        /// <summary>
        /// 加载结束后回调
        /// </summary>
        event Action<IAsyncOperation> Completed;
        
        /// <summary>
        /// 当前状态
        /// </summary>
        AsyncOperationStatus Status { get; } 
        
        /// <summary>
        /// 最后一个操作错误信息
        /// </summary>
        Exception OperationException { get; }
        
        /// <summary>
        /// 加载进度 [0, 1]
        /// </summary>
        float Progress { get; }
        
        /// <summary>
        /// 是否已经完成
        /// </summary>
        bool IsDone { get; }
        
        /// <summary>
        /// 句柄是否有效
        /// </summary>
        bool IsValid { get; }
        
        /// <summary>
        /// 等待操作结束
        /// </summary>
        void WaitForComplete();
        
        /// <summary>
        /// 释放操作句柄
        /// </summary>
        void Release();
    }

    public interface IAsyncOperation<out T> : IAsyncOperation
    {
        new event Action<IAsyncOperation<T>> Completed;
        
        /// <summary>
        /// 异步结果
        /// </summary>
        T Result { get; }

        new T WaitForComplete();
        
        event Action<IAsyncOperation> IAsyncOperation.Completed
        {
            add => Completed += value;
            remove => Completed -= value;
        }
        
        void IAsyncOperation.WaitForComplete() => WaitForComplete();
    }
}