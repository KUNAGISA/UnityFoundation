using System.Collections.Generic;

namespace Gameplay
{
    public sealed class ConditionDependencyCollector
    {
        private readonly HashSet<ConditionChangeType> _types = new();

        public IReadOnlyCollection<ConditionChangeType> Types => _types;
        public bool IsEmpty => _types.Count == 0;

        public void Add(ConditionChangeType type)
        {
            if (type == ConditionChangeType.None)
            {
                return;
            }

            _types.Add(type);
        }

        public void Clear()
        {
            _types.Clear();
        }
    }
}
