using System;
using MessagePack;

namespace Gameplay
{
    [MessagePackObject]
    public struct RangedValue : IEquatable<RangedValue>
    {
        [Key(0)] public int Current;
        [Key(1)] public int Max;

        public RangedValue(int current, int max)
        {
            Current = current;
            Max = max;
        }

        public bool Equals(RangedValue other)
        {
            return Current == other.Current && Max == other.Max;
        }

        public override bool Equals(object obj)
        {
            return obj is RangedValue other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Current, Max);
        }
    }
}
