using System;

namespace Gameplay
{
    public readonly struct InteractionToken : IEquatable<InteractionToken>
    {
        public static InteractionToken None => default;

        internal int Id { get; }
        public bool IsValid => Id != 0;

        internal InteractionToken(int id)
        {
            Id = id;
        }

        public bool Equals(InteractionToken other) => Id == other.Id;
        public override bool Equals(object obj) => obj is InteractionToken other && Equals(other);
        public override int GetHashCode() => Id;
        public override string ToString() => IsValid ? $"InteractionToken({Id})" : "InteractionToken(None)";

        public static bool operator ==(InteractionToken left, InteractionToken right) => left.Equals(right);
        public static bool operator !=(InteractionToken left, InteractionToken right) => !left.Equals(right);
    }
}
