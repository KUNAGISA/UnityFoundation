namespace Gameplay.Sequence
{
    public enum AdvPortraitChangeType
    {
        Visual,
        Asset,
        SortingOrder
    }

    public readonly struct AdvPortraitChange
    {
        public int PortraitId { get; }
        public AdvPortraitChangeType Type { get; }

        internal AdvPortraitChange(int portraitId, AdvPortraitChangeType type)
        {
            PortraitId = portraitId;
            Type = type;
        }
    }
}
