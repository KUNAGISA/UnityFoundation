using System;

namespace Gameplay.Asset
{
    /// <summary>
    /// 无效操作句柄
    /// <remarks>无效操作需要返回值的可以用这个</remarks>
    /// </summary>
    internal sealed class InvalidAsyncOperation : IAsyncOperation
    {
        public static InvalidAsyncOperation Default { get; } = new();
        
        public event Action<IAsyncOperation> Completed
        {
            add { }
            remove { }
        }

        public AsyncOperationStatus Status => AsyncOperationStatus.Failed;
        public Exception OperationException => null;

        public float Progress => 0f;
        public bool IsDone => true;
        public bool IsValid => false;
        
        public void WaitForComplete() { }

        public void Release() { }
    }
}