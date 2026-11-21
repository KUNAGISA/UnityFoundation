namespace Gameplay.Tags
{
    public readonly struct GameplayTagDefinition
    {
        public GameplayTag Tag { get; }
        public GameplayTag Parent { get; }

        public GameplayTagDefinition(GameplayTag tag, GameplayTag parent = default)
        {
            Tag = tag;
            Parent = parent;
        }
    }
}
