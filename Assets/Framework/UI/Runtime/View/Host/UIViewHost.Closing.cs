using System;
using System.Collections.Generic;
using UnityEngine;

namespace Gameplay.UI
{
    public sealed partial class UIViewHost
    {
        internal void CloseView(UIViewToken token, bool immediately = false)
        {
            if (_entries.TryGetValue(token, out var entry))
            {
                RequestClose(entry, immediately);
            }
        }

        public void CloseAll(UILayerId layerId, bool immediately = false)
        {
            var layer = GetLayer(layerId);
            var entries = _entryListPool.Get();
            entries.AddRange(layer.Entries);
            ClosePooledEntries(entries, immediately);
        }

        public void CloseAll(bool immediately = false)
        {
            var entries = _entryListPool.Get();
            entries.AddRange(_entries.Values);
            ClosePooledEntries(entries, immediately);
        }

        private void ClosePooledEntries(List<UIViewEntry> entries, bool immediately)
        {
            try
            {
                foreach (var entry in entries)
                {
                    RequestClose(entry, immediately);
                }
            }
            finally
            {
                _entryListPool.Release(entries);
            }
        }

        private void RequestClose(UIViewEntry entry, bool immediately)
        {
            if (!entry.TryRequestClose(out var previousState))
            {
                return;
            }

            if (previousState == UIViewEntryState.Loading)
            {
                if (entry.View is null)
                {
                    ReleaseEntry(entry);
                }
                return;
            }

            if (previousState == UIViewEntryState.Closing)
            {
                if (immediately)
                {
                    CompleteImmediateClose(entry);
                }
                return;
            }

            if (immediately)
            {
                CompleteImmediateClose(entry);
                return;
            }

            switch (entry.VisibilityState)
            {
                case UIViewVisibilityState.Hidden:
                    ReleaseEntry(entry);
                    break;
                case UIViewVisibilityState.Showing:
                case UIViewVisibilityState.Shown:
                    StartHide(entry);
                    break;
                case UIViewVisibilityState.Hiding:
                    break;
            }
        }

        private void CompleteImmediateClose(UIViewEntry entry)
        {
            var startHide = entry.VisibilityState is UIViewVisibilityState.Showing or UIViewVisibilityState.Shown;
            var interruptHide = entry.VisibilityState == UIViewVisibilityState.Hiding;
            if (startHide)
            {
                if (!entry.TryBeginHide())
                {
                    return;
                }

                NotifyBeforeHide(entry);
                if (!entry.IsHiding)
                {
                    return;
                }
            }

            if (entry.View != null)
            {
                if (startHide && entry.View.IsShow)
                {
                    entry.View.HideAsync(true).GetAwaiter().GetResult();
                }
                else if (startHide || interruptHide)
                {
                    // 已经 Hiding 或尚未真正 Show 时只强制失效过渡，不重复派发 View.OnHide。
                    entry.View.ForceHidden();
                }
            }

            if ((startHide || interruptHide) && !entry.TryCompleteHide())
            {
                return;
            }

            ReleaseEntry(entry);
        }

        private void ReleaseEntry(UIViewEntry entry)
        {
            if (!entry.TryBeginRelease())
            {
                return;
            }

            var view = entry.View;
            if (view != null)
            {
                try
                {
                    view.Close();
                }
                catch (Exception exception)
                {
                    Debug.LogException(exception);
                }
            }

            var layer = _layers[entry.LayerId];
            if (layer.RemoveEntry(entry) && !_isShuttingDown)
            {
                RequestVisibilityRefresh(layer);
            }

            var releasedContext = CreateContext(entry, entry.IsTargetVisible);
            _extensionContainer?.Released(in releasedContext);

            if (view != null)
            {
                ReleaseInstance(entry.Location, view.gameObject);
            }

            _entries.Remove(entry.Token);
            // Provider Release 返回后，等待语义在所有逻辑记录与 Extension 清理完成后结束。
            entry.CompleteRelease();
        }

        private void ReleaseInstance(string location, GameObject instance)
        {
            if (instance == null)
            {
                return;
            }

            try
            {
                _instanceProvider.Release(location, instance);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }
    }
}
