using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Gameplay.Asset
{
    internal class AsyncOperationConfiguredSource: IUniTaskSource, IPlayerLoopItem, ITaskPoolNode<AsyncOperationConfiguredSource>
    {
        private static TaskPool<AsyncOperationConfiguredSource> pool;
        
        private AsyncOperationConfiguredSource _nextNode;
        public ref AsyncOperationConfiguredSource NextNode => ref _nextNode;
        
        static AsyncOperationConfiguredSource()
        {
            TaskPool.RegisterSizeGetter(typeof(AsyncOperationConfiguredSource), () => pool.Size);
        }

        private readonly Action<IAsyncOperation> _completedCallback;
        
        private IAsyncOperation _operation;
        private CancellationToken _cancellationToken;
        private CancellationTokenRegistration _cancellationTokenRegistration;
        
        private IProgress<float> _progress;
        
        private bool _autoReleaseWhenCanceled;
        private bool _cancelImmediately;
        private bool _completed;
        
        private UniTaskCompletionSourceCore<AsyncUnit> _core;

        public static IUniTaskSource Create(IAsyncOperation operation, PlayerLoopTiming timing, IProgress<float> progress, CancellationToken cancellationToken, bool cancelImmediately, bool autoReleaseWhenCanceled, out short token)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                if (autoReleaseWhenCanceled)
                {
                    operation.Release();
                }
                return AutoResetUniTaskCompletionSource.CreateFromCanceled(cancellationToken, out token);
            }

            if (!pool.TryPop(out var result))
            {
                result = new AsyncOperationConfiguredSource();
            }

            result._operation = operation;
            result._progress = progress;
            result._cancellationToken = cancellationToken;
            result._cancelImmediately = cancelImmediately;
            result._autoReleaseWhenCanceled = autoReleaseWhenCanceled;
            result._completed = false;
                
            if (cancelImmediately && cancellationToken.CanBeCanceled)
            {
                result._cancellationTokenRegistration = cancellationToken.RegisterWithoutCaptureExecutionContext(static state =>
                {
                    var promise = (AsyncOperationConfiguredSource)state;
                    if (promise._autoReleaseWhenCanceled && promise._operation.IsValid)
                    {
                        promise._operation.Release();
                    }
                    promise._core.TrySetCanceled(promise._cancellationToken);
                }, result);
            }

            TaskTracker.TrackActiveTask(result, 3);

            PlayerLoopHelper.AddAction(timing, result);

            operation.Completed += result._completedCallback;

            token = result._core.Version;
            return result;
        }

        private AsyncOperationConfiguredSource()
        {
            _completedCallback = HandleCompleted;
        }
        
        public void GetResult(short token)
        {
            try
            {
                _core.GetResult(token);
            }
            finally
            {
                if (!(_cancelImmediately && _cancellationToken.IsCancellationRequested))
                {
                    TryReturn();
                }
                else
                {
                    TaskTracker.RemoveTracking(this);
                }
            }
        }

        public UniTaskStatus UnsafeGetStatus()
        {
            return _core.UnsafeGetStatus();
        }

        public UniTaskStatus GetStatus(short token)
        {
            return _core.GetStatus(token);
        }

        public void OnCompleted(Action<object> continuation, object state, short token)
        {
            _core.OnCompleted(continuation, state, token);
        }

        public bool MoveNext()
        {
            if (_completed)
            {
                return false;
            }

            if (_cancellationToken.IsCancellationRequested)
            {
                _completed = true;
                if (_autoReleaseWhenCanceled && _operation.IsValid)
                {
                    _operation.Release();
                }
                _core.TrySetCanceled(_cancellationToken);
                return false;
            }

            if (_progress != null && _operation.IsValid)
            {
                _progress.Report(_operation.Progress);
            }

            return true;
        }
        
        private bool TryReturn()
        {
            TaskTracker.RemoveTracking(this);
            _core.Reset();
            _operation = null;
            _progress = null;
            _cancellationToken = CancellationToken.None;
            _cancellationTokenRegistration.Dispose();
            return pool.TryPush(this);
        }
        
        private void HandleCompleted(IAsyncOperation operation)
        {
            if (_operation.IsValid)
            {
                _operation.Completed -= _completedCallback;
            }

            if (_completed)
            {
                return;
            }
                
            _completed = true;
            _progress?.Report(_operation.Progress);
            
            if (_cancellationToken.IsCancellationRequested)
            {
                if (_autoReleaseWhenCanceled && _operation.IsValid)
                {
                    _operation.Release();
                }
                _core.TrySetCanceled(_cancellationToken);
            }
            else if (_operation.Status == AsyncOperationStatus.Failed)
            {
                _core.TrySetException(_operation.OperationException);
            }
            else
            {
                _core.TrySetResult(AsyncUnit.Default);
            }
        }
    }
    
    internal class AsyncOperationConfiguredSource<T> : IUniTaskSource<T>, IPlayerLoopItem, ITaskPoolNode<AsyncOperationConfiguredSource<T>>
    {
        private static TaskPool<AsyncOperationConfiguredSource<T>> pool;
        
        private AsyncOperationConfiguredSource<T> _nextNode;
        public ref AsyncOperationConfiguredSource<T> NextNode => ref _nextNode;
        
        static AsyncOperationConfiguredSource()
        {
            TaskPool.RegisterSizeGetter(typeof(AsyncOperationConfiguredSource<T>), () => pool.Size);
        }

        private readonly Action<IAsyncOperation<T>> _completedCallback;
        
        private IAsyncOperation<T> _operation;
        private CancellationToken _cancellationToken;
        private CancellationTokenRegistration _cancellationTokenRegistration;
        
        private IProgress<float> _progress;
        
        private bool _autoReleaseWhenCanceled;
        private bool _cancelImmediately;
        private bool _completed;
        
        private UniTaskCompletionSourceCore<T> _core;

        public static IUniTaskSource<T> Create(IAsyncOperation<T> operation, PlayerLoopTiming timing, IProgress<float> progress, CancellationToken cancellationToken, bool cancelImmediately, bool autoReleaseWhenCanceled, out short token)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                if (autoReleaseWhenCanceled)
                {
                    operation.Release();
                }
                return AutoResetUniTaskCompletionSource<T>.CreateFromCanceled(cancellationToken, out token);
            }

            if (!pool.TryPop(out var result))
            {
                result = new AsyncOperationConfiguredSource<T>();
            }

            result._operation = operation;
            result._progress = progress;
            result._cancellationToken = cancellationToken;
            result._cancelImmediately = cancelImmediately;
            result._autoReleaseWhenCanceled = autoReleaseWhenCanceled;
            result._completed = false;
                
            if (cancelImmediately && cancellationToken.CanBeCanceled)
            {
                result._cancellationTokenRegistration = cancellationToken.RegisterWithoutCaptureExecutionContext(static state =>
                {
                    var promise = (AsyncOperationConfiguredSource<T>)state;
                    if (promise._autoReleaseWhenCanceled && promise._operation.IsValid)
                    {
                        promise._operation.Release();
                    }
                    promise._core.TrySetCanceled(promise._cancellationToken);
                }, result);
            }

            TaskTracker.TrackActiveTask(result, 3);

            PlayerLoopHelper.AddAction(timing, result);

            operation.Completed += result._completedCallback;

            token = result._core.Version;
            return result;
        }

        private AsyncOperationConfiguredSource()
        {
            _completedCallback = HandleCompleted;
        }
        
        public T GetResult(short token)
        {
            try
            {
                return _core.GetResult(token);
            }
            finally
            {
                if (!(_cancelImmediately && _cancellationToken.IsCancellationRequested))
                {
                    TryReturn();
                }
                else
                {
                    TaskTracker.RemoveTracking(this);
                }
            }
        }

        void IUniTaskSource.GetResult(short token)
        {
            GetResult(token);
        }

        public UniTaskStatus UnsafeGetStatus()
        {
            return _core.UnsafeGetStatus();
        }

        public UniTaskStatus GetStatus(short token)
        {
            return _core.GetStatus(token);
        }

        public void OnCompleted(Action<object> continuation, object state, short token)
        {
            _core.OnCompleted(continuation, state, token);
        }

        public bool MoveNext()
        {
            if (_completed)
            {
                return false;
            }

            if (_cancellationToken.IsCancellationRequested)
            {
                _completed = true;
                if (_autoReleaseWhenCanceled && _operation.IsValid)
                {
                    _operation.Release();
                }
                _core.TrySetCanceled(_cancellationToken);
                return false;
            }

            if (_progress != null && _operation.IsValid)
            {
                _progress.Report(_operation.Progress);
            }

            return true;
        }
        
        private bool TryReturn()
        {
            TaskTracker.RemoveTracking(this);
            _core.Reset();
            _operation = null;
            _progress = null;
            _cancellationToken = CancellationToken.None;
            _cancellationTokenRegistration.Dispose();
            return pool.TryPush(this);
        }
        
        private void HandleCompleted(IAsyncOperation<T> operation)
        {
            if (_operation.IsValid)
            {
                _operation.Completed -= _completedCallback;
            }

            if (_completed)
            {
                return;
            }
                
            _completed = true;
            _progress?.Report(_operation.Progress);
            
            if (_cancellationToken.IsCancellationRequested)
            {
                if (_autoReleaseWhenCanceled && _operation.IsValid)
                {
                    _operation.Release();
                }
                _core.TrySetCanceled(_cancellationToken);
            }
            else if (_operation.Status == AsyncOperationStatus.Failed)
            {
                _core.TrySetException(_operation.OperationException);
            }
            else
            {
                _core.TrySetResult(operation.Result);
            }
        }
    }
}