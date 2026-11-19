using System;
using Aoiro;
using Gameplay.Tables;

namespace Gameplay
{
    public sealed class EmotionState
    {
        private const int EmotionCount = (int)EmotionId.Count;

        private readonly int[] _values = new int[EmotionCount];
        private readonly int[] _thresholds = new int[EmotionCount];

        private Signal<StatValueChanged<EmotionId>> _valueChanged;
        private Signal<StatValueChanged<EmotionId>> _thresholdChanged;

        public ISignal<StatValueChanged<EmotionId>> ValueChanged => _valueChanged ??= new Signal<StatValueChanged<EmotionId>>();
        public ISignal<StatValueChanged<EmotionId>> ThresholdChanged => _thresholdChanged ??= new Signal<StatValueChanged<EmotionId>>();

        public void Reset()
        {
            Array.Fill(_values, 0);
            Array.Fill(_thresholds, 0);
        }
        
        public int GetValue(EmotionId id)
        {
            return _values[(int)id];
        }

        public void SetValue(EmotionId id, int value)
        {
            var index = (int)id;
            var oldValue = _values[index];
            if (oldValue == value)
            {
                return;
            }

            _values[index] = value;
            _valueChanged?.Emit(new StatValueChanged<EmotionId>(id, oldValue, value));
        }

        public void AddValue(EmotionId id, int value)
        {
            SetValue(id, GetValue(id) + value);
        }

        public int GetThreshold(EmotionId id)
        {
            return _thresholds[(int)id];
        }

        public void SetThreshold(EmotionId id, int value)
        {
            var index = (int)id;
            var oldValue = _thresholds[index];
            if (oldValue == value)
            {
                return;
            }

            _thresholds[index] = value;
            _thresholdChanged?.Emit(new StatValueChanged<EmotionId>(id, oldValue, value));
        }

        public void AddThreshold(EmotionId id, int value)
        {
            SetThreshold(id, GetThreshold(id) + value);
        }

        public int GetDominant(Span<DominantEmotion> results)
        {
            if (results.Length == 0)
            {
                return 0;
            }

            Span<DominantEmotion> buffer = stackalloc DominantEmotion[EmotionCount];
            for (var i = 0; i < buffer.Length; i++)
            {
                buffer[i] = new DominantEmotion((EmotionId)i, _values[i]);
            }

            SortDescending(buffer);

            var count = 0;
            for (var i = 0; i < buffer.Length && count < results.Length; i++)
            {
                if (buffer[i].Value <= 0)
                {
                    continue;
                }

                results[count++] = buffer[i];
            }

            return count;
        }

        private static void SortDescending(Span<DominantEmotion> values)
        {
            for (var i = 0; i < values.Length - 1; i++)
            {
                var maxIndex = i;
                for (var j = i + 1; j < values.Length; j++)
                {
                    if (values[j].Value > values[maxIndex].Value)
                    {
                        maxIndex = j;
                    }
                }

                if (maxIndex == i)
                {
                    continue;
                }

                (values[i], values[maxIndex]) = (values[maxIndex], values[i]);
            }
        }
    }
}
