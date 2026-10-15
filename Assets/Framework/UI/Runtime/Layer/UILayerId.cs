using System;

namespace Gameplay.UI
{
    public readonly struct UILayerId : IEquatable<UILayerId>
    {
        private const int UnspecifiedValue = int.MinValue;

        public static UILayerId Unspecified => new UILayerId(UnspecifiedValue);

        public int Value { get; }
        public bool IsSpecified => Value != UnspecifiedValue;

        public UILayerId(int value)
        {
            Value = value;
        }

        public bool Equals(UILayerId other) => Value == other.Value;
        public override bool Equals(object obj) => obj is UILayerId other && Equals(other);
        public override int GetHashCode() => Value;
        public override string ToString() => IsSpecified ? Value.ToString() : nameof(Unspecified);

        public static bool operator ==(UILayerId left, UILayerId right) => left.Equals(right);
        public static bool operator !=(UILayerId left, UILayerId right) => !left.Equals(right);
    }
}
