using System;
using System.Collections.Generic;
using Aoiro;

namespace Gameplay.Tags
{
    public sealed class GameplayTagContainer : IDisposable
    {
        private sealed class Signals
        {
            public readonly Signal<GameplayTagChange> CountChanged = new();
            public readonly Signal<GameplayTagChange> PresenceChanged = new();
        }

        private const int MaxDispatchCount = 1024;

        private readonly GameplayTagCatalog _catalog;
        private readonly Dictionary<GameplayTag, int> _counts = new();
        private readonly Dictionary<GameplayTag, Signals> _signals = new();
        private readonly Dictionary<long, GameplayTag> _activeTokens = new();
        private readonly Queue<GameplayTagChange> _pendingChanges = new();
        private long _nextTokenId;
        private bool _isDispatching;
        private bool _isDisposed;

        public GameplayTagContainer(GameplayTagCatalog catalog)
        {
            _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
        }

        public int GetCount(GameplayTag tag) => _counts.GetValueOrDefault(tag, 0);
        public bool Has(GameplayTag tag) => GetCount(tag) > 0;

        public bool HasAll(ReadOnlySpan<GameplayTag> tags)
        {
            for (var index = 0; index < tags.Length; ++index)
            {
                if (!Has(tags[index]))
                {
                    return false;
                }
            }
            return true;
        }

        public bool HasAny(ReadOnlySpan<GameplayTag> tags)
        {
            for (var index = 0; index < tags.Length; ++index)
            {
                if (Has(tags[index]))
                {
                    return true;
                }
            }
            return false;
        }

        public void AddTag(GameplayTag tag, out GameplayTagToken token)
        {
            token = default;
            ThrowIfDisposed();
            RequireTag(tag);
            var tokenId = checked(_nextTokenId + 1);
            for (var cursor = tag; cursor.IsValid; cursor = _catalog.GetParent(cursor))
            {
                if (GetCount(cursor) == int.MaxValue)
                {
                    throw new OverflowException($"Tag [{cursor.Id}] count overflow.");
                }
            }

            _activeTokens.Add(tokenId, tag);
            _nextTokenId = tokenId;
            ChangeCounts(tag, 1);
            // Publish only after the caller owns the cancellation credential.
            token = new GameplayTagToken(this, tokenId);
            DispatchChanges();
        }

        public SubscriptionToken SubscribeCountChanged(GameplayTag tag, Action<GameplayTagChange> callback)
        {
            return RequireSignals(tag).CountChanged.Subscribe(callback);
        }

        public SubscriptionToken SubscribePresenceChanged(GameplayTag tag, Action<GameplayTagChange> callback)
        {
            return RequireSignals(tag).PresenceChanged.Subscribe(callback);
        }

        public void Clear()
        {
            if (_isDisposed)
            {
                return;
            }
            _activeTokens.Clear();
            foreach (var pair in _counts)
            {
                _pendingChanges.Enqueue(new GameplayTagChange(pair.Key, pair.Value, 0));
            }
            _counts.Clear();
            DispatchChanges();
        }

        public void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }
            _isDisposed = true;
            _activeTokens.Clear();
            _pendingChanges.Clear();
            _counts.Clear();
            foreach (var signals in _signals.Values)
            {
                signals.CountChanged.Clear();
                signals.PresenceChanged.Clear();
            }
            _signals.Clear();
        }

        internal void Cancel(long tokenId)
        {
            if (!_activeTokens.Remove(tokenId, out var tag))
            {
                return;
            }

            ChangeCounts(tag, -1);
            DispatchChanges();
        }

        private void ChangeCounts(GameplayTag tag, int delta)
        {
            // Queue the entire ancestry before any listener can mutate the container.
            for (var cursor = tag; cursor.IsValid; cursor = _catalog.GetParent(cursor))
            {
                var oldCount = GetCount(cursor);
                var newCount = oldCount + delta;
                if (newCount == 0)
                {
                    _counts.Remove(cursor);
                }
                else
                {
                    _counts[cursor] = newCount;
                }
                _pendingChanges.Enqueue(new GameplayTagChange(cursor, oldCount, newCount));
            }
        }

        private void DispatchChanges()
        {
            if (_isDispatching)
            {
                return;
            }
            _isDispatching = true;
            var dispatchCount = 0;
            try
            {
                while (!_isDisposed && _pendingChanges.Count > 0)
                {
                    if (++dispatchCount > MaxDispatchCount)
                    {
                        throw new InvalidOperationException($"GameplayTagContainer exceeded {MaxDispatchCount} changes in one dispatch cycle. Possible event loop detected.");
                    }
                    var change = _pendingChanges.Dequeue();
                    if (!_signals.TryGetValue(change.Tag, out var signals))
                    {
                        continue;
                    }
                    signals.CountChanged.Emit(change);
                    if (!_isDisposed && (change.OldCount == 0 || change.NewCount == 0))
                    {
                        signals.PresenceChanged.Emit(change);
                    }
                }
            }
            catch
            {
                _pendingChanges.Clear();
                throw;
            }
            finally
            {
                _isDispatching = false;
            }
        }

        private Signals RequireSignals(GameplayTag tag)
        {
            ThrowIfDisposed();
            RequireTag(tag);
            if (!_signals.TryGetValue(tag, out var signals))
            {
                signals = new Signals();
                _signals.Add(tag, signals);
            }
            return signals;
        }

        private void RequireTag(GameplayTag tag)
        {
            if (!_catalog.Contains(tag))
            {
                throw new ArgumentException($"Unknown tag [{tag.Id}].", nameof(tag));
            }
        }

        private void ThrowIfDisposed()
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException(nameof(GameplayTagContainer));
            }
        }
    }
}
