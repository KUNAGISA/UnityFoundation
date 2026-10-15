using System;
using Aoiro;

namespace Gameplay
{
    public sealed class NpcCharacterState : CharacterState
    {
        public override int DominantEmotionCapacity => 1;
        public BindableProperty<int> Favorability { get; } = new();

        public NpcCharacterState(int npcId) : base(ValidateNpcId(npcId), CharacterKind.Npc)
        {
        }

        private static int ValidateNpcId(int npcId)
        {
            if (npcId <= PlayerCharacterState.PlayerId)
            {
                throw new ArgumentOutOfRangeException(nameof(npcId), npcId, "Npc id must be greater than the player id.");
            }
            return npcId;
        }
    }
}
