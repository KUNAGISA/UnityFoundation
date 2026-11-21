using Gameplay.Tables;

namespace Gameplay
{
    public readonly struct VitalValueChanged
    {
        public readonly VitalId Id;
        public readonly RangedValue OldValue;
        public readonly RangedValue NewValue;

        public VitalValueChanged(VitalId id, RangedValue oldValue, RangedValue newValue)
        {
            Id = id;
            OldValue = oldValue;
            NewValue = newValue;
        }
    }
}
