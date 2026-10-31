namespace Gameplay
{
    public sealed class NpcEntity : CharacterEntity
    {
        public new NpcCharacterState State => (NpcCharacterState)base.State;

        public NpcEntity(NpcCharacterState state, SpatialState spatial) : base(state, spatial)
        {
        }
    }
}
