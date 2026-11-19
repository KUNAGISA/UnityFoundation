namespace Gameplay
{
    public readonly struct StatValueChanged<TId>
    {
        public readonly TId Id;
        public readonly int OldValue;
        public readonly int NewValue;

        public StatValueChanged(TId id, int oldValue, int newValue)
        {
            Id = id;
            OldValue = oldValue;
            NewValue = newValue;
        }
    }
}
