using System;
using Aoiro;
using Gameplay.Tables;

namespace Gameplay
{
    public sealed class StatsState
    {
        private const int AttributeCount = (int)AttributeId.Count;
        private const int VitalCount = (int)VitalId.Count;

        private readonly int[] _attributes = new int[AttributeCount];
        private readonly RangedValue[] _vitals = new RangedValue[VitalCount];

        private Signal<StatValueChanged<AttributeId>> _attributeChanged;
        private Signal<VitalValueChanged> _vitalChanged;

        public ISignal<StatValueChanged<AttributeId>> AttributeChanged => _attributeChanged ??= new Signal<StatValueChanged<AttributeId>>();
        public ISignal<VitalValueChanged> VitalChanged => _vitalChanged ??= new Signal<VitalValueChanged>();

        public void Reset()
        {
            Array.Fill(_attributes, 0);
            Array.Fill(_vitals, default);
        }
        
        public int GetAttribute(AttributeId id)
        {
            return _attributes[(int)id];
        }

        public void SetAttribute(AttributeId id, int value)
        {
            var index = (int)id;
            var oldValue = _attributes[index];
            if (oldValue == value)
            {
                return;
            }

            _attributes[index] = value;
            _attributeChanged?.Emit(new StatValueChanged<AttributeId>(id, oldValue, value));
        }

        public void AddAttribute(AttributeId id, int value)
        {
            SetAttribute(id, GetAttribute(id) + value);
        }

        public RangedValue GetVital(VitalId id)
        {
            return _vitals[(int)id];
        }

        public void SetVital(VitalId id, RangedValue value)
        {
            var index = (int)id;
            var oldValue = _vitals[index];
            if (oldValue.Equals(value))
            {
                return;
            }

            _vitals[index] = value;
            _vitalChanged?.Emit(new VitalValueChanged(id, oldValue, value));
        }

    }
}
