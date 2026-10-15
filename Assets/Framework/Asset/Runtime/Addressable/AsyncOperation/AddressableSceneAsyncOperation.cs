using System;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;

namespace Gameplay.Asset
{
    internal class AddressableSceneAsyncOperation : ISceneAsyncOperation
    {
        public event Action<ISceneAsyncOperation> Completed
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
        
        public Scene Result => _handle.Result.Scene;
        public AsyncOperationStatus Status => (AsyncOperationStatus)_handle.Status;
        public Exception OperationException => _handle.OperationException;
        public float Progress => _handle.PercentComplete;
        public bool IsDone => _handle.IsDone;
        public bool IsValid => _handle.IsValid();

        private event Action<ISceneAsyncOperation> _completed;
        private AsyncOperationHandle<SceneInstance> _handle;

        public AddressableSceneAsyncOperation(AsyncOperationHandle<SceneInstance> handle)
        {
            _handle = handle;
            _handle.Completed += OnComplete;
        }
        
        public Scene WaitForComplete()
        {
            if (_handle.IsValid() && !_handle.IsDone)
            {
                _handle.WaitForCompletion();
            }
            return default;
        }

        public void Activate()
        {
            if (_handle.IsValid())
            {
                _handle.Result.ActivateAsync();
            }
        }

        public void Release()
        {
            if (_handle.IsValid())
            {
                Addressables.Release(_handle);
            }
        }
        
        public IAsyncOperation UnloadAsync(bool autoReleaseOperation = true)
        {
            if (_handle.IsValid())
            {
                var operation = new AddressableAsyncOperation(Addressables.UnloadSceneAsync(_handle, autoReleaseOperation));
                _handle = default;
                return operation;
            }
            return InvalidAsyncOperation.Default;
        }
        
        private void OnComplete(AsyncOperationHandle<SceneInstance> handle)
        {
            _completed?.Invoke(this);
        }
    }
}