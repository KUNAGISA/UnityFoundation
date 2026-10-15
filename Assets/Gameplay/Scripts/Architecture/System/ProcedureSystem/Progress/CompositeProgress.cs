using System;

namespace Gameplay
{
    public class CompositeProgress : IReadOnlyProgress
    {
        private struct Entry
        {
            public Progress Progress;
            public float Weight;
        }

        public float Value
        {
            get
            {
                var totalWeight = 0f;
                var totalValue = 0f;

                for (var index = 0; index < _count; index++)
                {
                    ref readonly var entry = ref _entries[index];
                    totalWeight += entry.Weight;
                    totalValue += entry.Progress.Value * entry.Weight;
                }
                
                return totalWeight > 0f ? (totalValue / totalWeight) : 0f;
            }
        }

        private int _count = 0;
        private Entry[] _entries = new Entry[3];
        
        public Progress Add(float weight = 1f)
        {
            EnsureCapacity();

            ref var entry = ref _entries[_count++];
            entry.Weight = weight;
            entry.Progress = new Progress();

            return entry.Progress;
        }
        
        private void EnsureCapacity()
        {
            if (_count == _entries.Length)
            {
                Array.Resize(ref _entries, _entries.Length * 2);
            }
        }
    }
}