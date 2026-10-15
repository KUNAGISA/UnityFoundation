using System;

namespace Gameplay.UI
{
    public readonly struct UIViewOptionMask : IEquatable<UIViewOptionMask>
    {
        public int Value { get; }
        public bool IsEmpty => Value == 0;

        public UIViewOptionMask(int value)
        {
            Value = value;
        }

        public bool Contains(UIViewOptionMask option) => !option.IsEmpty && (Value & option.Value) == option.Value;
        public bool Equals(UIViewOptionMask other) => Value == other.Value;
        public override bool Equals(object obj) => obj is UIViewOptionMask other && Equals(other);
        public override int GetHashCode() => Value;
        public override string ToString() => Value.ToString();

        public static bool operator ==(UIViewOptionMask left, UIViewOptionMask right) => left.Equals(right);
        public static bool operator !=(UIViewOptionMask left, UIViewOptionMask right) => !left.Equals(right);
    }
}
