namespace Gameplay.Sequence
{
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
