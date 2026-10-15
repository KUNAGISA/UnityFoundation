namespace Gameplay.Sequence
{
    public readonly struct AdvPortraitChannelToken
    {
        public int PortraitId { get; }
        public AdvPortraitChannel Channel { get; }
        public int Version { get; }

        internal AdvPortraitChannelToken(int portraitId, AdvPortraitChannel channel, int version)
        {
            PortraitId = portraitId;
            Channel = channel;
            Version = version;
        }
    }
}
