using System;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Gameplay
{
    [Serializable]
    public struct CameraPriorityRange
    {
        [SerializeField, HorizontalGroup, LabelText("最小值")]
        private int _minimum;
        public int Minimum => _minimum;

        [SerializeField, HorizontalGroup, LabelText("最大值")]
        private int _maximum;
        public int Maximum => _maximum;

        public CameraPriorityRange(int minimum, int maximum)
        {
            _minimum = minimum;
            _maximum = maximum;
        }

        public bool IsValid => _minimum <= _maximum;
        public bool Contains(int priority) => priority >= _minimum && priority <= _maximum;
        public bool Overlaps(in CameraPriorityRange other) => _minimum <= other._maximum && other._minimum <= _maximum;
        public int ResolveOffset(int offset) => checked(_minimum + offset);
    }
}
