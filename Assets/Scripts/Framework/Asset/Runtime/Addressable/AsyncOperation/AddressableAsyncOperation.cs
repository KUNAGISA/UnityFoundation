using System;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Gameplay.Asset
{
    internal class AddressableAsyncOperation : IAsyncOperation
    {
        public event Action<IAsyncOperation> Completed
        {
            add
            {
                _completed += value;
                if (_handle.IsDone && _handle.IsValid())
                {
                    value(this);
                }
            }
            remove => _completed -= value;
        }
        
        public AsyncOperationStatus Status => (AsyncOperationStatus)_handle.Status;
        public Exception OperationException => _handle.OperationException;
        public float Progress => _handle.PercentComplete;
        public bool IsDone => _handle.IsDone;
        public bool IsValid => _handle.IsValid();

        private event Action<IAsyncOperation> _completed;
        private AsyncOperationHandle _handle;

        public AddressableAsyncOperation(AsyncOperationHandle handle)
        {
            _handle = handle;
            _handle.Completed += OnComplete;
        }
        
        public void WaitForComplete()
        {
            if (_handle.IsValid() && !_handle.IsDone)
            {
                _handle.WaitForCompletion();
            }
        }

        public void Release()
        {
            if (_handle.IsValid())
            {
                _handle.Completed -= OnComplete;
                Addressables.Release(_handle);
                _handle = default;
            }
        }
        
        private void OnComplete(AsyncOperationHandle handle)
        {
            _completed?.Invoke(this);
        }
    }
}