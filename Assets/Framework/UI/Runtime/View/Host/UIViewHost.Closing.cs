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
            switch (entry.RequestClose(immediately))
            {
                case UIViewEntry.CloseAction.Release:
                    ReleaseEntry(entry);
                    break;
                case UIViewEntry.CloseAction.Hide:
                    StartHide(entry);
                    break;
                case UIViewEntry.CloseAction.HideImmediately:
                    StartHide(entry, true);
                    break;
                case UIViewEntry.CloseAction.FinishHideImmediately:
                    if (entry.IsHiding)
                    {
                        if (entry.View != null)
                        {
                            entry.View.Hide(true);
                        }
                        else
                        {
                            ReleaseEntry(entry);
                        }
                    }
                    break;
            }
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
