namespace Gameplay.Tags
{
    public readonly struct GameplayTagChange
    {
        public GameplayTag Tag { get; }
        public int OldCount { get; }
        public int NewCount { get; }

        public GameplayTagChange(GameplayTag tag, int oldCount, int newCount)
        {
            Tag = tag;
            OldCount = oldCount;
            NewCount = newCount;
        }
    }
}
