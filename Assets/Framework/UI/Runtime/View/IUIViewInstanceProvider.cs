using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Gameplay.UI
{
    /// <summary>
    /// Creates and releases root instances owned by a <see cref="UIViewHost"/>.
    /// </summary>
    public interface IUIViewInstanceProvider
    {
        /// <remarks>
        /// A successful creation returns an inactive root instance parented to <paramref name="parent"/> and transfers its ownership to the Host.
        /// A provider must clean up any instance it creates but does not return because of cancellation or failure.
        /// </remarks>
        UniTask<GameObject> InstantiateAsync(string location, Transform parent, CancellationToken cancellationToken);

        /// <remarks>
        /// The Host calls this exactly once for every successfully returned instance. <paramref name="location"/> is the pooling key.
        /// Foreign instances and duplicate releases violate the provider contract.
        /// </remarks>
        void Release(string location, GameObject instance);
    }
}
