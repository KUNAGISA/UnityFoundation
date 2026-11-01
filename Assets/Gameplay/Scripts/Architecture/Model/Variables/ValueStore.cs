using System.Collections.Generic;

namespace Gameplay
{
    public sealed class ValueStore
    {
        private readonly Dictionary<int, int> _variables = new();
        private readonly Dictionary<int, bool> _switches = new();

        public int GetVariable(int id, int defaultValue) => _variables.GetValueOrDefault(id, defaultValue);

        public bool SetVariable(int id, int value)
        {
            if (_variables.TryGetValue(id, out var previous) && previous == value)
            {
                return false;
            }
            _variables[id] = value;
            return true;
        }

        public bool ClearVariable(int id) => _variables.Remove(id);

        public bool ClearVariables()
        {
            var changed = _variables.Count > 0;
            _variables.Clear();
            return changed;
        }

        public bool GetSwitch(int id, bool defaultValue) => _switches.GetValueOrDefault(id, defaultValue);

        public bool SetSwitch(int id, bool value)
        {
            if (_switches.TryGetValue(id, out var previous) && previous == value)
            {
                return false;
            }
            _switches[id] = value;
            return true;
        }

        public bool ClearSwitch(int id) => _switches.Remove(id);

        public bool ClearSwitches()
        {
            var changed = _switches.Count > 0;
            _switches.Clear();
            return changed;
        }

        public (bool VariablesChanged, bool SwitchesChanged) Clear()
        {
            var changes = (_variables.Count > 0, _switches.Count > 0);
            _variables.Clear();
            _switches.Clear();
            return changes;
        }

        public (bool VariablesChanged, bool SwitchesChanged) Restore(ValueSaveData data)
        {
            var variablesChanged = RestoreValues(_variables, data.Variables);
            var switchesChanged = RestoreValues(_switches, data.Switches);
            return (variablesChanged, switchesChanged);
        }

        public ValueSaveData Capture()
        {
            return new ValueSaveData
            {
                Variables = new Dictionary<int, int>(_variables),
                Switches = new Dictionary<int, bool>(_switches),
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
