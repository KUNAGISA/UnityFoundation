using System.Collections.Generic;

namespace Gameplay.Sequence
{
    internal sealed class AdvPortraitRegistry
    {
        private readonly Dictionary<int, AdvPortraitEntry> _entries = new Dictionary<int, AdvPortraitEntry>();
        private readonly AdvPortraitVisualState _defaultVisualState;

        internal IReadOnlyDictionary<int, AdvPortraitEntry> Entries => _entries;

        internal AdvPortraitRegistry()
        {
            _defaultVisualState.Reset();
        }

        internal void Clear() => _entries.Clear();

        internal AdvPortraitEntry GetOrAdd(int portraitId)
        {
            if (_entries.TryGetValue(portraitId, out var entry))
            {
                return entry;
            }

            entry = new AdvPortraitEntry(portraitId);
            _entries.Add(portraitId, entry);
            return entry;
        }

        internal bool TryGet(int portraitId, out AdvPortraitEntry entry) => _entries.TryGetValue(portraitId, out entry);

        internal bool TryGet(in AdvPortraitChannelToken token, AdvPortraitChannel channel, out AdvPortraitEntry entry)
        {
            entry = null;
            return token.Channel == channel && _entries.TryGetValue(token.PortraitId, out entry) && entry.IsValid(token);
        }

        internal bool IsTokenValid(in AdvPortraitChannelToken token)
        {
            return _entries.TryGetValue(token.PortraitId, out var entry) && entry.IsValid(token);
        }

        internal ref readonly AdvPortraitVisualState GetVisualState(int portraitId)
        {
            return ref _entries.TryGetValue(portraitId, out var entry) ? ref entry.VisualState : ref _defaultVisualState;
        }
    }
}
