using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Gameplay.Asset;
using Gameplay.UI;
using UnityEngine;

namespace Gameplay
{
    internal sealed class UIViewPrefabCache : IUIViewInstanceProvider
    {
        private readonly Dictionary<string, IAssetAsyncOperation<GameObject>> _operations = new(StringComparer.Ordinal);
        private readonly List<string> _unusedLocations = new();
        private readonly List<IAssetAsyncOperation<GameObject>> _retiredOperations = new();
        private readonly IAssetService _assetService;

        internal UIViewPrefabCache(IAssetService assetService)
        {
            _assetService = assetService ?? throw new ArgumentNullException(nameof(assetService));
        }

        public UniTask<GameObject> InstantiateAsync(string location, Transform parent, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (string.IsNullOrWhiteSpace(location))
            {
                throw new ArgumentException("UI prefab location cannot be empty.", nameof(location));
            }

            var prefab = GetPrefab(location);
            cancellationToken.ThrowIfCancellationRequested();

            GameObject instance = null;
            try
            {
                instance = UnityEngine.Object.Instantiate(prefab, parent, false);
                instance.SetActive(false);
                return UniTask.FromResult(instance);
            }
            catch
            {
                if (instance != null)
                {
                    UnityEngine.Object.Destroy(instance);
                }

                throw;
            }
        }

        public void Release(string location, GameObject instance)
        {
            if (string.IsNullOrWhiteSpace(location))
            {
                throw new ArgumentException("UI prefab location cannot be empty.", nameof(location));
            }

            if (instance == null)
            {
                throw new ArgumentNullException(nameof(instance));
            }

            UnityEngine.Object.Destroy(instance);
        }

        internal void TrimUnused(HashSet<string> usedLocations)
        {
            if (usedLocations == null)
            {
                throw new ArgumentNullException(nameof(usedLocations));
            }

            foreach (var pair in _operations)
            {
                if (!usedLocations.Contains(pair.Key))
                {
                    _unusedLocations.Add(pair.Key);
                }
            }

            for (var index = 0; index < _unusedLocations.Count; ++index)
            {
                var location = _unusedLocations[index];
                if (_operations.Remove(location, out var operation))
                {
                    operation.Release();
                }
            }
            _unusedLocations.Clear();
            ReleaseRetiredOperations();
        }

        internal void Shutdown()
        {
            foreach (var operation in _operations.Values)
            {
                operation.Release();
            }
            _operations.Clear();
            _unusedLocations.Clear();
            ReleaseRetiredOperations();
        }

        private GameObject GetPrefab(string location)
        {
            if (!_operations.TryGetValue(location, out var operation))
            {
                operation = _assetService.LoadAssetSync<GameObject>(location);
                _operations.Add(location, operation);
            }

            try
            {
                var prefab = operation.Result;
                if (operation.Status == AsyncOperationStatus.Failed)
                {
                    throw operation.OperationException ?? new InvalidOperationException($"UI prefab asset [{location}] failed to load.");
                }

                if (prefab == null)
                {
                    throw new InvalidOperationException($"UI prefab asset [{location}] returned null.");
                }

                return prefab;
            }
            catch
            {
                RetireFailedOperation(location, operation);
                throw;
            }
        }

        private void RetireFailedOperation(string location, IAssetAsyncOperation<GameObject> operation)
        {
            if (_operations.TryGetValue(location, out var cachedOperation) && ReferenceEquals(cachedOperation, operation))
            {
                _operations.Remove(location);
                _retiredOperations.Add(operation);
            }
        }

        private void ReleaseRetiredOperations()
        {
            for (var index = 0; index < _retiredOperations.Count; ++index)
            {
                _retiredOperations[index].Release();
            }
            _retiredOperations.Clear();
        }
    }
}
