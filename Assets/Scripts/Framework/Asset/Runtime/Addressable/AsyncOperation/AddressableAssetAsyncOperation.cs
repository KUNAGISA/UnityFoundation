using System;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Gameplay.Asset
{
    internal class AddressableAssetAsyncOperation<T> : IAssetAsyncOperation<T>
    {
        public event Action<IAssetAsyncOperation<T>> Completed
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

        public T Result => _handle.Result;
        
        private event Action<IAssetAsyncOperation<T>> _completed;
        private AsyncOperationHandle<T> _handle;

        public AddressableAssetAsyncOperation(AsyncOperationHandle<T> handle)
        {
            _handle = handle;
            _handle.Completed += OnComplete;
        }
        
        public T WaitForComplete()
        {
            if (_handle.IsValid())
            {
                return _handle.IsDone ? _handle.Result : _handle.WaitForCompletion();
            }
            return default;
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

        private void OnComplete(AsyncOperationHandle<T> handle)
        {
            _completed?.Invoke(this);
        }
    }
}