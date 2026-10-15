using System;

namespace Gameplay.Asset
{
    public interface IAssetAsyncOperation<out T> : IAsyncOperation<T>
    {
        new event Action<IAssetAsyncOperation<T>> Completed;

        event Action<IAsyncOperation<T>> IAsyncOperation<T>.Completed
        {
            add => Completed += value;
            remove => Completed -= value;
        }
    }
}