namespace Gameplay
{
    public sealed class PlayerCharacterState : CharacterState
    {
        public const int PlayerId = 0;

        public override int DominantEmotionCapacity => 3;
        public StatsState Stats { get; }
        public SpatialState Spatial { get; }

        public PlayerCharacterState() : base(PlayerId, CharacterKind.Player)
        {
            Stats = new StatsState();
            Spatial = new SpatialState();
        }
    }
}
