namespace Gameplay
{
    public sealed class NpcActor : CharacterActor
    {
        public new NpcCharacterState State => (NpcCharacterState)base.State;
    }
}
