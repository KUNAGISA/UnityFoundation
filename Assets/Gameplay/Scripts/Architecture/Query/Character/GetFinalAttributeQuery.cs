using System;
using Aoiro;
using Gameplay.Tables;

namespace Gameplay
{
    public readonly struct GetFinalAttributeQuery : IQuery<int>
    {
        private readonly PlayerCharacterState _state;
        private readonly AttributeId _id;

        public GetFinalAttributeQuery(PlayerCharacterState state, AttributeId id)
        {
            _state = state ?? throw new ArgumentNullException(nameof(state));
            _id = id;
        }

        int IQuery<int>.Execute(IQueryContext context)
        {
            var baseValue = _state.Stats.GetAttribute(_id);
            var emotionModifier = GetEmotionModifier(context);

            return baseValue + emotionModifier;
        }

        private int GetEmotionModifier(IQueryContext context)
        {
            var emotion = _state.Emotion;
            var capacity = _state.DominantEmotionCapacity;
            if (capacity <= 0)
            {
                return 0;
            }

            Span<DominantEmotion> dominantEmotions = stackalloc DominantEmotion[capacity];
            var count = emotion.GetDominant(dominantEmotions);
            if (count <= 0)
            {
                return 0;
            }

            var emotionTable = context.GetModel<TablesModel>().EmotionTable;
            var modifier = 0;
            for (var i = 0; i < count; i++)
            {
                if (!emotionTable.TryGet(dominantEmotions[i].Id, out var entry))
                {
                    continue;
                }

                var modifiers = entry.Modifiers;
                for (var j = 0; j < modifiers.Length; j++)
                {
                    ref readonly var attributeModifier = ref modifiers[j];
                    if (attributeModifier.Id == _id)
                    {
                        modifier += attributeModifier.Value;
                    }
                }
            }

            return modifier;
        }
    }
}
