using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine.Pool;

namespace Gameplay.UI
{
    public sealed partial class UIViewHost
    {
        public bool IsShuttingDown => _isShuttingDown;

        private readonly Dictionary<UIViewToken, UIViewEntry> _entries = new();
        private readonly ObjectPool<List<UIViewEntry>> _entryListPool = new(() => new List<UIViewEntry>(), actionOnRelease: static entries => entries.Clear(), collectionCheck: true, defaultCapacity: 4, maxSize: 16);
        private readonly ObjectPool<List<UIViewVisibilityChange>> _visibilityChangeListPool = new(() => new List<UIViewVisibilityChange>(), actionOnRelease: static changes => changes.Clear(), collectionCheck: true, defaultCapacity: 4, maxSize: 16);
        // 单个 Entry 关闭只丢弃晚到结果；只有 Host Shutdown 会取消 Provider 请求。
        private readonly CancellationTokenSource _lifetimeCancellationSource = new();
        private readonly IUIViewInstanceProvider _instanceProvider;
        private readonly Dictionary<UILayerId, UILayer> _layers;
        private readonly UILayerId _defaultLayerId;
        private readonly UIViewExtensionContainer _extensionContainer;
        private bool _isShuttingDown;

        public UIViewHost(IUIViewInstanceProvider instanceProvider, IReadOnlyDictionary<UILayerId, UILayerRoots> layerRoots, UILayerId defaultLayerId, UIViewExtensionContainer extensionContainer = null)
        {
            _instanceProvider = instanceProvider ?? throw new ArgumentNullException(nameof(instanceProvider));
            _defaultLayerId = defaultLayerId;
            _extensionContainer = extensionContainer;

            if (layerRoots == null)
            {
                throw new ArgumentNullException(nameof(layerRoots));
            }

            if (!_defaultLayerId.IsSpecified)
            {
                throw new ArgumentException("UI view host default layer must be specified.", nameof(defaultLayerId));
            }

            if (layerRoots.Count == 0)
            {
                throw new ArgumentException("UI view host requires at least one layer root.", nameof(layerRoots));
            }

            _layers = new Dictionary<UILayerId, UILayer>(layerRoots.Count);
            foreach (var pair in layerRoots)
            {
                if (!pair.Key.IsSpecified)
                {
                    throw new ArgumentException("UI view host cannot register the unspecified layer.", nameof(layerRoots));
                }

                if (pair.Value.Primary == null)
                {
                    throw new ArgumentException($"UI view host layer [{pair.Key}] does not have a primary root.", nameof(layerRoots));
                }

                _layers.Add(pair.Key, new UILayer(pair.Value));
            }

            if (!_layers.ContainsKey(_defaultLayerId))
            {
                throw new ArgumentException($"UI view host default layer [{_defaultLayerId}] is not configured.", nameof(defaultLayerId));
            }
        }

        public UIViewToken GetViewToken<TView>() where TView : UIView
        {
            var token = UIViewToken.None;
            foreach (var entry in _entries.Values)
            {
                if (!entry.IsCloseRequested && entry.View is TView && token.ViewId < entry.Token.ViewId)
                {
                    token = entry.Token;
                }
            }

            return token;
        }

        internal TView GetView<TView>(UIViewToken token) where TView : UIView
        {
            if (_entries.TryGetValue(token, out var entry) && !entry.IsCloseRequested)
            {
                return entry.View as TView;
            }

            return null;
        }

        internal UniTask WaitForReadyAsync(UIViewToken token, CancellationToken cancellationToken = default)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return UniTask.FromCanceled(cancellationToken);
            }

            if (!_entries.TryGetValue(token, out var entry))
            {
                return UniTask.FromException(new InvalidOperationException($"UI view token [{token.ViewId}] is not available."));
            }

            var readyTask = entry.GetReadyTask();
            return cancellationToken.CanBeCanceled ? readyTask.AttachExternalCancellation(cancellationToken) : readyTask;
        }

        internal UniTask WaitForShownAsync(UIViewToken token, CancellationToken cancellationToken = default)
        {
            if (!_entries.TryGetValue(token, out var entry))
            {
                return cancellationToken.IsCancellationRequested ? UniTask.FromCanceled(cancellationToken) : UniTask.CompletedTask;
            }

            var shownTask = entry.GetShownTask();
            return cancellationToken.CanBeCanceled ? shownTask.AttachExternalCancellation(cancellationToken) : shownTask;
        }

        internal UniTask WaitForClosedAsync(UIViewToken token, CancellationToken cancellationToken = default)
        {
            if (!_entries.TryGetValue(token, out var entry))
            {
                return cancellationToken.IsCancellationRequested ? UniTask.FromCanceled(cancellationToken) : UniTask.CompletedTask;
            }

            var closedTask = entry.GetClosedTask();
            return cancellationToken.CanBeCanceled ? closedTask.AttachExternalCancellation(cancellationToken) : closedTask;
        }

        public void CollectUsedPrefabLocations(HashSet<string> locations)
        {
            if (locations == null)
            {
                throw new ArgumentNullException(nameof(locations));
            }

            foreach (var entry in _entries.Values)
            {
                locations.Add(entry.Location);
            }
        }

        public void Shutdown()
        {
            if (_isShuttingDown)
            {
                return;
            }

            _isShuttingDown = true;
            var entries = _entryListPool.Get();
            entries.AddRange(_entries.Values);
            foreach (var entry in entries)
            {
                RequestClose(entry, true);
            }

            // 先完成 Entry 的逻辑释放，避免取消回调重入同一批释放流程。
            _lifetimeCancellationSource.Cancel();
            _extensionContainer?.Shutdown(this);

            foreach (var layer in _layers.Values)
            {
                layer.Shutdown();
            }

            _entryListPool.Release(entries);
            _entryListPool.Dispose();
            _visibilityChangeListPool.Dispose();
            _lifetimeCancellationSource.Dispose();
        }

        private UIViewExtensionContext CreateContext(UIViewToken token, UIViewData data, UIView view, UILayerId layerId, UIViewOptionMask options, bool isVisible)
        {
            return new UIViewExtensionContext(this, token, data, view, layerId, options, isVisible);
        }

        private UIViewExtensionContext CreateContext(UIViewEntry entry, bool isVisible)
        {
            return CreateContext(entry.Token, entry.Data, entry.View, entry.LayerId, entry.Options, isVisible);
        }

        private UILayer GetLayer(UILayerId layerId)
        {
            if (!_layers.TryGetValue(layerId, out var layer))
            {
                throw new ArgumentOutOfRangeException(nameof(layerId), layerId, "UI view layer is not registered by the UI root.");
            }

            return layer;
        }
    }
}
