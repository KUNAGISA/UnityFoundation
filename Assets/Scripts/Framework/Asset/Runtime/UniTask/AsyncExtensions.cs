using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Gameplay.Asset
{
    public static class AsyncExtensions
    {
        public static UniTask ToUniTask(this IAsyncOperation operation, IProgress<float> progress = null, PlayerLoopTiming timing = PlayerLoopTiming.EarlyUpdate, CancellationToken cancellationToken = default(CancellationToken), bool cancelImmediately = false, bool autoReleaseWhenCanceled = false)
        {
            if (!operation.IsValid)
            {
                throw new Exception("Attempting to use an invalid operation");
            }
            
            if (cancellationToken.IsCancellationRequested)
            {
                if (autoReleaseWhenCanceled)
                {
                    operation.Release();
                }
                return UniTask.FromCanceled(cancellationToken);
            }

            if (operation.IsDone)
            {
                return operation.Status == AsyncOperationStatus.Failed ? UniTask.FromException(operation.OperationException) : UniTask.CompletedTask;
            }

            return new UniTask(AsyncOperationConfiguredSource.Create(operation, timing, progress, cancellationToken, cancelImmediately, autoReleaseWhenCanceled, out var token), token);
        }

        public static UniTask.Awaiter GetAwaiter(this IAsyncOperation operation)
        {
            return operation.ToUniTask().GetAwaiter();
        }
        
        public static UniTask<T> ToUniTask<T>(this IAsyncOperation<T> operation, IProgress<float> progress = null, PlayerLoopTiming timing = PlayerLoopTiming.EarlyUpdate, CancellationToken cancellationToken = default(CancellationToken), bool cancelImmediately = false, bool autoReleaseWhenCanceled = false)
        {
            if (!operation.IsValid)
            {
                throw new Exception("Attempting to use an invalid operation");
            }

            if (cancellationToken.IsCancellationRequested)
            {
                if (autoReleaseWhenCanceled)
                {
                    operation.Release();
                }
                return UniTask.FromCanceled<T>(cancellationToken);
            }
            
            if (operation.IsDone)
            {
                return operation.Status == AsyncOperationStatus.Failed ? UniTask.FromException<T>(operation.OperationException) : UniTask.FromResult(operation.Result);
            }

            return new UniTask<T>(AsyncOperationConfiguredSource<T>.Create(operation, timing, progress, cancellationToken, cancelImmediately, autoReleaseWhenCanceled, out var token), token);
        }

        public static UniTask<T>.Awaiter GetAwaiter<T>(this IAsyncOperation<T> operation)
        {
            return operation.ToUniTask().GetAwaiter();
        }
    }
}