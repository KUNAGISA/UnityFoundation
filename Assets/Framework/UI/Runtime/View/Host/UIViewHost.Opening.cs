using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Gameplay.UI
{
    public sealed partial class UIViewHost
    {
        public UIViewToken OpenView(UIViewData data)
        {
            var layerId = ValidateOpenData(data);
            var beforeOpenContext = CreateContext(UIViewToken.None, data, null, layerId, data.Options, false);
            _extensionContainer?.BeforeOpen(in beforeOpenContext);
            var layer = _layers[layerId];
            if (TryRefreshTopView(data, layer, out var reusedToken))
            {
                return reusedToken;
            }

            var entry = new UIViewEntry(this, data, layerId);
            _entries.Add(entry.Token, entry);
            layer.RegisterEntry(entry);

            OpenEntryAsync(entry, _lifetimeCancellationSource.Token).Forget();
            return entry.Token;
        }

        private bool TryRefreshTopView(UIViewData data, UILayer layer, out UIViewToken token)
        {
            token = UIViewToken.None;
            if (data.AllowMultiple)
            {
                return false;
            }

            var entry = layer.TopEntry;
            if (entry == null || !string.Equals(entry.Location, data.Location, StringComparison.Ordinal))
            {
                return false;
            }

            RequestRefresh(entry, data);
            token = entry.Token;
            return true;
        }

        private void RequestRefresh(UIViewEntry entry, UIViewData data)
        {
            entry.UpdateData(data);
            if (entry.State == UIViewEntryState.Opened)
            {
                DrainRefresh(entry, true);
            }
        }

        private async UniTask OpenEntryAsync(UIViewEntry entry, CancellationToken cancellationToken)
        {
            try
            {
                var instance = await _instanceProvider.InstantiateAsync(entry.Location, _layers[entry.LayerId].PrimaryRoot, cancellationToken);
                OpenInstantiatedEntry(entry, instance);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                if (!entry.IsReleasingOrReleased)
                {
                    ReleaseEntry(entry);
                }
            }
            catch (Exception exception)
            {
                if (!_isShuttingDown && !entry.IsReleasingOrReleased)
                {
                    FailOpenEntry(entry, exception);
                }
            }
        }

        private void OpenInstantiatedEntry(UIViewEntry entry, GameObject instance)
        {
            // 单个 Loading Entry 不取消共享请求；Provider 晚到的实例必须立即归还。
            if (entry.IsReleasingOrReleased)
            {
                ReleaseInstance(entry.Location, instance);
                return;
            }

            if (entry.IsCloseRequested)
            {
                ReleaseInstance(entry.Location, instance);
                ReleaseEntry(entry);
                return;
            }

            if (instance == null)
            {
                FailOpenEntry(entry, new InvalidOperationException($"UI instance provider returned null for [{entry.Location}]."));
                return;
            }

            try
            {
                if (!instance.TryGetComponent<UIView>(out var view))
                {
                    throw new InvalidOperationException($"UI instance [{entry.Location}] does not contain a {nameof(UIView)} component.");
                }

                entry.AttachView(view);
                ValidateViewData(entry, entry.Data);
                view.Bind(entry.Token);
                var initializeData = entry.Data;
                var initializeDataVersion = entry.DataVersion;
                view.Initialize(initializeData);
                entry.CommitDataVersion(initializeDataVersion);
                DrainRefresh(entry, false);

                if (entry.IsCloseRequested)
                {
                    ReleaseEntry(entry);
                    return;
                }

                EnforceSingleInstance(entry);
                if (entry.IsCloseRequested)
                {
                    ReleaseEntry(entry);
                    return;
                }

                entry.MarkOpened();
                var layer = _layers[entry.LayerId];
                layer.CommitOpenedEntry(entry);
                entry.CompleteReady();
                if (entry.State == UIViewEntryState.Opened)
                {
                    RequestVisibilityRefresh(layer);
                }
            }
            catch (Exception exception)
            {
                if (entry.View == null)
                {
                    ReleaseInstance(entry.Location, instance);
                }

                FailOpenEntry(entry, exception);
            }
        }

        private void DrainRefresh(UIViewEntry entry, bool notifyExtensions)
        {
            if (!entry.TryBeginRefresh())
            {
                return;
            }

            try
            {
                // Initialize/Refresh 可以重入更新 Data；每轮只提交捕获的版本，随后继续追到最新版本。
                while (entry.AppliedDataVersion != entry.DataVersion && !entry.IsCloseRequested)
                {
                    var targetData = entry.Data;
                    var targetVersion = entry.DataVersion;
                    ValidateViewData(entry, targetData);
                    entry.View.Refresh(targetData);
                    entry.CommitDataVersion(targetVersion);

                    if (notifyExtensions)
                    {
                        var context = CreateContext(entry, entry.IsTargetVisible);
                        _extensionContainer?.Refreshed(in context);
                    }
                }
            }
            finally
            {
                entry.EndRefresh();
            }
        }

        private void EnforceSingleInstance(UIViewEntry entry)
        {
            if (entry.AllowMultiple)
            {
                return;
            }

            var conflicts = _entryListPool.Get();
            try
            {
                foreach (var candidate in _entries.Values)
                {
                    if (ReferenceEquals(candidate, entry) || candidate.IsCloseRequested || !string.Equals(candidate.Location, entry.Location, StringComparison.Ordinal))
                    {
                        continue;
                    }

                    conflicts.Add(candidate);
                }
            }
            catch
            {
                _entryListPool.Release(conflicts);
                throw;
            }

            ClosePooledEntries(conflicts, true);
        }

        private void FailOpenEntry(UIViewEntry entry, Exception exception)
        {
            if (entry.IsReleasingOrReleased)
            {
                return;
            }

            Debug.LogException(exception);
            entry.FailOpen(exception);
            ReleaseEntry(entry);
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

        private static void ValidateViewData(UIViewEntry entry, UIViewData data)
        {
            if (!entry.View.Validate(data, out var reason))
            {
                throw new ArgumentException($"Invalid view data for [{entry.Location}]: {reason}");
            }
        }
    }
}
