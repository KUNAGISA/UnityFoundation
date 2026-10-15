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
        internal IReadOnlyList<UIViewEntry> Entries => _entries;
        internal bool IsRefreshingVisibility { get; set; }
        internal bool IsVisibilityDirty { get; set; }

        internal UIViewEntry TopEntry
        {
            get
            {
                for (var index = _entries.Count - 1; index >= 0; index--)
                {
                    if (!_entries[index].IsCloseRequested)
                    {
                        return _entries[index];
                    }
                }

                return null;
            }
        }

        private readonly List<UIViewEntry> _entries = new();
        private readonly HashSet<UIViewToken> _visibilityBlockers = new();
        private readonly UILayerRoots _roots;

        internal UILayer(UILayerRoots roots)
        {
            _roots = roots;
        }

        internal bool SupportsRoot(UILayerRoot root) => _roots.TryGetRoot(root, out _);

        internal bool TrySetRoot(UIViewEntry entry, UILayerRoot root)
        {
            if (!_roots.TryGetRoot(root, out var target))
            {
                return false;
            }

            var transform = entry.View.transform;
            if (transform.parent == target)
            {
                return true;
            }

            transform.SetParent(target, false);
            ApplySiblingOrder(entry);
            return true;
        }

        internal void RegisterEntry(UIViewEntry entry)
        {
            _entries.Add(entry);
        }

        internal bool RemoveEntry(UIViewEntry entry)
        {
            return _entries.Remove(entry);
        }

        internal void CommitOpenedEntry(UIViewEntry entry)
        {
            ApplySiblingOrder(entry);
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

        internal void CollectVisibilityChanges(List<UIViewVisibilityChange> changes)
        {
            var targetVisible = _visibilityBlockers.Count == 0;
            for (var index = _entries.Count - 1; index >= 0; index--)
            {
                var entry = _entries[index];
                if (entry.State != UIViewEntryState.Opened)
                {
                    continue;
                }

                if (entry.IsTargetVisible != targetVisible)
                {
                    changes.Add(new UIViewVisibilityChange(entry, targetVisible));
                }

                if (targetVisible && entry.HideViewsBelow)
                {
                    targetVisible = false;
                }
            }
        }

        internal void Shutdown()
        {
            IsVisibilityDirty = false;
            IsRefreshingVisibility = false;
            _entries.Clear();
            _visibilityBlockers.Clear();
        }

        private void ApplySiblingOrder(UIViewEntry entry)
        {
            // 一个逻辑 Layer 可以跨两个物理 Root；只与同父节点的最近邻居比较，保持 Entry 顺序稳定。
            var entryIndex = _entries.IndexOf(entry);
            var entryTransform = entry.View.transform;
            var entryParent = entryTransform.parent;
            var currentSiblingIndex = entryTransform.GetSiblingIndex();
            for (var index = entryIndex - 1; index >= 0; index--)
            {
                var previousView = _entries[index].View;
                if (previousView == null || previousView.transform.parent != entryParent)
                {
                    continue;
                }

                var previousSiblingIndex = previousView.transform.GetSiblingIndex();
                if (currentSiblingIndex < previousSiblingIndex)
                {
                    // 当前节点移出后，前驱索引会减一；使用移动前的前驱索引即可插入其后。
                    entryTransform.SetSiblingIndex(previousSiblingIndex);
                    return;
                }

                break;
            }

            for (var index = entryIndex + 1; index < _entries.Count; index++)
            {
                var nextView = _entries[index].View;
                if (nextView == null || nextView.transform.parent != entryParent)
                {
                    continue;
                }

                var nextSiblingIndex = nextView.transform.GetSiblingIndex();
                if (currentSiblingIndex > nextSiblingIndex)
                {
                    entryTransform.SetSiblingIndex(nextSiblingIndex);
                }

                return;
            }
        }
    }
}
