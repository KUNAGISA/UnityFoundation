using System.Collections.Generic;

namespace Gameplay
{
    public sealed class VariableStore
    {
        private readonly Dictionary<int, int> _ints = new();
        private readonly Dictionary<int, bool> _bools = new();

        public int GetInt(int id, int defaultValue) => _ints.TryGetValue(id, out var value) ? value : defaultValue;

        public bool SetInt(int id, int value)
        {
            if (_ints.TryGetValue(id, out var previous) && previous == value)
            {
                return false;
            }
            _ints[id] = value;
            return true;
        }

        public bool ClearInt(int id) => _ints.Remove(id);

        public bool GetBool(int id, bool defaultValue) => _bools.TryGetValue(id, out var value) ? value : defaultValue;

        public bool SetBool(int id, bool value)
        {
            if (_bools.TryGetValue(id, out var previous) && previous == value)
            {
                return false;
            }
            _bools[id] = value;
            return true;
        }

        public bool ClearBool(int id) => _bools.Remove(id);

        public (bool IntsChanged, bool BoolsChanged) Clear()
        {
            var changes = (_ints.Count > 0, _bools.Count > 0);
            _ints.Clear();
            _bools.Clear();
            return changes;
        }

        public (bool IntsChanged, bool BoolsChanged) Restore(VariableSaveData data)
        {
            var intsChanged = RestoreValues(_ints, data.IntValues);
            var boolsChanged = RestoreValues(_bools, data.BoolValues);
            return (intsChanged, boolsChanged);
        }

        public VariableSaveData Capture()
        {
            return new VariableSaveData
            {
                IntValues = new Dictionary<int, int>(_ints),
                BoolValues = new Dictionary<int, bool>(_bools),
            };
        }

        private static bool RestoreValues<T>(Dictionary<int, T> current, Dictionary<int, T> next)
        {
            if (EqualsValues(current, next))
            {
                return false;
            }
            current.Clear();
            foreach (var pair in next)
            {
                current.Add(pair.Key, pair.Value);
            }
            return true;
        }

        private static bool EqualsValues<T>(Dictionary<int, T> current, Dictionary<int, T> next)
        {
            if (current.Count != next.Count)
            {
                return false;
            }
            foreach (var pair in current)
            {
                if (!next.TryGetValue(pair.Key, out var value) || !EqualityComparer<T>.Default.Equals(pair.Value, value))
                {
                    return false;
                }
            }
            return true;
        }
    }
}
