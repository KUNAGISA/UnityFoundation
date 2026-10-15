namespace Gameplay.Sequence
{
    public sealed class AdvPortraitEntry
    {
        private readonly int _portraitId;
        private readonly int[] _channelVersions = new int[(int)AdvPortraitChannel.Count];
        
        private AdvPortraitVisualState _visualState;
        public ref AdvPortraitVisualState VisualState => ref _visualState;
        
        public string AssetLocation { get; internal set; } = string.Empty;
        public int SortingOrder { get; internal set; }
        
        internal AdvPortraitEntry(int portraitId)
        {
            _portraitId = portraitId;
            _visualState.Reset();
        }
        
        internal AdvPortraitChannelToken Acquire(AdvPortraitChannel channel)
        {
            var version = ++_channelVersions[(int)channel];
            return new AdvPortraitChannelToken(_portraitId, channel, version);
        }

        internal bool IsValid(in AdvPortraitChannelToken token)
        {
            var channel = (int)token.Channel;
            return channel >= 0 && channel < _channelVersions.Length && _channelVersions[channel] == token.Version;
        }
    }
}
