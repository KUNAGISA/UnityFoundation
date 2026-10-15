using System;
using System.Collections.Generic;

namespace Gameplay.Tags
{
    public sealed class GameplayTagCatalog
    {
        public int Count => _tags.Length;

        private readonly GameplayTag[] _tags;
        private readonly int[] _parents;
        private readonly Dictionary<GameplayTag, int> _indices;

        public GameplayTagCatalog(ReadOnlySpan<GameplayTagDefinition> definitions)
        {
            _tags = new GameplayTag[definitions.Length];
            _parents = new int[definitions.Length];
            _indices = new Dictionary<GameplayTag, int>(definitions.Length);
            for (var index = 0; index < definitions.Length; ++index)
            {
                var tag = definitions[index].Tag;
                if (!tag.IsValid || !_indices.TryAdd(tag, index))
                {
                    throw new ArgumentException($"Invalid or duplicate tag [{tag.Id}].", nameof(definitions));
                }
                _tags[index] = tag;
            }

            for (var index = 0; index < definitions.Length; ++index)
            {
                var parent = definitions[index].Parent;
                if (!parent.IsValid)
                {
                    _parents[index] = -1;
                }
                else if (!_indices.TryGetValue(parent, out _parents[index]))
                {
                    throw new ArgumentException($"Missing parent [{parent.Id}] of tag [{_tags[index].Id}].", nameof(definitions));
                }
            }

            var visits = new byte[Count];
            for (var index = 0; index < Count; ++index)
            {
                var cursor = index;
                while (cursor >= 0 && visits[cursor] == 0)
                {
                    visits[cursor] = 1;
                    cursor = _parents[cursor];
                }
                if (cursor >= 0 && visits[cursor] == 1)
                {
                    throw new ArgumentException($"Tag hierarchy contains a cycle at [{_tags[cursor].Id}].", nameof(definitions));
                }
                cursor = index;
                while (cursor >= 0 && visits[cursor] == 1)
                {
                    visits[cursor] = 2;
                    cursor = _parents[cursor];
                }
            }
        }

        public bool Contains(GameplayTag tag) => _indices.ContainsKey(tag);

        public GameplayTag GetParent(GameplayTag tag)
        {
            var parent = GetParentIndex(RequireIndex(tag));
            return parent < 0 ? default : _tags[parent];
        }

        private int GetParentIndex(int index) => _parents[index];

        private int RequireIndex(GameplayTag tag)
        {
            if (!_indices.TryGetValue(tag, out var index))
            {
                throw new ArgumentException($"Unknown tag [{tag.Id}].", nameof(tag));
            }
            return index;
        }
    }
}
