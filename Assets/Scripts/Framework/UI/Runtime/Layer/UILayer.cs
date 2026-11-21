using System.Collections.Generic;
using UnityEngine;

namespace Gameplay.UI
{
    /// <summary>
    /// 维护单个 UI 层的界面顺序、物理 Root 和目标可见性。
    /// </summary>
    internal sealed class UILayer
    {
        internal Transform PrimaryRoot => _roots.Primary;
        internal IReadOnlyList<UIViewSession> Sessions => _sessions;

        internal UIViewSession TopSession
        {
            get
            {
                for (var index = _sessions.Count - 1; index >= 0; index--)
                {
                    if (!_sessions[index].IsCloseRequested)
                    {
                        return _sessions[index];
                    }
                }

                return null;
            }
        }

        private readonly List<UIViewSession> _sessions = new();
        private readonly HashSet<UIViewToken> _visibilityBlockers = new();
        private readonly UILayerRoots _roots;
        private readonly List<VisibilityChange> _visibilityChanges = new();
        private bool _isRefreshingVisibility;
        private bool _isVisibilityDirty;
        private bool _isVisibilitySuspended;

        internal UILayer(UILayerRoots roots)
        {
            _roots = roots;
        }

        internal bool SupportsRoot(UILayerRoot root) => _roots.TryGetRoot(root, out _);

        /// <summary>只切换物理父节点并恢复同 Root 内的顺序，不改变逻辑层归属与遮挡关系。</summary>
        internal bool TrySetRoot(UIViewSession session, UILayerRoot root)
        {
            if (!_roots.TryGetRoot(root, out var target))
            {
                return false;
            }

            var transform = session.View.transform;
            if (transform.parent == target)
            {
                return true;
            }

            transform.SetParent(target, false);
            UpdateSiblingOrder(session);
            return true;
        }

        /// <summary>按打开请求顺序登记会话，避免加载完成先后改变层内顺序。</summary>
        internal void RegisterSession(UIViewSession session)
        {
            _sessions.Add(session);
        }

        internal bool RemoveSession(UIViewSession session)
        {
            return _sessions.Remove(session);
        }

        internal bool AddVisibilityBlocker(UIViewToken owner)
        {
            if (!_visibilityBlockers.Add(owner))
            {
                Debug.LogError($"[UI] UI layer visibility blocker [{owner.ViewId}] was registered more than once.");
                return false;
            }

            return _visibilityBlockers.Count == 1;
        }

        internal bool RemoveVisibilityBlocker(UIViewToken owner)
        {
            if (!_visibilityBlockers.Remove(owner))
            {
                Debug.LogError($"[UI] UI layer visibility blocker [{owner.ViewId}] was not registered.");
                return false;
            }

            return _visibilityBlockers.Count == 0;
        }

        /// <summary>收集并应用目标可见性；同步回调使层变脏时丢弃剩余快照并重新计算，Shutdown 后停止重算。</summary>
        internal void RefreshVisibility()
        {
            _isVisibilityDirty = true;
            if (_isVisibilitySuspended || _isRefreshingVisibility)
            {
                return;
            }

            _isRefreshingVisibility = true;
            try
            {
                while (_isVisibilityDirty && !_isVisibilitySuspended)
                {
                    _isVisibilityDirty = false;
                    _visibilityChanges.Clear();
                    CollectVisibilityChanges();
                    for (var index = 0; index < _visibilityChanges.Count; index++)
                    {
                        var change = _visibilityChanges[index];
                        change.Session.SetVisible(change.Visible);
                        // 回调可改变本层或关闭 Host，旧快照不能继续应用。
                        if (_isVisibilityDirty || _isVisibilitySuspended)
                        {
                            break;
                        }
                    }
                }
            }
            finally
            {
                _visibilityChanges.Clear();
                _isRefreshingVisibility = false;
            }
        }

        /// <summary>关闭宿主前暂停可见性重算，防止逐个关闭时重新显示下层界面。</summary>
        internal void SuspendVisibility() => _isVisibilitySuspended = true;

        internal void Shutdown()
        {
            _isVisibilitySuspended = true;
            _isVisibilityDirty = false;
            _visibilityChanges.Clear();
            _sessions.Clear();
            _visibilityBlockers.Clear();
        }

        /// <summary>依据逻辑会话顺序修正同一物理 Root 下的节点顺序。</summary>
        internal void UpdateSiblingOrder(UIViewSession session)
        {
            // 一个逻辑 Layer 可以跨两个物理 Root；只与同父节点的最近邻居比较，保持 Session 顺序稳定。
            var sessionIndex = _sessions.IndexOf(session);
            var sessionTransform = session.View.transform;
            var sessionParent = sessionTransform.parent;
            var currentSiblingIndex = sessionTransform.GetSiblingIndex();
            for (var index = sessionIndex - 1; index >= 0; index--)
            {
                var previousView = _sessions[index].View;
                if (previousView == null || previousView.transform.parent != sessionParent)
                {
                    continue;
                }

                var previousSiblingIndex = previousView.transform.GetSiblingIndex();
                if (currentSiblingIndex < previousSiblingIndex)
                {
                    // 当前节点移出后，前驱索引会减一；使用移动前的前驱索引即可插入其后。
                    sessionTransform.SetSiblingIndex(previousSiblingIndex);
                    return;
                }

                break;
            }

            for (var index = sessionIndex + 1; index < _sessions.Count; index++)
            {
                var nextView = _sessions[index].View;
                if (nextView == null || nextView.transform.parent != sessionParent)
                {
                    continue;
                }

                var nextSiblingIndex = nextView.transform.GetSiblingIndex();
                if (currentSiblingIndex > nextSiblingIndex)
                {
                    sessionTransform.SetSiblingIndex(nextSiblingIndex);
                }

                return;
            }
        }

        /// <summary>从顶向下推导已打开界面的目标可见性；只由当前可见且声明遮挡的界面阻挡下层。</summary>
        private void CollectVisibilityChanges()
        {
            var targetVisible = _visibilityBlockers.Count == 0;
            for (var index = _sessions.Count - 1; index >= 0; index--)
            {
                var session = _sessions[index];
                if (session.Lifecycle != UIViewLifecycle.Opened)
                {
                    continue;
                }

                if (session.IsTargetVisible != targetVisible)
                {
                    _visibilityChanges.Add(new VisibilityChange(session, targetVisible));
                }

                if (targetVisible && session.HideViewsBelow)
                {
                    targetVisible = false;
                }
            }
        }

        private readonly struct VisibilityChange
        {
            public UIViewSession Session { get; }
            public bool Visible { get; }

            public VisibilityChange(UIViewSession session, bool visible)
            {
                Session = session;
                Visible = visible;
            }
        }
    }
}
