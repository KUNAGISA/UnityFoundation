using Gameplay.Tables;

namespace Gameplay
{
    public readonly struct DominantEmotion
    {
        public readonly EmotionId Id;
        public readonly int Value;

        public DominantEmotion(EmotionId id, int value)
        {
            Id = id;
            Value = value;
        }
    }
}
