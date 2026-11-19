using System;

namespace Gameplay
{
    public abstract class CharacterState
    {
        public int CharacterId { get; }
        public CharacterKind Kind { get; }
        public EmotionState Emotion { get; }
        public abstract int DominantEmotionCapacity { get; }

        protected CharacterState(int characterId, CharacterKind kind)
        {
            if (characterId < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(characterId), characterId, "Character id cannot be negative.");
            }
            if (kind != CharacterKind.Player && kind != CharacterKind.Npc)
            {
                throw new ArgumentOutOfRangeException(nameof(kind), kind, "Character kind cannot be unknown.");
            }

            CharacterId = characterId;
            Kind = kind;
            Emotion = new EmotionState();
        }
    }
}
