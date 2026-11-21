using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;

namespace Gameplay
{
    internal sealed class InputModeStack
    {
        [Serializable]
        private readonly struct Entry
        {
            [field: ShowInInspector]
            internal long Id { get; }
            [field: ShowInInspector, InlineProperty]
            internal InputModeDefinition Definition { get; }

            internal Entry(long id, InputModeDefinition definition)
            {
                Id = id;
                Definition = definition;
            }
        }

        [ShowInInspector]
        private readonly List<Entry> _entries = new();
        private long _entryIdGeneration;

        internal long Add(InputModeDefinition mode)
        {
            if (!mode.IsValid)
            {
                throw new System.ArgumentException("Input mode must enable or disable at least one channel without conflicts.", nameof(mode));
            }

            var entryId = CreateEntryId();
            _entries.Add(new Entry(entryId, mode));
            _entries.Sort(CompareEntry);
            return entryId;
        }

        internal bool Remove(long entryId)
        {
            for (var index = 0; index < _entries.Count; index++)
            {
                if (_entries[index].Id != entryId)
                {
                    continue;
                }

                _entries.RemoveAt(index);
                return true;
            }
            return false;
        }

        internal void Clear()
        {
            _entries.Clear();
        }

        internal bool Resolve(InputChannelMask channel, bool defaultEnabled)
        {
            for (var index = _entries.Count - 1; index >= 0; index--)
            {
                if (_entries[index].Definition.TryResolve(channel, out var enabled))
                {
                    return enabled;
                }
            }

            return defaultEnabled;
        }

        private long CreateEntryId()
        {
            do
            {
                _entryIdGeneration = unchecked(_entryIdGeneration + 1);
            }
            while (_entryIdGeneration == 0);
            return _entryIdGeneration;
        }

        private static int CompareEntry(Entry left, Entry right)
        {
            var priorityComparison = left.Definition.Priority.CompareTo(right.Definition.Priority);
            return priorityComparison != 0 ? priorityComparison : left.Id.CompareTo(right.Id);
        }
    }
}
