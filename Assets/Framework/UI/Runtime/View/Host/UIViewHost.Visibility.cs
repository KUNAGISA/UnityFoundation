namespace Gameplay.UI
{
    public sealed partial class UIViewHost
    {
        public void AddLayerVisibilityBlocker(UILayerId layerId, UIViewToken owner)
        {
            var layer = GetLayer(layerId);
            if (layer.AddVisibilityBlocker(owner))
            {
                RequestVisibilityRefresh(layer);
            }
        }

        public void RemoveLayerVisibilityBlocker(UILayerId layerId, UIViewToken owner)
        {
            var layer = GetLayer(layerId);
            if (layer.RemoveVisibilityBlocker(owner))
            {
                RequestVisibilityRefresh(layer);
            }
        }

        public bool SupportsRoot(UILayerId layerId, UILayerRoot root)
        {
            return _layers.TryGetValue(layerId, out var layer) && layer.SupportsRoot(root);
        }

        public bool TrySetRoot(UIViewToken token, UILayerRoot root)
        {
            if (_isShuttingDown || !_entries.TryGetValue(token, out var entry) || entry.State >= UIViewEntryState.Releasing || entry.View == null)
            {
                return false;
            }

            return _layers[entry.LayerId].TrySetRoot(entry, root);
        }

        private void RequestVisibilityRefresh(UILayer layer)
        {
            layer.IsVisibilityDirty = true;
            if (_isShuttingDown || layer.IsRefreshingVisibility)
            {
                return;
            }

            layer.IsRefreshingVisibility = true;
            var changes = _visibilityChangeListPool.Get();
            try
            {
                while (layer.IsVisibilityDirty)
                {
                    // Extension 可以重入改变 Layer；一旦再次标脏就放弃当前快照并重新计算。
                    layer.IsVisibilityDirty = false;
                    changes.Clear();
                    layer.CollectVisibilityChanges(changes);
                    for (var index = 0; index < changes.Count; index++)
                    {
                        var change = changes[index];
                        ApplyVisibility(change.Entry, change.Visible);
                        if (layer.IsVisibilityDirty)
                        {
                            break;
                        }
                    }
                }
            }
            finally
            {
                _visibilityChangeListPool.Release(changes);
                layer.IsRefreshingVisibility = false;
            }
        }

        private void ApplyVisibility(UIViewEntry entry, bool visible)
        {
            if (entry.State != UIViewEntryState.Opened)
            {
                return;
            }

            if (visible)
            {
                if (entry.VisibilityState is UIViewVisibilityState.Hidden or UIViewVisibilityState.Hiding)
                {
                    StartShow(entry);
                }
                return;
            }

            if (entry.VisibilityState is UIViewVisibilityState.Showing or UIViewVisibilityState.Shown)
            {
                StartHide(entry);
            }
        }

        private void StartShow(UIViewEntry entry)
        {
            if (!entry.TryBeginShow())
            {
                return;
            }

            NotifyBeforeShow(entry);
            // Extension 回调允许重入 Close、Refresh 或可见性重算，返回后必须重新确认当前过渡。
            if (!entry.IsShowing)
            {
                return;
            }

            entry.View.Show();
        }

        internal void CompleteShow(UIViewToken token)
        {
            if (!_entries.TryGetValue(token, out var entry) || !entry.TryCompleteShow(out var completionSource))
            {
                return;
            }

            NotifyShowCompleted(entry);
            completionSource?.TrySetResult();
        }

        private void StartHide(UIViewEntry entry, bool immediately = false)
        {
            // TryBeginHide 取消 Shown 等待时也可能同步重入，不能继续派发失效的隐藏。
            if (!entry.TryBeginHide() || !entry.IsHiding)
            {
                return;
            }

            NotifyBeforeHide(entry);
            if (!entry.IsHiding)
            {
                return;
            }

            entry.View.Hide(immediately);
        }

        internal void CompleteHide(UIViewToken token)
        {
            if (!_entries.TryGetValue(token, out var entry) || !entry.TryCompleteHide())
            {
                return;
            }

            if (entry.State == UIViewEntryState.Closing)
            {
                ReleaseEntry(entry);
            }
        }

        private void NotifyBeforeShow(UIViewEntry entry)
        {
            var context = CreateContext(entry, true);
            _extensionContainer?.BeforeShow(in context);
        }

        private void NotifyShowCompleted(UIViewEntry entry)
        {
            var context = CreateContext(entry, true);
            _extensionContainer?.ShowCompleted(in context);
        }

        private void NotifyBeforeHide(UIViewEntry entry)
        {
            var context = CreateContext(entry, false);
            _extensionContainer?.BeforeHide(in context);
        }
    }
}
