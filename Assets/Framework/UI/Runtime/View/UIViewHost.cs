using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine.Pool;

namespace Gameplay.UI
{
    /// <summary>管理界面注册、层配置与跨界面协调；单次打开的生命周期由 Session 持有。</summary>
    public sealed class UIViewHost
    {
        private readonly Dictionary<UIViewToken, UIViewSession> _sessions = new();
        private readonly ObjectPool<List<UIViewSession>> _sessionListPool = new(() => new List<UIViewSession>(), actionOnRelease: static sessions => sessions.Clear(), collectionCheck: true, defaultCapacity: 4, maxSize: 16);
        // 单个 Session 关闭只丢弃晚到结果；只有 Host Shutdown 会取消 Provider 请求。
        private readonly CancellationTokenSource _lifetimeCancellationSource = new();
        private readonly IUIViewInstanceProvider _instanceProvider;
        private readonly Dictionary<UILayerId, UILayer> _layers;
        private readonly UILayerId _defaultLayerId;
        private readonly UIViewExtensionContainer _extensionContainer;
        private bool _isShuttingDown;
        private bool _isStartingShutdown;
        private bool _isShutdownCompleted;

        public bool IsShuttingDown => _isShuttingDown;

        /// <summary>校验层配置并创建宿主；Provider 和 Extension 由宿主统一协调使用。</summary>
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

        /// <summary>请求打开界面；同层顶部的同资源单实例会复用 Token 并刷新 Data。</summary>
        /// <returns>操作与查询凭证；返回时不保证加载、初始化或显示已完成。</returns>
        /// <remarks>新 Session 快照打开配置；复用时只刷新 Data。配置校验和 BeforeOpen 的异常由调用方处理。</remarks>
        public UIViewToken OpenView(UIViewData data)
        {
            var layerId = ValidateOpenData(data);
            var context = new UIViewExtensionContext(this, UIViewToken.None, data, null, layerId, data.Options, false);
            _extensionContainer?.BeforeOpen(in context);
            var layer = _layers[layerId];
            var top = layer.TopSession;
            if (!data.AllowMultiple && top != null && string.Equals(top.Location, data.Location, StringComparison.Ordinal))
            {
                top.Refresh(data);
                return top.Token;
            }

            var session = new UIViewSession(this, layer, _instanceProvider, _extensionContainer, data, layerId);
            _sessions.Add(session.Token, session);
            layer.RegisterSession(session);
            // Session 内部处理打开失败、取消和晚到实例的所有权。
            session.OpenAsync(_lifetimeCancellationSource.Token).Forget();
            return session.Token;
        }

        /// <summary>查找已附着指定 View 类型且未请求关闭的最新 Token；找不到时返回 None，不保证界面已就绪。</summary>
        public UIViewToken GetViewToken<TView>() where TView : UIView
        {
            var token = UIViewToken.None;
            foreach (var session in _sessions.Values)
            {
                if (!session.IsCloseRequested && session.View is TView && token.ViewId < session.Token.ViewId)
                {
                    token = session.Token;
                }
            }

            return token;
        }

        /// <summary>关闭指定层的当前会话快照；immediately 为 true 时跳过隐藏动画，关闭回调中新开的会话不在该快照内。</summary>
        public void CloseAll(UILayerId layerId, bool immediately = false)
        {
            var layer = GetLayer(layerId);
            var sessions = _sessionListPool.Get();
            sessions.AddRange(layer.Sessions);
            ClosePooledSessions(sessions, immediately);
        }

        /// <summary>关闭宿主当前会话快照；immediately 为 true 时跳过隐藏动画，关闭回调中新开的会话不在该快照内。</summary>
        public void CloseAll(bool immediately = false)
        {
            var sessions = _sessionListPool.Get();
            sessions.AddRange(_sessions.Values);
            ClosePooledSessions(sessions, immediately);
        }

        /// <summary>为指定层登记遮挡所有者；首个所有者使本层隐藏，必须与移除操作配对。</summary>
        public void AddLayerVisibilityBlocker(UILayerId layerId, UIViewToken owner)
        {
            var layer = GetLayer(layerId);
            if (layer.AddVisibilityBlocker(owner))
            {
                layer.RefreshVisibility();
            }
        }

        /// <summary>移除指定所有者的层遮挡；最后一个所有者移除后重新推导层内可见性。</summary>
        public void RemoveLayerVisibilityBlocker(UILayerId layerId, UIViewToken owner)
        {
            var layer = GetLayer(layerId);
            if (layer.RemoveVisibilityBlocker(owner))
            {
                layer.RefreshVisibility();
            }
        }

        /// <summary>查询指定层是否配置了目标 Root；层或 Root 不存在时返回 false。</summary>
        public bool SupportsRoot(UILayerId layerId, UILayerRoot root)
        {
            return _layers.TryGetValue(layerId, out var layer) && layer.SupportsRoot(root);
        }

        /// <summary>将已附着的界面移至同一逻辑层的目标 Root 并恢复相对顺序；宿主关闭中、Token 不可用或 Root 不支持时返回 false。</summary>
        public bool TrySetRoot(UIViewToken token, UILayerRoot root)
        {
            if (_isShuttingDown || !_sessions.TryGetValue(token, out var session) || session.IsReleasingOrReleased || session.View == null)
            {
                return false;
            }

            return _layers[session.LayerId].TrySetRoot(session, root);
        }

        /// <summary>向传入集合追加仍注册的资源地址，包含加载和关闭中的会话；不会清空集合。</summary>
        public void CollectUsedPrefabLocations(HashSet<string> locations)
        {
            if (locations == null)
            {
                throw new ArgumentNullException(nameof(locations));
            }

            foreach (var session in _sessions.Values)
            {
                locations.Add(session.Location);
            }
        }

        /// <summary>停止新打开与可见性重算，立即关闭会话并取消加载请求；重复调用无效。</summary>
        /// <remarks>若在初始化栈内调用，共享扩展和层的清理推迟到最后一个会话注销后。</remarks>
        public void Shutdown()
        {
            if (_isShuttingDown)
            {
                return;
            }

            _isShuttingDown = true;
            _isStartingShutdown = true;
            foreach (var layer in _layers.Values)
            {
                layer.SuspendVisibility();
            }

            CloseAll(true);
            _lifetimeCancellationSource.Cancel();
            _isStartingShutdown = false;
            CompleteShutdownIfReady();
        }

        /// <summary>新实例初始化成功后，跨层关闭同资源的旧单实例；避免新实例打开失败时丢失旧界面。</summary>
        internal void ResolveSingleInstanceConflicts(UIViewSession session)
        {
            if (session.AllowMultiple)
            {
                return;
            }

            var conflicts = _sessionListPool.Get();
            try
            {
                foreach (var candidate in _sessions.Values)
                {
                    if (!ReferenceEquals(candidate, session) && !candidate.IsCloseRequested && string.Equals(candidate.Location, session.Location, StringComparison.Ordinal))
                    {
                        conflicts.Add(candidate);
                    }
                }

                foreach (var conflict in conflicts)
                {
                    conflict.Close(true);
                }
            }
            finally
            {
                _sessionListPool.Release(conflicts);
            }
        }

        /// <summary>移除会话注册，并在所有会话退出后完成延迟的 Shutdown。</summary>
        internal void UnregisterSession(UIViewSession session)
        {
            _sessions.Remove(session.Token);
            CompleteShutdownIfReady();
        }

        private UILayerId ValidateOpenData(UIViewData data)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            if (_isShuttingDown)
            {
                throw new InvalidOperationException("Cannot open a UI view while the UI view host is shutting down.");
            }

            if (string.IsNullOrWhiteSpace(data.Location))
            {
                throw new ArgumentException("UI view location cannot be empty.", nameof(data));
            }

            var layerId = data.LayerId.IsSpecified ? data.LayerId : _defaultLayerId;
            if (!_layers.ContainsKey(layerId))
            {
                throw new ArgumentOutOfRangeException(nameof(data), layerId, "UI view layer is not registered by the UI root.");
            }

            return layerId;
        }

        private UILayer GetLayer(UILayerId layerId)
        {
            if (!_layers.TryGetValue(layerId, out var layer))
            {
                throw new ArgumentOutOfRangeException(nameof(layerId), layerId, "UI view layer is not registered by the UI root.");
            }

            return layer;
        }

        /// <summary>使用快照隔离关闭回调对注册表的修改，并保证临时列表归还对象池。</summary>
        private void ClosePooledSessions(List<UIViewSession> sessions, bool immediately)
        {
            try
            {
                foreach (var session in sessions)
                {
                    session.Close(immediately);
                }
            }
            finally
            {
                _sessionListPool.Release(sessions);
            }
        }

        /// <summary>等关闭启动阶段与全部会话释放结束后，统一清理共享扩展、层和宿主资源。</summary>
        private void CompleteShutdownIfReady()
        {
            // 初始化回调内的 Shutdown 必须等该调用栈退出、Session 注销后再关闭共享依赖。
            if (!_isShuttingDown || _isStartingShutdown || _isShutdownCompleted || _sessions.Count != 0)
            {
                return;
            }

            _isShutdownCompleted = true;
            _extensionContainer?.Shutdown(this);
            foreach (var layer in _layers.Values)
            {
                layer.Shutdown();
            }

            _sessionListPool.Dispose();
            _lifetimeCancellationSource.Dispose();
        }
    }
}
